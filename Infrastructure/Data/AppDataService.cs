using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Documents;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Infrastructure.Network;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.Infrastructure.Data
{
    public sealed class AppDataService
    {
        private const string RecordCodePrefix = "HS";
        private const int RecordCodeSequenceWidth = 6;
        private static readonly Lazy<AppDataService> LazyInstance = new Lazy<AppDataService>(() => new AppDataService());
        private readonly string _connectionString;
        private readonly LanDataClient _lanClient;
        private readonly LanDataServer _lanServer;

        private AppDataService()
        {
            DatabasePath = AppPathSettings.Current.DatabasePath;
            if (AppPathSettings.Current.IsClientMode)
            {
                _lanClient = new LanDataClient();
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath));
            _connectionString = new SqliteConnectionStringBuilder { DataSource = DatabasePath }.ToString();
            _lanServer = new LanDataServer(this);
        }

        public static AppDataService Instance => LazyInstance.Value;

        public static Action<Action> UiDispatcher { get; set; }

        public event Action<string> CatalogChanged;

        public string DatabasePath { get; }

        public void Initialize()
        {
            var stopwatch = Stopwatch.StartNew();
            if (AppPathSettings.Current.IsClientMode)
            {
                AppLogger.Info("Database", "Initialize", $"Client mode enabled. Using admin server {AppPathSettings.Current.AdminServerUrl}.");
                _lanClient.Ping();
                LogElapsed("Database", "InitializeClient", stopwatch);
                return;
            }

            AppLogger.Info("Database", "Initialize", $"Initializing database at {DatabasePath}.");

            using var connection = OpenConnection();
            CreateSchema(connection);
            MigrateLegacyPriorityColumnIfNeeded(connection);
            RepairRecordForeignKeys(connection);
            SeedUsers(connection);
            SeedAreas(connection);
            EnsureStandardOrganizationAreas(connection);
            SeedCatalogs(connection);
            NormalizeSeverityCatalog(connection);
            NormalizeRecordSeverity(connection);
            NormalizeProcessingHistoryTitles(connection);
            SeedRecords(connection);
            NormalizeFutureRecordDates(connection);
            SyncProcessorCatalogFromRecords(connection);
            _lanServer.Start();
            LogElapsed("Database", "Initialize", stopwatch);
        }

        public IReadOnlyList<string> GetAreaNames(bool includeAll = false)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<string>>("catalog/areas", new IncludeAllRequest { IncludeAll = includeAll });
            }

            var result = new List<string>();
            if (includeAll)
            {
                result.Add("Tất cả");
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT AreaType, Name FROM Areas ORDER BY DisplayOrder;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(FormatAreaDisplayName(reader.GetString(0), reader.GetString(1)));
            }

            return result;
        }

        public IReadOnlyList<string> GetCatalogValues(string catalogType, bool includeAll = false)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<string>>("catalog/values", new CatalogValuesRequest { CatalogType = catalogType, IncludeAll = includeAll });
            }

            var result = new List<string>();
            if (includeAll)
            {
                result.Add("Tất cả");
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Name
FROM CatalogItems
WHERE CatalogType = $catalogType AND IsActive = 1
ORDER BY DisplayOrder;";
            command.Parameters.AddWithValue("$catalogType", catalogType);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader.GetString(0));
            }

            return result;
        }

        public IReadOnlyList<CatalogValueSetting> GetCatalogItems(string catalogType, bool includeInactive = false)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<CatalogValueSetting>>("settings/catalog-items", new CatalogItemsRequest { CatalogType = catalogType, IncludeInactive = includeInactive });
            }

            var result = new List<CatalogValueSetting>();

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = includeInactive
                ? @"
SELECT Id, CatalogType, Name, DisplayOrder, IsActive
FROM CatalogItems
WHERE CatalogType = $catalogType
ORDER BY IsActive DESC, DisplayOrder, Name;"
                : @"
SELECT Id, CatalogType, Name, DisplayOrder, IsActive
FROM CatalogItems
WHERE CatalogType = $catalogType AND IsActive = 1
ORDER BY DisplayOrder, Name;";
            command.Parameters.AddWithValue("$catalogType", catalogType);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new CatalogValueSetting
                {
                    Id = reader.GetInt32(0),
                    CatalogType = reader.GetString(1),
                    Name = reader.GetString(2),
                    DisplayOrder = reader.GetInt32(3),
                    IsActive = reader.GetInt32(4) == 1
                });
            }

            return result;
        }

        public IReadOnlyDictionary<string, int> CountCatalogItemsByType()
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<Dictionary<string, int>>("settings/catalog-counts", null);
            }

            var result = new Dictionary<string, int>(StringComparer.Ordinal);

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT CatalogType, COUNT(*)
FROM CatalogItems
WHERE IsActive = 1
GROUP BY CatalogType;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result[reader.GetString(0)] = reader.GetInt32(1);
            }

            return result;
        }

        public IReadOnlyList<SystemLogEntry> GetSystemLogs(int take = 200)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<SystemLogEntry>>("settings/system-logs", new SystemLogsRequest { Take = take });
            }

            using var connection = OpenConnection();
            var result = new List<SystemLogEntry>();
            var currentUserName = AuthContext.CurrentUser?.UserName;
            var userNameFilter = AuthContext.IsAdmin || string.IsNullOrWhiteSpace(currentUserName)
                ? null
                : currentUserName.Trim();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT CreatedAt, UserName, Module, Action, Target, Detail
FROM SystemLogs
WHERE $userName IS NULL OR lower(UserName) = lower($userName)
ORDER BY Id DESC
LIMIT $take;";
            command.Parameters.AddWithValue("$take", Math.Max(1, take));
            command.Parameters.AddWithValue("$userName", string.IsNullOrWhiteSpace(userNameFilter) ? DBNull.Value : userNameFilter);

            using var reader = command.ExecuteReader();
            var index = 1;
            while (reader.Read())
            {
                result.Add(new SystemLogEntry
                {
                    Index = index++,
                    CreatedAt = FormatDateTime(reader.GetString(0)),
                    UserName = reader.GetString(1),
                    Module = reader.GetString(2),
                    Action = reader.GetString(3),
                    Target = reader.GetString(4),
                    Detail = reader.GetString(5)
                });
            }

            return result;
        }

        public AppUser AuthenticateUser(string userName, string password)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<AppUser>("auth/login", new LoginRequest { UserName = userName, Password = password });
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Id, UserName, DisplayName, Role, PasswordHash, IsActive
FROM Users
WHERE lower(UserName) = lower($userName)
LIMIT 1;";
            command.Parameters.AddWithValue("$userName", userName.Trim());

            using var reader = command.ExecuteReader();
            if (!reader.Read() || reader.GetInt32(5) != 1)
            {
                return null;
            }

            var passwordHash = reader.GetString(4);
            if (!PasswordHasher.VerifyPassword(password, passwordHash))
            {
                return null;
            }

            return new AppUser
            {
                Id = reader.GetInt32(0),
                UserName = reader.GetString(1),
                DisplayName = reader.GetString(2),
                Role = reader.GetString(3),
                IsActive = true
            };
        }

        public IReadOnlyList<AppUser> GetUsers()
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<AppUser>>("settings/users", null);
            }

            EnsureAdmin();
            var result = new List<AppUser>();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Id, UserName, DisplayName, Role, IsActive
FROM Users
ORDER BY IsActive DESC, Role, DisplayName;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new AppUser
                {
                    Id = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    DisplayName = reader.GetString(2),
                    Role = reader.GetString(3),
                    IsActive = reader.GetInt32(4) == 1
                });
            }

            return result;
        }

        public bool SaveUser(AppUser user, string password)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<bool>("settings/users/save", new SaveUserRequest { User = user, Password = password });
            }

            EnsureAdmin();
            if (user == null ||
                string.IsNullOrWhiteSpace(user.UserName) ||
                string.IsNullOrWhiteSpace(user.DisplayName) ||
                string.IsNullOrWhiteSpace(user.Role))
            {
                return false;
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            if (user.Id > 0)
            {
                command.CommandText = string.IsNullOrWhiteSpace(password)
                    ? @"
UPDATE Users
SET UserName = $userName, DisplayName = $displayName, Role = $role, IsActive = $isActive
WHERE Id = $id;"
                    : @"
UPDATE Users
SET UserName = $userName, DisplayName = $displayName, Role = $role, PasswordHash = $passwordHash, IsActive = $isActive
WHERE Id = $id;";
                command.Parameters.AddWithValue("$id", user.Id);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    return false;
                }

                command.CommandText = @"
INSERT INTO Users (UserName, DisplayName, Role, PasswordHash, IsActive, CreatedAt, UpdatedAt)
VALUES ($userName, $displayName, $role, $passwordHash, $isActive, $now, $now);";
            }

            command.Parameters.AddWithValue("$userName", NormalizeDbText(user.UserName));
            command.Parameters.AddWithValue("$displayName", NormalizeDbText(user.DisplayName));
            command.Parameters.AddWithValue("$role", NormalizeRole(user.Role));
            command.Parameters.AddWithValue("$isActive", user.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("$now", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(password))
            {
                command.Parameters.AddWithValue("$passwordHash", PasswordHasher.HashPassword(password));
            }

            var affectedRows = command.ExecuteNonQuery();
            if (affectedRows > 0)
            {
                WriteDatabaseLog(connection, null, "Người dùng", user.Id > 0 ? "Sửa" : "Thêm", user.UserName, $"Cập nhật tài khoản {user.UserName}.");
            }

            return affectedRows > 0;
        }

        public bool DeleteUser(int userId)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<bool>("settings/users/delete", new UserIdRequest { UserId = userId });
            }

            EnsureAdmin();
            if (userId <= 0 || AuthContext.CurrentUser?.Id == userId)
            {
                return false;
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Users SET IsActive = 0, UpdatedAt = $now WHERE Id = $id;";
            command.Parameters.AddWithValue("$id", userId);
            command.Parameters.AddWithValue("$now", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            var affectedRows = command.ExecuteNonQuery();
            if (affectedRows > 0)
            {
                WriteDatabaseLog(connection, null, "Người dùng", "Xóa", userId.ToString(CultureInfo.InvariantCulture), "Ngừng sử dụng tài khoản.");
            }

            return affectedRows > 0;
        }

        public bool ChangeCurrentUserPassword(string currentPassword, string newPassword)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<bool>("settings/users/change-password", new ChangePasswordRequest
                {
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword
                });
            }

            var currentUser = AuthContext.CurrentUser;
            if (currentUser == null ||
                currentUser.Id <= 0 ||
                string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                return false;
            }

            using var connection = OpenConnection();
            string passwordHash;
            using (var readCommand = connection.CreateCommand())
            {
                readCommand.CommandText = "SELECT PasswordHash FROM Users WHERE Id = $id AND IsActive = 1 LIMIT 1;";
                readCommand.Parameters.AddWithValue("$id", currentUser.Id);
                passwordHash = readCommand.ExecuteScalar()?.ToString();
            }

            if (!PasswordHasher.VerifyPassword(currentPassword, passwordHash))
            {
                return false;
            }

            using var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = "UPDATE Users SET PasswordHash = $passwordHash, UpdatedAt = $now WHERE Id = $id AND IsActive = 1;";
            updateCommand.Parameters.AddWithValue("$id", currentUser.Id);
            updateCommand.Parameters.AddWithValue("$passwordHash", PasswordHasher.HashPassword(newPassword));
            updateCommand.Parameters.AddWithValue("$now", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            var affectedRows = updateCommand.ExecuteNonQuery();
            if (affectedRows > 0)
            {
                WriteDatabaseLog(connection, null, "Người dùng", "Đổi mật khẩu", currentUser.UserName, "Người dùng đổi mật khẩu tài khoản của mình.");
            }

            return affectedRows > 0;
        }

        public int AddCatalogItem(string catalogType, string name)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<int>("settings/catalog/add", new SaveCatalogItemRequest { CatalogType = catalogType, Name = name });
            }

            EnsureAdmin();
            if (string.IsNullOrWhiteSpace(catalogType) || string.IsNullOrWhiteSpace(name))
            {
                return 0;
            }

            using var connection = OpenConnection();
            var trimmedName = name.Trim();
            if (CatalogNameExists(connection, catalogType, trimmedName))
            {
                return 0;
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO CatalogItems (CatalogType, Name, DisplayOrder, IsActive)
VALUES (
    $catalogType,
    $name,
    COALESCE((SELECT MAX(DisplayOrder) + 1 FROM CatalogItems WHERE CatalogType = $catalogType), 1),
    1);
SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("$catalogType", catalogType);
            command.Parameters.AddWithValue("$name", trimmedName);
            var newId = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            WriteDatabaseLog(connection, null, "Danh mục", "Thêm", $"{catalogType}:{newId}", $"Thêm danh mục \"{trimmedName}\".");
            NotifyCatalogChanged(catalogType);
            return newId;
        }

        public bool UpdateCatalogItem(int id, string name)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<bool>("settings/catalog/update", new SaveCatalogItemRequest { Id = id, Name = name });
            }

            EnsureAdmin();
            if (id <= 0 || string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            using var connection = OpenConnection();
            var trimmedName = name.Trim();
            var catalogType = GetCatalogType(connection, id);
            if (string.IsNullOrWhiteSpace(catalogType) || CatalogNameExists(connection, catalogType, trimmedName, id))
            {
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE CatalogItems SET Name = $name WHERE Id = $id AND IsActive = 1;";
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$name", trimmedName);
            var affectedRows = command.ExecuteNonQuery();
            if (affectedRows > 0)
            {
                WriteDatabaseLog(connection, null, "Danh mục", "Sửa", $"{catalogType}:{id}", $"Cập nhật danh mục thành \"{trimmedName}\".");
                NotifyCatalogChanged(catalogType);
            }

            return affectedRows > 0;
        }

        public bool DeleteCatalogItem(int id)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<bool>("settings/catalog/delete", new CatalogItemIdRequest { Id = id });
            }

            EnsureAdmin();
            if (id <= 0)
            {
                return false;
            }

            using var connection = OpenConnection();
            var catalogType = GetCatalogType(connection, id);
            var catalogName = GetCatalogItemName(connection, id);
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE CatalogItems SET IsActive = 0 WHERE Id = $id AND IsActive = 1;";
            command.Parameters.AddWithValue("$id", id);
            var affectedRows = command.ExecuteNonQuery();
            if (affectedRows > 0)
            {
                WriteDatabaseLog(connection, null, "Danh mục", "Xóa", $"{catalogType}:{id}", $"Ngưng sử dụng danh mục \"{catalogName}\".");
                NotifyCatalogChanged(catalogType);
            }

            return affectedRows > 0;
        }

        public void UpdateCatalogItemOrders(IReadOnlyList<CatalogValueSetting> items)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                _lanClient.Call<bool>("settings/catalog/reorder", new ReorderCatalogItemsRequest { Items = items });
                return;
            }

            EnsureAdmin();
            if (items == null || items.Count == 0)
            {
                return;
            }

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            for (var index = 0; index < items.Count; index++)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "UPDATE CatalogItems SET DisplayOrder = $displayOrder WHERE Id = $id;";
                command.Parameters.AddWithValue("$displayOrder", index + 1);
                command.Parameters.AddWithValue("$id", items[index].Id);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
            WriteDatabaseLog(connection, null, "Danh mục", "Sắp xếp", "CatalogItems", $"Cập nhật thứ tự {items.Count} danh mục.");
            NotifyCatalogChanged(items[0].CatalogType);
        }

        public IReadOnlyList<string> GetProcessorNames(bool includeAll = false)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<string>>("catalog/processors", new IncludeAllRequest { IncludeAll = includeAll });
            }

            var result = new List<string>();
            if (includeAll)
            {
                result.Add("Tất cả");
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Name
FROM CatalogItems
WHERE CatalogType = 'ProcessorName' AND IsActive = 1
UNION
SELECT ProcessorName
FROM Records
WHERE trim(ProcessorName) <> ''
ORDER BY Name;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader.GetString(0));
            }

            return result;
        }

        public IReadOnlyList<StaffPerformanceRow> GetStaffPerformanceRows(DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<StaffPerformanceRow>>("staff/performance", new DateRangeRequest { FromDate = fromDate, ToDate = toDate });
            }

            var processorNames = GetProcessorNames(includeAll: false)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (AuthContext.IsOfficer)
            {
                processorNames = processorNames
                    .Where(name => string.Equals(name.Trim(), AuthContext.CurrentDisplayName.Trim(), StringComparison.CurrentCultureIgnoreCase))
                    .ToList();
            }

            var result = new List<StaffPerformanceRow>();
            if (processorNames.Count == 0)
            {
                return result;
            }

            using var connection = OpenConnection();
            var today = DateTime.Today.ToString("O", CultureInfo.InvariantCulture);
            var dueSoon = DateTime.Today.AddDays(7).Date.AddDays(1).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture);
            var from = fromDate?.Date.ToString("O", CultureInfo.InvariantCulture);
            var to = toDate?.Date.AddDays(1).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture);

            foreach (var processorName in processorNames)
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
SELECT
    COUNT(*) AS AssignedCount,
    SUM(CASE WHEN Status <> 'Đã giải quyết' THEN 1 ELSE 0 END) AS ProcessingCount,
    SUM(CASE WHEN Status = 'Đã giải quyết' THEN 1 ELSE 0 END) AS CompletedCount,
    SUM(CASE WHEN Status <> 'Đã giải quyết' AND ExpectedResultDate <> '' AND ExpectedResultDate >= $today AND ExpectedResultDate <= $dueSoon THEN 1 ELSE 0 END) AS DueSoonCount,
    SUM(CASE WHEN Status <> 'Đã giải quyết' AND ExpectedResultDate <> '' AND ExpectedResultDate < $today THEN 1 ELSE 0 END) AS OverdueCount,
    SUM(CASE WHEN Status = 'Đã giải quyết' AND ExpectedResultDate <> '' AND UpdatedAt <> '' AND datetime(UpdatedAt) <= datetime(ExpectedResultDate) THEN 1 ELSE 0 END) AS OnTimeCompletedCount,
    SUM(CASE WHEN ExpectedResultDate <> '' THEN 1 ELSE 0 END) AS DeadlineTrackedCount,
    AVG(CASE WHEN Status = 'Đã giải quyết' AND ReceivedDate <> '' AND UpdatedAt <> '' AND datetime(UpdatedAt) >= datetime(ReceivedDate) THEN julianday(UpdatedAt) - julianday(ReceivedDate) END) AS AverageProcessingDays
FROM Records
WHERE TRIM(ProcessorName) = $processorName
    AND ($fromDate IS NULL OR ReceivedDate >= $fromDate)
    AND ($toDate IS NULL OR ReceivedDate <= $toDate);";
                command.Parameters.AddWithValue("$processorName", processorName.Trim());
                command.Parameters.AddWithValue("$today", today);
                command.Parameters.AddWithValue("$dueSoon", dueSoon);
                                command.Parameters.AddWithValue("$fromDate", (object)from ?? DBNull.Value);
                                command.Parameters.AddWithValue("$toDate", (object)to ?? DBNull.Value);

                using var reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    continue;
                }

                var assignedCount = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
                var processingCount = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);
                var completedCount = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture);
                var dueSoonCount = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture);
                var overdueCount = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetValue(4), CultureInfo.InvariantCulture);
                var onTimeCompletedCount = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture);
                var deadlineTrackedCount = reader.IsDBNull(6) ? 0 : Convert.ToInt32(reader.GetValue(6), CultureInfo.InvariantCulture);
                var averageProcessingDays = reader.IsDBNull(7) ? 0d : Convert.ToDouble(reader.GetValue(7), CultureInfo.InvariantCulture);
                var onTimeRate = deadlineTrackedCount > 0
                    ? (int)Math.Round((double)onTimeCompletedCount / deadlineTrackedCount * 100d, MidpointRounding.AwayFromZero)
                    : 100;

                result.Add(new StaffPerformanceRow
                {
                    Initials = BuildInitials(processorName),
                    Name = processorName,
                    Position = "Chuyên viên",
                    AssignedCount = assignedCount,
                    ProcessingCount = processingCount,
                    CompletedCount = completedCount,
                    DueSoonCount = dueSoonCount,
                    OverdueCount = overdueCount,
                    AverageProcessingTimeText = averageProcessingDays > 0d ? $"{averageProcessingDays:0.0} ngày" : "0 ngày",
                    OnTimeRateText = $"{onTimeRate}%",
                    OnTimeRateColor = onTimeRate >= 90 ? "#0FA958" : onTimeRate >= 84 ? "#1784E8" : "#F97316",
                    KpiPercent = Math.Clamp(onTimeRate, 0, 100),
                    KpiStatus = onTimeRate >= 90 ? "Tốt" : onTimeRate >= 80 ? "Khá" : "Cần cải thiện",
                    KpiStatusBackground = onTimeRate >= 90 ? "#DCFCE7" : onTimeRate >= 80 ? "#E7F0FF" : "#FFF0E6",
                    KpiStatusForeground = onTimeRate >= 90 ? "#0D7A2A" : onTimeRate >= 80 ? "#075CE8" : "#EA580C"
                });
            }

            return result.OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public IReadOnlyList<StatusStat> GetStaffDeadlineStats(DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<StatusStat>>("staff/deadlines", new DateRangeRequest { FromDate = fromDate, ToDate = toDate });
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            var today = DateTime.Today.ToString("O", CultureInfo.InvariantCulture);
            var dueSoon = DateTime.Today.AddDays(7).Date.AddDays(1).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture);
            var from = fromDate?.Date.ToString("O", CultureInfo.InvariantCulture);
            var to = toDate?.Date.AddDays(1).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture);

            command.CommandText = @"
SELECT
    SUM(CASE WHEN ExpectedResultDate <> '' AND ((Status = 'Đã giải quyết' AND UpdatedAt <> '' AND datetime(UpdatedAt) <= datetime(ExpectedResultDate)) OR (Status <> 'Đã giải quyết' AND ExpectedResultDate > $dueSoon)) THEN 1 ELSE 0 END) AS OnTimeCount,
    SUM(CASE WHEN Status <> 'Đã giải quyết' AND ExpectedResultDate <> '' AND ExpectedResultDate >= $today AND ExpectedResultDate <= $dueSoon THEN 1 ELSE 0 END) AS DueSoonCount,
    SUM(CASE WHEN ExpectedResultDate <> '' AND ((Status <> 'Đã giải quyết' AND ExpectedResultDate < $today) OR (Status = 'Đã giải quyết' AND UpdatedAt <> '' AND datetime(UpdatedAt) > datetime(ExpectedResultDate))) THEN 1 ELSE 0 END) AS OverdueCount
FROM Records
WHERE TRIM(ProcessorName) <> ''
  AND ($currentProcessorName IS NULL OR TRIM(ProcessorName) = $currentProcessorName)
  AND ($fromDate IS NULL OR ReceivedDate >= $fromDate)
  AND ($toDate IS NULL OR ReceivedDate <= $toDate);";
            command.Parameters.AddWithValue("$today", today);
            command.Parameters.AddWithValue("$dueSoon", dueSoon);
            command.Parameters.AddWithValue("$currentProcessorName", AuthContext.IsOfficer ? AuthContext.CurrentDisplayName.Trim() : DBNull.Value);
            command.Parameters.AddWithValue("$fromDate", (object)from ?? DBNull.Value);
            command.Parameters.AddWithValue("$toDate", (object)to ?? DBNull.Value);

            using var reader = command.ExecuteReader();
            var onTimeCount = 0;
            var dueSoonCount = 0;
            var overdueCount = 0;
            if (reader.Read())
            {
                onTimeCount = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
                dueSoonCount = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);
                overdueCount = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture);
            }

            var total = Math.Max(1, onTimeCount + dueSoonCount + overdueCount);
            var result = new List<StatusStat>
            {
                CreateDeadlineStatusStat("Đúng hạn", onTimeCount, total, "#35C47B"),
                CreateDeadlineStatusStat("Sắp quá hạn", dueSoonCount, total, "#F6A623"),
                CreateDeadlineStatusStat("Quá hạn", overdueCount, total, "#EF4444")
            };

            var startAngle = -90.0;
            foreach (var item in result)
            {
                item.StartAngle = startAngle;
                item.SweepAngle = item.Count * 360.0 / total;
                startAngle += item.SweepAngle;
            }

            return result;
        }

        public IReadOnlyList<StaffWorkRecord> GetStaffActiveRecords(string processorName, DateTime? fromDate = null, DateTime? toDate = null, int take = 4)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<StaffWorkRecord>>("staff/active-records", new StaffActiveRecordsRequest { ProcessorName = processorName, FromDate = fromDate, ToDate = toDate, Take = take });
            }

            if (string.IsNullOrWhiteSpace(processorName))
            {
                return Array.Empty<StaffWorkRecord>();
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT RecordCode, CaseType, ExpectedResultDate
FROM Records
WHERE TRIM(ProcessorName) = $processorName
  AND Status <> 'Đã giải quyết'
  AND ($fromDate IS NULL OR ReceivedDate >= $fromDate)
  AND ($toDate IS NULL OR ReceivedDate <= $toDate)
ORDER BY
  CASE WHEN ExpectedResultDate = '' THEN 1 ELSE 0 END,
  ExpectedResultDate ASC,
  UpdatedAt DESC
LIMIT $take;";
            command.Parameters.AddWithValue("$processorName", processorName.Trim());
            command.Parameters.AddWithValue("$fromDate", (object)fromDate?.Date.ToString("O", CultureInfo.InvariantCulture) ?? DBNull.Value);
            command.Parameters.AddWithValue("$toDate", (object)toDate?.Date.AddDays(1).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture) ?? DBNull.Value);
            command.Parameters.AddWithValue("$take", Math.Max(1, take));

            var result = new List<StaffWorkRecord>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var expectedResultDate = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                var deadlineStatus = BuildStaffDeadlineStatus(expectedResultDate, out var statusColor);
                result.Add(new StaffWorkRecord
                {
                    RecordCode = reader.GetString(0),
                    CaseType = reader.GetString(1),
                    DeadlineText = string.IsNullOrWhiteSpace(expectedResultDate) ? "Chưa có hạn xử lý" : $"Deadline: {FormatDate(expectedResultDate)}",
                    DeadlineStatus = deadlineStatus,
                    StatusColor = statusColor
                });
            }

            return result;
        }

        public (string Message, string ReceivedText) GetLatestLeadershipNotice(string officerName)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                var response = _lanClient.Call<LeadershipNoticeResponse>("leadership-notices/latest", new LeadershipNoticeRequest { OfficerName = officerName });
                return (response?.Message ?? "Chưa có thông báo mới từ lãnh đạo.", response?.ReceivedText ?? "Cập nhật mới nhất: --/--/---- --:--");
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Message, CreatedAt, SenderName
FROM LeadershipNotices
WHERE $officerName = '' OR Scope = 'All' OR TRIM(TargetName) = $officerName
ORDER BY CreatedAt DESC, Id DESC
LIMIT 1;";
            command.Parameters.AddWithValue("$officerName", (officerName ?? string.Empty).Trim());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return ("Chưa có thông báo mới từ lãnh đạo.", "Cập nhật mới nhất: --/--/---- --:--");
            }

            var message = reader.GetString(0);
            var createdAt = FormatDateTime(reader.GetString(1));
            var senderName = reader.GetString(2);
            return (message, $"Nhận lúc {createdAt} từ {senderName}");
        }

        public StaffNotificationPage GetLeadershipNotices(
            string officerName,
            int skip,
            int take,
            bool adminOnly = false,
            bool includeAll = false)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                try
                {
                    return _lanClient.Call<StaffNotificationPage>("leadership-notices/list", new LeadershipNoticeRequest
                    {
                        OfficerName = officerName,
                        Skip = Math.Max(0, skip),
                        Take = Math.Max(1, take),
                        AdminOnly = adminOnly,
                        IncludeAll = includeAll
                    }) ?? new StaffNotificationPage { Items = Array.Empty<StaffNotification>() };
                }
                catch (InvalidOperationException)
                {
                    if (adminOnly)
                    {
                        return new StaffNotificationPage { Items = Array.Empty<StaffNotification>() };
                    }

                    var latest = GetLatestLeadershipNotice(officerName);
                    return new StaffNotificationPage
                    {
                        Items = string.IsNullOrWhiteSpace(latest.Message)
                            ? Array.Empty<StaffNotification>()
                            : new[]
                            {
                                new StaffNotification
                                {
                                    Message = latest.Message,
                                    ReceivedText = latest.ReceivedText,
                                    IsUnread = false
                                }
                            },
                        TotalCount = string.IsNullOrWhiteSpace(latest.Message) ? 0 : 1,
                        UnreadCount = 0
                    };
                }
            }

            var normalizedOfficerName = (officerName ?? string.Empty).Trim();
            var readToken = BuildNoticeReadToken(normalizedOfficerName);
            var canIncludeAll = includeAll && AuthContext.IsAdmin;
            using var connection = OpenConnection();

            using var countCommand = connection.CreateCommand();
            countCommand.CommandText = @"
SELECT COUNT(*),
       SUM(CASE WHEN $readToken <> '||' AND INSTR(ReadBy, $readToken) = 0 THEN 1 ELSE 0 END)
FROM LeadershipNotices
WHERE ($includeAll = 1 OR $officerName = '' OR Scope = 'All' OR TRIM(TargetName) = $officerName)
  AND ($adminOnly = 0 OR EXISTS (
      SELECT 1
      FROM Users
      WHERE TRIM(DisplayName) = TRIM(LeadershipNotices.SenderName)
        AND Role = $adminRole));";
            countCommand.Parameters.AddWithValue("$officerName", normalizedOfficerName);
            countCommand.Parameters.AddWithValue("$readToken", readToken);
            countCommand.Parameters.AddWithValue("$includeAll", canIncludeAll ? 1 : 0);
            countCommand.Parameters.AddWithValue("$adminOnly", adminOnly ? 1 : 0);
            countCommand.Parameters.AddWithValue("$adminRole", UserRoles.Admin);
            using var countReader = countCommand.ExecuteReader();
            var totalCount = 0;
            var unreadCount = 0;
            if (countReader.Read())
            {
                totalCount = countReader.IsDBNull(0) ? 0 : countReader.GetInt32(0);
                unreadCount = countReader.IsDBNull(1) ? 0 : countReader.GetInt32(1);
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Id, Message, CreatedAt, SenderName, ReadBy
FROM LeadershipNotices
WHERE ($includeAll = 1 OR $officerName = '' OR Scope = 'All' OR TRIM(TargetName) = $officerName)
  AND ($adminOnly = 0 OR EXISTS (
      SELECT 1
      FROM Users
      WHERE TRIM(DisplayName) = TRIM(LeadershipNotices.SenderName)
        AND Role = $adminRole))
ORDER BY CreatedAt DESC, Id DESC
LIMIT $take OFFSET $skip;";
            command.Parameters.AddWithValue("$officerName", normalizedOfficerName);
            command.Parameters.AddWithValue("$includeAll", canIncludeAll ? 1 : 0);
            command.Parameters.AddWithValue("$adminOnly", adminOnly ? 1 : 0);
            command.Parameters.AddWithValue("$adminRole", UserRoles.Admin);
            command.Parameters.AddWithValue("$take", Math.Max(1, take));
            command.Parameters.AddWithValue("$skip", Math.Max(0, skip));

            var items = new List<StaffNotification>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var createdAt = FormatDateTime(reader.GetString(2));
                var senderName = reader.GetString(3);
                var readBy = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                items.Add(new StaffNotification
                {
                    Id = reader.GetInt32(0),
                    Message = reader.GetString(1),
                    SenderName = senderName,
                    ReceivedText = $"Nhận lúc {createdAt} từ {senderName}",
                    IsUnread = readToken != "||" && !readBy.Contains(readToken, StringComparison.OrdinalIgnoreCase)
                });
            }

            return new StaffNotificationPage
            {
                Items = items,
                TotalCount = totalCount,
                UnreadCount = unreadCount
            };
        }

            /*
            using var countCommand = connection.CreateCommand();
            countCommand.CommandText = @"
SELECT COUNT(*),
       SUM(CASE WHEN $readToken <> '||' AND INSTR(ReadBy, $readToken) = 0 THEN 1 ELSE 0 END)
FROM LeadershipNotices
WHERE $officerName = '' OR Scope = 'All' OR TRIM(TargetName) = $officerName;";
            countCommand.Parameters.AddWithValue("$officerName", normalizedOfficerName);
            countCommand.Parameters.AddWithValue("$readToken", readToken);
            using var countReader = countCommand.ExecuteReader();
            var totalCount = 0;
            var unreadCount = 0;
            if (countReader.Read())
            {
                totalCount = countReader.IsDBNull(0) ? 0 : countReader.GetInt32(0);
                unreadCount = countReader.IsDBNull(1) ? 0 : countReader.GetInt32(1);
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Id, Message, CreatedAt, SenderName, ReadBy
FROM LeadershipNotices
WHERE $officerName = '' OR Scope = 'All' OR TRIM(TargetName) = $officerName
ORDER BY CreatedAt DESC, Id DESC
LIMIT $take OFFSET $skip;";
            command.Parameters.AddWithValue("$officerName", normalizedOfficerName);
            command.Parameters.AddWithValue("$take", Math.Max(1, take));
            command.Parameters.AddWithValue("$skip", Math.Max(0, skip));

            var items = new List<StaffNotification>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var createdAt = FormatDateTime(reader.GetString(2));
                var senderName = reader.GetString(3);
                var readBy = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                items.Add(new StaffNotification
                {
                    Id = reader.GetInt32(0),
                    Message = reader.GetString(1),
                    SenderName = senderName,
                    ReceivedText = $"Nhận lúc {createdAt} từ {senderName}",
                    IsUnread = readToken != "||" && !readBy.Contains(readToken, StringComparison.OrdinalIgnoreCase)
                });
            }

            return new StaffNotificationPage
            {
                Items = items,
                TotalCount = totalCount,
                UnreadCount = unreadCount
            };
        }

        */
        public int CountUnreadLeadershipNotices(string officerName, bool adminOnly = false, bool includeAll = false)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                try
                {
                    var page = _lanClient.Call<StaffNotificationPage>("leadership-notices/list", new LeadershipNoticeRequest
                    {
                        OfficerName = officerName,
                        Skip = 0,
                        Take = 1,
                        AdminOnly = adminOnly,
                        IncludeAll = includeAll
                    });
                    return page?.UnreadCount ?? 0;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            }

            return GetLeadershipNotices(officerName, 0, 1, adminOnly, includeAll).UnreadCount;
        }

        public void MarkLeadershipNoticesAsRead(string officerName, IReadOnlyList<int> noticeIds, bool includeAll = false)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                try
                {
                    _lanClient.Call<bool>("leadership-notices/mark-read", new MarkLeadershipNoticesReadRequest
                    {
                        OfficerName = officerName,
                        NoticeIds = noticeIds,
                        IncludeAll = includeAll
                    });
                }
                catch (InvalidOperationException)
                {
                }
                return;
            }

            var normalizedOfficerName = (officerName ?? string.Empty).Trim();
            var readToken = BuildNoticeReadToken(normalizedOfficerName);
            var canIncludeAll = includeAll && AuthContext.IsAdmin;
            if (readToken == "||" || noticeIds == null || noticeIds.Count == 0)
            {
                return;
            }

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            foreach (var noticeId in noticeIds.Distinct())
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
UPDATE LeadershipNotices
SET ReadBy = ReadBy || $readToken
WHERE Id = $id
  AND ($includeAll = 1 OR $officerName = '' OR Scope = 'All' OR TRIM(TargetName) = $officerName)
  AND INSTR(ReadBy, $readToken) = 0;";
                command.Parameters.AddWithValue("$id", noticeId);
                command.Parameters.AddWithValue("$officerName", normalizedOfficerName);
                command.Parameters.AddWithValue("$includeAll", canIncludeAll ? 1 : 0);
                command.Parameters.AddWithValue("$readToken", readToken);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        public string GetLatestLeadershipKpiTarget(string officerName)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                try
                {
                    var response = _lanClient.Call<LeadershipKpiTargetResponse>("leadership-kpi/latest", new LeadershipKpiTargetRequest { OfficerName = officerName });
                    return string.IsNullOrWhiteSpace(response?.KpiTarget) ? "30" : response.KpiTarget;
                }
                catch (InvalidOperationException)
                {
                    return "30";
                }
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT KpiTarget
FROM LeadershipKpiTargets
WHERE $officerName = '' OR Scope = 'All' OR TRIM(TargetName) = $officerName
ORDER BY CreatedAt DESC, Id DESC
LIMIT 1;";
            command.Parameters.AddWithValue("$officerName", (officerName ?? string.Empty).Trim());

            var result = command.ExecuteScalar() as string;
            return string.IsNullOrWhiteSpace(result) ? "30" : result;
        }

        public void SaveLeadershipKpi(string scope, string targetName, string kpiTarget)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                _lanClient.Call<bool>("leadership-kpi/save", new SaveLeadershipKpiRequest { Scope = scope, TargetName = targetName, KpiTarget = kpiTarget });
                return;
            }

            if (!AuthContext.IsLeader && !AuthContext.IsAdmin)
            {
                throw new UnauthorizedAccessException("Chi admin hoac lanh dao duoc dat KPI.");
            }

            if (!int.TryParse(kpiTarget, NumberStyles.Integer, CultureInfo.InvariantCulture, out var target) || target <= 0)
            {
                throw new ArgumentException("Chi tieu KPI khong hop le.", nameof(kpiTarget));
            }

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO LeadershipKpiTargets (CreatedAt, SenderName, Scope, TargetName, KpiTarget)
VALUES ($createdAt, $senderName, $scope, $targetName, $kpiTarget);";
            command.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$senderName", AuthContext.CurrentDisplayName);
            command.Parameters.AddWithValue("$scope", string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase) ? "All" : "Staff");
            command.Parameters.AddWithValue("$targetName", NormalizeDbText(targetName));
            command.Parameters.AddWithValue("$kpiTarget", target.ToString(CultureInfo.InvariantCulture));
            command.ExecuteNonQuery();

            WriteDatabaseLog(connection, transaction, "Theo dõi cán bộ", "Đặt KPI", targetName, $"Đặt KPI {target} hồ sơ.");
            transaction.Commit();
        }

        public void SaveLeadershipNotice(string scope, string targetName, string kpiTarget, string message)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                _lanClient.Call<bool>("leadership-notices/save", new SaveLeadershipNoticeRequest { Scope = scope, TargetName = targetName, KpiTarget = kpiTarget, Message = message });
                return;
            }

            if (!AuthContext.IsLeader && !AuthContext.IsAdmin)
            {
                throw new UnauthorizedAccessException("Chi admin hoac lanh dao duoc gui thong bao.");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Noi dung thong bao khong duoc de trong.", nameof(message));
            }

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO LeadershipNotices (CreatedAt, SenderName, Scope, TargetName, KpiTarget, Message)
VALUES ($createdAt, $senderName, $scope, $targetName, $kpiTarget, $message);";
            command.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$senderName", AuthContext.CurrentDisplayName);
            command.Parameters.AddWithValue("$scope", string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase) ? "All" : "Staff");
            command.Parameters.AddWithValue("$targetName", NormalizeDbText(targetName));
            command.Parameters.AddWithValue("$kpiTarget", NormalizeDbText(kpiTarget));
            command.Parameters.AddWithValue("$message", NormalizeDbText(message));
            command.ExecuteNonQuery();

            WriteDatabaseLog(connection, transaction, "Theo dõi cán bộ", "Thông báo", targetName, "Lãnh đạo gửi thông báo cho cán bộ.");
            transaction.Commit();
        }

        private static string BuildNoticeReadToken(string officerName)
        {
            return $"|{(officerName ?? string.Empty).Trim().ToUpperInvariant()}|";
        }

        private static StatusStat CreateDeadlineStatusStat(string name, int count, int total, string color)
        {
            return new StatusStat
            {
                Name = name,
                Count = count,
                Percentage = $"{count * 100.0 / total:0.0}%",
                Width = Math.Max(8, (int)Math.Round(count * 130.0 / total)),
                Color = color
            };
        }

        private static string BuildStaffDeadlineStatus(string expectedResultDate, out string statusColor)
        {
            statusColor = "#35C47B";
            if (string.IsNullOrWhiteSpace(expectedResultDate) || !DateTime.TryParse(expectedResultDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var deadline))
            {
                statusColor = "#6B7280";
                return "Chưa xác định";
            }

            var remainingDays = (deadline.Date - DateTime.Today).Days;
            if (remainingDays < 0)
            {
                statusColor = "#EF4444";
                return $"Quá hạn {Math.Abs(remainingDays)} ngày";
            }

            if (remainingDays <= 7)
            {
                statusColor = "#F6A623";
                return $"Còn {remainingDays} ngày";
            }

            return $"Còn {remainingDays} ngày";
        }

        private static string BuildInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "NV";
            }

            var tokens = name.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return "NV";
            }

            if (tokens.Length == 1)
            {
                var token = tokens[0];
                return token.Substring(0, Math.Min(2, token.Length)).ToUpperInvariant();
            }

            var first = tokens[0][0];
            var last = tokens[^1][0];
            return string.Concat(first, last).ToString().ToUpperInvariant();
        }

        public int GetAreaCount()
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Areas;";
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        public string GetNextRecordCode()
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return string.Empty;
            }

            using var connection = OpenConnection();
            return GenerateNextRecordCode(connection, null, DateTime.Today.Year);
        }

        public SimilarRecordMatch FindSimilarRecord(RecordFormDraft record, int dateRangeDays = 30)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<SimilarRecordMatch>("records/similar", new SimilarRecordRequest { Record = record, DateRangeDays = dateRangeDays });
            }

            if (record == null ||
                string.IsNullOrWhiteSpace(record.SenderName) ||
                string.IsNullOrWhiteSpace(record.AreaName) ||
                string.IsNullOrWhiteSpace(record.CaseType))
            {
                return null;
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            var receivedDate = ParseDisplayDate(record.ReceivedDate).Date;
            var fromDate = receivedDate.AddDays(-Math.Abs(dateRangeDays));
            var toDate = receivedDate.AddDays(Math.Abs(dateRangeDays) + 1).AddTicks(-1);
            var conditions = new List<string>
            {
                "SenderName = $senderName",
                "AreaName = $areaName",
                "CaseType = $caseType",
                "ReceivedDate >= $fromDate",
                "ReceivedDate <= $toDate"
            };

            command.Parameters.AddWithValue("$senderName", NormalizeDbText(record.SenderName));
            command.Parameters.AddWithValue("$areaName", NormalizeDbText(record.AreaName));
            command.Parameters.AddWithValue("$caseType", NormalizeDbText(record.CaseType));
            command.Parameters.AddWithValue("$fromDate", fromDate.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$toDate", toDate.ToString("O", CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(record.SenderPhone))
            {
                conditions.Add("SenderPhone = $senderPhone");
                command.Parameters.AddWithValue("$senderPhone", NormalizeDbText(record.SenderPhone));
            }

            command.CommandText = $@"
SELECT RecordCode, ReceivedDate, SenderName, SenderPhone, AreaName, CaseType, Status
FROM Records
WHERE {string.Join(" AND ", conditions)}
ORDER BY ReceivedDate DESC, Id DESC
LIMIT 1;";

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new SimilarRecordMatch
            {
                RecordCode = reader.GetString(0),
                ReceivedDate = FormatDate(reader.GetString(1)),
                SenderName = reader.GetString(2),
                SenderPhone = reader.GetString(3),
                AreaName = reader.GetString(4),
                CaseType = reader.GetString(5),
                Status = reader.GetString(6)
            };
        }

        public IReadOnlyList<DashboardMetric> GetDashboardMetrics(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            DateTime? previousFromDate = null,
            DateTime? previousToDate = null)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<DashboardMetric>>("dashboard/metrics", new DashboardMetricsRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    PreviousFromDate = previousFromDate,
                    PreviousToDate = previousToDate
                });
            }

            using var connection = OpenConnection();
            var culture = CultureInfo.GetCultureInfo("vi-VN");
            var total = CountRecords(connection, fromDate, toDate);
            var processing = CountRecordsByStatuses(connection, fromDate, toDate, "Đang phân loại", "Đã phân công", "Đang xác minh", "Đang xử lý");
            var resolved = CountRecordsByStatuses(connection, fromDate, toDate, "Đã giải quyết");
            var waiting = CountRecordsByStatuses(connection, fromDate, toDate, "Chờ kết quả", "Đang chờ bổ sung tài liệu");
            var previousTotal = CountRecords(connection, previousFromDate, previousToDate);
            var previousProcessing = CountRecordsByStatuses(connection, previousFromDate, previousToDate, "Đang phân loại", "Đã phân công", "Đang xác minh", "Đang xử lý");
            var previousResolved = CountRecordsByStatuses(connection, previousFromDate, previousToDate, "Đã giải quyết");
            var previousWaiting = CountRecordsByStatuses(connection, previousFromDate, previousToDate, "Chờ kết quả", "Đang chờ bổ sung tài liệu");

            return new List<DashboardMetric>
            {
                CreateDashboardMetric("TỔNG HỒ SƠ", total, previousTotal, "\uE8A5", "#0B5CFF", culture),
                CreateDashboardMetric("ĐANG XỬ LÝ", processing, previousProcessing, "\uE823", "#F28C18", culture),
                CreateDashboardMetric("ĐÃ GIẢI QUYẾT", resolved, previousResolved, "\uE73E", "#1FA24A", culture),
                CreateDashboardMetric("CHỜ KẾT QUẢ", waiting, previousWaiting, "\uE916", "#7147D8", culture)
            };
        }

        public IReadOnlyList<StatusStat> GetStatusStats(DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<StatusStat>>("dashboard/status", new DateRangeRequest { FromDate = fromDate, ToDate = toDate });
            }

            using var connection = OpenConnection();
            var total = Math.Max(CountRecords(connection, fromDate, toDate), 1);
            var result = new List<StatusStat>();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT Status, COUNT(*)
FROM Records
{BuildScopedDateWhere(command, fromDate, toDate)}
GROUP BY Status
ORDER BY COUNT(*) DESC, Status;";
            AddDateParameters(command, fromDate, toDate);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var status = reader.GetString(0);
                var count = reader.GetInt32(1);
                result.Add(new StatusStat
                {
                    Name = status,
                    Count = count,
                    Percentage = $"{count * 100.0 / total:0.0}%",
                    Width = Math.Max(8, (int)Math.Round(count * 130.0 / total)),
                    Color = GetStatusColor(status)
                });
            }

            var startAngle = -90.0;
            foreach (var item in result)
            {
                item.StartAngle = startAngle;
                item.SweepAngle = item.Count * 360.0 / total;
                startAngle += item.SweepAngle;
            }

            return result;
        }

        private static DashboardMetric CreateDashboardMetric(string title, int value, int previousValue, string iconGlyph, string accentColor, CultureInfo culture)
        {
            var difference = value - previousValue;
            var absoluteDifference = Math.Abs(difference);
            return new DashboardMetric
            {
                Title = title,
                Value = value.ToString("N0", culture),
                DeltaAmount = absoluteDifference.ToString("N0", culture),
                DeltaDescription = "hồ sơ so với kỳ trước",
                Delta = $"{absoluteDifference.ToString("N0", culture)} hồ sơ so với kỳ trước",
                DeltaGlyph = difference > 0 ? "▲" : difference < 0 ? "▼" : "→",
                DeltaColor = difference > 0 ? "#0D8A2A" : difference < 0 ? "#E11414" : "#63708A",
                IconGlyph = iconGlyph,
                AccentColor = accentColor
            };
        }

        public IReadOnlyList<AreaStat> GetTopAreas(int take = 5, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<AreaStat>>("dashboard/areas", new TopAreasRequest { Take = take, FromDate = fromDate, ToDate = toDate });
            }

            using var connection = OpenConnection();
            var rows = new List<(string AreaName, int Count)>();
            using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT AreaName, COUNT(*) AS Total
FROM Records
{BuildScopedDateWhere(command, fromDate, toDate)}
GROUP BY AreaName
ORDER BY Total DESC, AreaName
LIMIT $take;";
            command.Parameters.AddWithValue("$take", take);
            AddDateParameters(command, fromDate, toDate);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add((reader.GetString(0), reader.GetInt32(1)));
            }

            var max = rows.Count == 0 ? 1 : Math.Max(1, rows[0].Count);
            var result = new List<AreaStat>();
            foreach (var row in rows)
            {
                result.Add(new AreaStat
                {
                    AreaName = row.AreaName,
                    Count = row.Count,
                    Width = Math.Max(36, (int)Math.Round(row.Count * 270.0 / max))
                });
            }

            return result;
        }

        public IReadOnlyList<TrendStat> GetReceivedTrendStats(DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<TrendStat>>("dashboard/trend", new DateRangeRequest { FromDate = fromDate, ToDate = toDate });
            }

            using var connection = OpenConnection();
            var records = new List<(DateTime ReceivedDate, bool IsResolved)>();
            using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT ReceivedDate, Status
FROM Records
{BuildScopedDateWhere(command, fromDate, toDate)}
ORDER BY ReceivedDate;";
            AddDateParameters(command, fromDate, toDate);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (DateTime.TryParse(reader.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
                {
                    records.Add((date.Date, reader.GetString(1) == "Đã giải quyết"));
                }
            }

            var startDate = fromDate?.Date ?? (records.Count == 0 ? DateTime.Today.AddDays(-6) : records.Min(item => item.ReceivedDate));
            var endDate = toDate?.Date ?? (records.Count == 0 ? DateTime.Today : records.Max(item => item.ReceivedDate));
            if (endDate < startDate)
            {
                endDate = startDate;
            }

            return (endDate - startDate).TotalDays <= 31
                ? BuildDailyTrend(records, startDate, endDate)
                : BuildMonthlyTrend(records, startDate, endDate);
        }

        public IReadOnlyList<RecentRecord> GetRecentRecords(int take = 8, DateTime? fromDate = null, DateTime? toDate = null, int skip = 0)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<RecentRecord>>("dashboard/recent", new RecentRecordsRequest { Take = take, FromDate = fromDate, ToDate = toDate, Skip = skip });
            }

            using var connection = OpenConnection();
            var result = new List<RecentRecord>();
            using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT RecordCode, SenderName, AreaName, Status, UpdatedAt, ProcessorName
FROM Records
{BuildScopedDateWhere(command, fromDate, toDate)}
ORDER BY UpdatedAt DESC, Id DESC
LIMIT $take OFFSET $skip;";
            command.Parameters.AddWithValue("$take", take);
            command.Parameters.AddWithValue("$skip", skip);
            AddDateParameters(command, fromDate, toDate);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new RecentRecord
                {
                    RecordCode = reader.GetString(0),
                    SenderName = reader.GetString(1),
                    AreaName = reader.GetString(2),
                    Status = reader.GetString(3),
                    UpdatedAt = FormatDateTime(reader.GetString(4)),
                    ProcessorName = reader.GetString(5)
                });
            }

            return result;
        }

        public IReadOnlyList<RecentRecord> GetFilteredRecords(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string status = null,
            string caseType = null,
            string field = null,
            string areaName = null,
            string processorName = null,
            string searchText = null,
            string sortOption = null,
            int take = 20,
            int skip = 0)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<RecentRecord>>("records/list", new FilteredRecordsRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Status = status,
                    CaseType = caseType,
                    Field = field,
                    AreaName = areaName,
                    ProcessorName = processorName,
                    SearchText = searchText,
                    SortOption = sortOption,
                    Take = take,
                    Skip = skip
                });
            }

            using var connection = OpenConnection();
            var result = new List<RecentRecord>();
            using var command = connection.CreateCommand();
            var whereClause = BuildExportWhere(command, fromDate, toDate, status, caseType, field, areaName, processorName, searchText, applyUserScope: true);
            command.CommandText = $@"
SELECT RecordCode, SenderName, AreaName, CaseType, Field, ReceivedDate, Status, UpdatedAt, ProcessorName
FROM Records
{whereClause}
ORDER BY {BuildExportOrderBy(sortOption)}
LIMIT $take OFFSET $skip;";
            command.Parameters.AddWithValue("$take", Math.Max(1, take));
            command.Parameters.AddWithValue("$skip", Math.Max(0, skip));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new RecentRecord
                {
                    RecordCode = reader.GetString(0),
                    SenderName = reader.GetString(1),
                    AreaName = reader.GetString(2),
                    CaseType = reader.GetString(3),
                    Field = reader.GetString(4),
                    ReceivedDate = FormatDate(reader.GetString(5)),
                    Status = reader.GetString(6),
                    UpdatedAt = FormatDateTime(reader.GetString(7)),
                    ProcessorName = reader.GetString(8)
                });
            }

            return result;
        }

        public RecordFormDraft GetLatestRecordForm()
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Id, RecordCode, ReceivedDate, ReceiveSource, ReceiverName, SenderName, SenderPhone, ContactAddress,
       AreaName, IncidentAddress, Content, CaseType, ContentGroup, Field, RelatedPerson,
       ExpectedHandlingMethod, SenderExpectedHandlingMethod, SeverityLevel, ExpectedResultDate, Note, AdditionalNote
FROM Records
ORDER BY UpdatedAt DESC, Id DESC
LIMIT 1;";
            return ReadRecordForm(connection, command);
        }

        public RecordFormDraft GetRecordForm(string recordCode)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<RecordFormDraft>("records/detail", new RecordCodeRequest { RecordCode = recordCode });
            }

            if (string.IsNullOrWhiteSpace(recordCode))
            {
                return new RecordFormDraft();
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Id, RecordCode, ReceivedDate, ReceiveSource, ReceiverName, SenderName, SenderPhone, ContactAddress,
       AreaName, IncidentAddress, Content, CaseType, ContentGroup, Field, RelatedPerson,
       ExpectedHandlingMethod, SenderExpectedHandlingMethod, SeverityLevel, ExpectedResultDate, Note, AdditionalNote
FROM Records
WHERE RecordCode = $recordCode
LIMIT 1;";
            command.Parameters.AddWithValue("$recordCode", recordCode);
            var record = ReadRecordForm(connection, command);
            return AuthContext.CanAccessRecord(GetRecordProcessorName(connection, recordCode))
                ? record
                : new RecordFormDraft();
        }

        public string SaveRecordForm(RecordFormDraft record, string originalRecordCode = null)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<string>("records/save", new SaveRecordFormRequest { Record = record, OriginalRecordCode = originalRecordCode });
            }

            if (record == null)
            {
                return string.Empty;
            }

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            var recordId = string.IsNullOrWhiteSpace(originalRecordCode)
                ? null
                : GetRecordId(connection, transaction, originalRecordCode);
            if (recordId.HasValue)
            {
                EnsureCanEditRecord(connection, transaction, recordId.Value);
            }
            var now = DateTime.Now.ToString("O", CultureInfo.InvariantCulture);
            var savedRecordCode = NormalizeDbText(record.RecordCode);

            if (recordId.HasValue)
            {
                using var updateCommand = connection.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = @"
UPDATE Records
SET RecordCode = $recordCode,
    ReceivedDate = $receivedDate,
    ReceiveSource = $receiveSource,
    ReceiverName = $receiverName,
    SenderName = $senderName,
    SenderPhone = $senderPhone,
    ContactAddress = $contactAddress,
    AreaName = $areaName,
    IncidentAddress = $incidentAddress,
    Content = $content,
    CaseType = $caseType,
    ContentGroup = $contentGroup,
    Field = $field,
    RelatedPerson = $relatedPerson,
    ExpectedHandlingMethod = $method,
    SenderExpectedHandlingMethod = $senderMethod,
    SeverityLevel = $severity,
    ExpectedResultDate = $expectedDate,
    Note = $note,
    AdditionalNote = $additionalNote,
    UpdatedAt = $updatedAt
WHERE Id = $recordId;";
                updateCommand.Parameters.AddWithValue("$recordId", recordId.Value);
                AddRecordFormParameters(updateCommand, record, now);
                updateCommand.ExecuteNonQuery();
                ReplaceAttachments(connection, transaction, recordId.Value, record.Attachments);
                WriteDatabaseLog(connection, transaction, "Hồ sơ", "Sửa", savedRecordCode, $"Cập nhật hồ sơ {savedRecordCode}.");
            }
            else
            {
                EnsureCanCreateRecords();
                savedRecordCode = GenerateNextRecordCode(connection, transaction, DateTime.Today.Year);
                record.RecordCode = savedRecordCode;
                using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = @"
INSERT INTO Records (
    RecordCode, ReceivedDate, ReceiveSource, ReceiverName, SenderName, SenderPhone, ContactAddress,
    AreaName, IncidentAddress, Content, CaseType, ContentGroup, Field, RelatedPerson,
    ExpectedHandlingMethod, SenderExpectedHandlingMethod, SeverityLevel, ExpectedResultDate, Status, ProcessorName,
    Note, AdditionalNote, CreatedAt, UpdatedAt)
VALUES (
    $recordCode, $receivedDate, $receiveSource, $receiverName, $senderName, $senderPhone, $contactAddress,
    $areaName, $incidentAddress, $content, $caseType, $contentGroup, $field, $relatedPerson,
    $method, $senderMethod, $severity, $expectedDate, $status, $processor,
    $note, $additionalNote, $createdAt, $updatedAt);
SELECT last_insert_rowid();";
                AddRecordFormParameters(insertCommand, record, now);
                insertCommand.Parameters.AddWithValue("$status", "Mới tiếp nhận");
                insertCommand.Parameters.AddWithValue("$processor", AuthContext.IsOfficer ? AuthContext.CurrentDisplayName : NormalizeDbText(record.ReceiverName));
                insertCommand.Parameters.AddWithValue("$createdAt", now);
                var insertedId = Convert.ToInt32(insertCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
                ReplaceAttachments(connection, transaction, insertedId, record.Attachments);
                WriteDatabaseLog(connection, transaction, "Hồ sơ", "Thêm", savedRecordCode, $"Thêm mới hồ sơ {savedRecordCode}.");
            }

            transaction.Commit();
            return savedRecordCode;
        }

        public bool DeleteRecord(string recordCode)
        {
            EnsureCanDeleteRecords();
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<bool>("records/delete", new RecordCodeRequest { RecordCode = recordCode });
            }

            if (string.IsNullOrWhiteSpace(recordCode))
            {
                return false;
            }

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using var selectCommand = connection.CreateCommand();
            selectCommand.Transaction = transaction;
            selectCommand.CommandText = "SELECT Id FROM Records WHERE RecordCode = $recordCode LIMIT 1;";
            selectCommand.Parameters.AddWithValue("$recordCode", recordCode);

            var recordIdValue = selectCommand.ExecuteScalar();
            if (recordIdValue == null)
            {
                transaction.Commit();
                return false;
            }

            var recordId = Convert.ToInt32(recordIdValue, CultureInfo.InvariantCulture);
            using var deleteHistoriesCommand = connection.CreateCommand();
            deleteHistoriesCommand.Transaction = transaction;
            deleteHistoriesCommand.CommandText = "DELETE FROM ProcessHistories WHERE RecordId = $recordId;";
            deleteHistoriesCommand.Parameters.AddWithValue("$recordId", recordId);
            deleteHistoriesCommand.ExecuteNonQuery();

            using var deleteAttachmentsCommand = connection.CreateCommand();
            deleteAttachmentsCommand.Transaction = transaction;
            deleteAttachmentsCommand.CommandText = "DELETE FROM RecordAttachments WHERE RecordId = $recordId;";
            deleteAttachmentsCommand.Parameters.AddWithValue("$recordId", recordId);
            deleteAttachmentsCommand.ExecuteNonQuery();

            using var deleteRecordCommand = connection.CreateCommand();
            deleteRecordCommand.Transaction = transaction;
            deleteRecordCommand.CommandText = "DELETE FROM Records WHERE Id = $recordId;";
            deleteRecordCommand.Parameters.AddWithValue("$recordId", recordId);
            deleteRecordCommand.ExecuteNonQuery();
            WriteDatabaseLog(connection, transaction, "Hồ sơ", "Xóa", recordCode, $"Xóa hồ sơ {recordCode} và dữ liệu liên quan.");

            transaction.Commit();
            return true;
        }

        private static int? GetRecordId(SqliteConnection connection, SqliteTransaction transaction, string recordCode)
        {
            if (string.IsNullOrWhiteSpace(recordCode))
            {
                return null;
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT Id FROM Records WHERE RecordCode = $recordCode LIMIT 1;";
            command.Parameters.AddWithValue("$recordCode", recordCode);
            var value = command.ExecuteScalar();
            return value == null ? (int?)null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static void AddRecordFormParameters(SqliteCommand command, RecordFormDraft record, string updatedAt)
        {
            command.Parameters.AddWithValue("$recordCode", NormalizeDbText(record.RecordCode));
            command.Parameters.AddWithValue("$receivedDate", ParseDisplayDate(record.ReceivedDate).ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$receiveSource", NormalizeDbText(record.ReceiveSource));
            command.Parameters.AddWithValue("$receiverName", NormalizeDbText(record.ReceiverName));
            command.Parameters.AddWithValue("$senderName", NormalizeDbText(record.SenderName));
            command.Parameters.AddWithValue("$senderPhone", NormalizeDbText(record.SenderPhone));
            command.Parameters.AddWithValue("$contactAddress", NormalizeDbText(record.ContactAddress));
            command.Parameters.AddWithValue("$areaName", NormalizeDbText(record.AreaName));
            command.Parameters.AddWithValue("$incidentAddress", NormalizeDbText(record.IncidentAddress));
            command.Parameters.AddWithValue("$content", NormalizeDbText(record.Content));
            command.Parameters.AddWithValue("$caseType", NormalizeDbText(record.CaseType));
            command.Parameters.AddWithValue("$contentGroup", NormalizeDbText(record.ContentGroup));
            command.Parameters.AddWithValue("$field", NormalizeDbText(record.Field));
            command.Parameters.AddWithValue("$relatedPerson", NormalizeDbText(record.RelatedPerson));
            command.Parameters.AddWithValue("$method", NormalizeDbText(record.ExpectedHandlingMethod));
            command.Parameters.AddWithValue("$senderMethod", NormalizeDbText(record.SenderExpectedHandlingMethod));
            command.Parameters.AddWithValue("$severity", NormalizeDbText(record.SeverityLevel));
            command.Parameters.AddWithValue("$expectedDate", string.IsNullOrWhiteSpace(record.ExpectedResultDate)
                ? string.Empty
                : ParseDisplayDate(record.ExpectedResultDate).ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$note", NormalizeDbText(record.Note));
            command.Parameters.AddWithValue("$additionalNote", NormalizeDbText(record.AdditionalNote));
            command.Parameters.AddWithValue("$updatedAt", updatedAt);
        }

        private static void ReplaceAttachments(SqliteConnection connection, SqliteTransaction transaction, int recordId, IReadOnlyList<AttachmentDraft> attachments)
        {
            using var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM RecordAttachments WHERE RecordId = $recordId;";
            deleteCommand.Parameters.AddWithValue("$recordId", recordId);
            deleteCommand.ExecuteNonQuery();

            if (attachments == null)
            {
                return;
            }

            foreach (var attachment in attachments)
            {
                using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = "INSERT INTO RecordAttachments (RecordId, FileName, FileSize, FilePath) VALUES ($recordId, $fileName, $fileSize, $filePath);";
                insertCommand.Parameters.AddWithValue("$recordId", recordId);
                insertCommand.Parameters.AddWithValue("$fileName", NormalizeDbText(attachment.FileName));
                insertCommand.Parameters.AddWithValue("$fileSize", NormalizeDbText(attachment.FileSize));
                insertCommand.Parameters.AddWithValue("$filePath", NormalizeDbText(attachment.FilePath));
                insertCommand.ExecuteNonQuery();
            }
        }

        private IReadOnlyList<AttachmentDraft> MergeInitialResultDocuments(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int recordId,
            string recordCode,
            DateTime processedAt,
            IReadOnlyList<AttachmentDraft> attachments)
        {
            var result = new List<AttachmentDraft>(attachments ?? Array.Empty<AttachmentDraft>());
            foreach (var attachment in BuildInitialResultDocuments(connection, transaction, recordId, recordCode, processedAt.ToString("O", CultureInfo.InvariantCulture)))
            {
                var existingIndex = result.FindIndex(item => string.Equals(item.FileName, attachment.FileName, StringComparison.OrdinalIgnoreCase));
                if (existingIndex >= 0)
                {
                    result[existingIndex] = attachment;
                }
                else
                {
                    result.Add(attachment);
                }
            }

            return result;
        }

        private void EnsureInitialResultDocuments(SqliteConnection connection, SqliteTransaction transaction, int recordId, string recordCode, string processingDate)
        {
            foreach (var attachment in BuildInitialResultDocuments(connection, transaction, recordId, recordCode, processingDate))
            {
                InsertAttachmentIfMissing(connection, transaction, recordId, attachment);
            }
        }

        private IReadOnlyList<AttachmentDraft> BuildInitialResultDocuments(SqliteConnection connection, SqliteTransaction transaction, int recordId, string recordCode, string processingDate)
        {
            try
            {
                var record = ReadRecordFormById(connection, transaction, recordId);
                return InitialResultDocumentGenerator.Generate(record, recordCode, processingDate, GetGeneratedDocumentsRoot());
            }
            catch (Exception ex)
            {
                AppLogger.Error("Documents", "GenerateInitialResultDocuments", ex, "Failed to generate initial result Word documents.", recordCode);
                return Array.Empty<AttachmentDraft>();
            }
        }

        private static void InsertAttachmentIfMissing(SqliteConnection connection, SqliteTransaction transaction, int recordId, AttachmentDraft attachment)
        {
            if (attachment == null || string.IsNullOrWhiteSpace(attachment.FileName))
            {
                return;
            }

            using var existsCommand = connection.CreateCommand();
            existsCommand.Transaction = transaction;
            existsCommand.CommandText = "SELECT Id FROM RecordAttachments WHERE RecordId = $recordId AND FileName = $fileName LIMIT 1;";
            existsCommand.Parameters.AddWithValue("$recordId", recordId);
            existsCommand.Parameters.AddWithValue("$fileName", NormalizeDbText(attachment.FileName));
            var existingId = existsCommand.ExecuteScalar();
            if (existingId != null)
            {
                using var updateCommand = connection.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = "UPDATE RecordAttachments SET FileSize = $fileSize, FilePath = $filePath WHERE Id = $id;";
                updateCommand.Parameters.AddWithValue("$id", Convert.ToInt32(existingId, CultureInfo.InvariantCulture));
                updateCommand.Parameters.AddWithValue("$fileSize", NormalizeDbText(attachment.FileSize));
                updateCommand.Parameters.AddWithValue("$filePath", NormalizeDbText(attachment.FilePath));
                updateCommand.ExecuteNonQuery();
                return;
            }

            using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = "INSERT INTO RecordAttachments (RecordId, FileName, FileSize, FilePath) VALUES ($recordId, $fileName, $fileSize, $filePath);";
            insertCommand.Parameters.AddWithValue("$recordId", recordId);
            insertCommand.Parameters.AddWithValue("$fileName", NormalizeDbText(attachment.FileName));
            insertCommand.Parameters.AddWithValue("$fileSize", NormalizeDbText(attachment.FileSize));
            insertCommand.Parameters.AddWithValue("$filePath", NormalizeDbText(attachment.FilePath));
            insertCommand.ExecuteNonQuery();
        }

        private static string GetGeneratedDocumentsRoot()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuanLyHoSo",
                "GeneratedDocuments");
        }

        private static string GetDefaultBackupFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuanLyHoSo",
                "Backup");
        }

        private static string NormalizeDbText(string value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private RecordFormDraft ReadRecordForm(SqliteConnection connection, SqliteCommand command)
        {

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return new RecordFormDraft();
            }

            var recordId = reader.GetInt32(0);
            return new RecordFormDraft
            {
                RecordCode = reader.GetString(1),
                ReceivedDate = FormatDate(reader.GetString(2)),
                ReceiveSource = reader.GetString(3),
                ReceiverName = reader.GetString(4),
                SenderName = reader.GetString(5),
                SenderPhone = reader.GetString(6),
                ContactAddress = reader.GetString(7),
                AreaName = reader.GetString(8),
                IncidentAddress = reader.GetString(9),
                Content = reader.GetString(10),
                CaseType = reader.GetString(11),
                ContentGroup = reader.GetString(12),
                Field = reader.GetString(13),
                RelatedPerson = reader.GetString(14),
                ExpectedHandlingMethod = reader.GetString(15),
                SenderExpectedHandlingMethod = reader.GetString(16),
                SeverityLevel = reader.GetString(17),
                ExpectedResultDate = FormatDate(reader.GetString(18)),
                Note = reader.GetString(19),
                AdditionalNote = reader.GetString(20),
                Attachments = GetAttachments(connection, recordId)
            };
        }

        private RecordFormDraft ReadRecordFormById(SqliteConnection connection, SqliteTransaction transaction, int recordId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
SELECT Id, RecordCode, ReceivedDate, ReceiveSource, ReceiverName, SenderName, SenderPhone, ContactAddress,
       AreaName, IncidentAddress, Content, CaseType, ContentGroup, Field, RelatedPerson,
       ExpectedHandlingMethod, SenderExpectedHandlingMethod, SeverityLevel, ExpectedResultDate, Note, AdditionalNote
FROM Records
WHERE Id = $recordId
LIMIT 1;";
            command.Parameters.AddWithValue("$recordId", recordId);
            return ReadRecordForm(connection, command);
        }

        public ProcessingRecordDetail GetProcessingRecordDetail(string recordCode = null)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<ProcessingRecordDetail>("processing/detail", new RecordCodeRequest { RecordCode = recordCode });
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            var conditions = new List<string>
            {
                string.IsNullOrWhiteSpace(recordCode) ? "Status <> 'Đã giải quyết'" : "RecordCode = $recordCode"
            };
            ApplyUserRecordScope(command, conditions);
            command.CommandText = $@"
SELECT Id, RecordCode, ReceivedDate, ReceiveSource, SenderName, SenderPhone, AreaName, CaseType, Field, Status, ProcessorName, UpdatedAt
FROM Records
WHERE {string.Join(" AND ", conditions)}
ORDER BY UpdatedAt DESC, Id DESC
LIMIT 1;";
            if (!string.IsNullOrWhiteSpace(recordCode))
            {
                command.Parameters.AddWithValue("$recordCode", recordCode);
            }

            int recordId;
            string recordCodeValue;
            string receivedDate;
            string receiveSource;
            string senderName;
            string senderPhone;
            string areaName;
            string caseType;
            string field;
            string status;
            string processorName;
            string processingDate;
            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return new ProcessingRecordDetail();
                }

                recordId = reader.GetInt32(0);
                recordCodeValue = reader.GetString(1);
                receivedDate = FormatDate(reader.GetString(2));
                receiveSource = reader.GetString(3);
                senderName = reader.GetString(4);
                senderPhone = reader.GetString(5);
                areaName = reader.GetString(6);
                caseType = reader.GetString(7);
                field = reader.GetString(8);
                status = reader.GetString(9);
                processorName = reader.GetString(10);
                processingDate = FormatDateTime(reader.GetString(11));
            }

            var history = GetProcessHistory(connection, recordId, status);
            return new ProcessingRecordDetail
            {
                RecordCode = recordCodeValue,
                ReceivedDate = receivedDate,
                ReceiveSource = receiveSource,
                SenderName = senderName,
                SenderPhone = senderPhone,
                AreaName = areaName,
                CaseType = caseType,
                Field = field,
                Status = status,
                ProcessorName = processorName,
                ProcessingDate = processingDate,
                ProcessContent = "Đang cập nhật tiến độ xử lý hồ sơ theo thông tin từ cơ sở dữ liệu.",
                ProcessNote = "Dữ liệu mẫu được seed tự động cho giai đoạn thiết kế giao diện.",
                Attachments = GetAttachments(connection, recordId),
                Steps = BuildProcessSteps(status, history),
                History = history
            };
        }

        public void UpdateProcessingRecord(string recordCode, string status, DateTime processedAt, string processorName, string content, string note, string transferAreaName, IReadOnlyList<AttachmentDraft> attachments, bool generateInitialResultDocuments = false)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                _lanClient.Call<bool>("processing/update", new UpdateProcessingRequest
                {
                    RecordCode = recordCode,
                    Status = status,
                    ProcessedAt = processedAt,
                    ProcessorName = processorName,
                    Content = content,
                    Note = note,
                    TransferAreaName = transferAreaName,
                    Attachments = attachments,
                    GenerateInitialResultDocuments = generateInitialResultDocuments
                });
                return;
            }

            EnsureCanWriteRecords();
            if (string.IsNullOrWhiteSpace(recordCode))
            {
                return;
            }

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            var recordId = GetRecordId(connection, transaction, recordCode);
            if (!recordId.HasValue)
            {
                transaction.Commit();
                return;
            }

            EnsureCanEditRecord(connection, transaction, recordId.Value);
            EnsureCanUpdateProcessingStatus(status);

            using var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = @"
UPDATE Records
SET Status = $status,
    AreaName = CASE WHEN $transferAreaName = '' THEN AreaName ELSE $transferAreaName END,
    ProcessorName = $processor,
    Note = CASE WHEN $note = '' THEN Note ELSE $note END,
    UpdatedAt = $updatedAt
WHERE Id = $recordId;";
            updateCommand.Parameters.AddWithValue("$recordId", recordId.Value);
            updateCommand.Parameters.AddWithValue("$status", NormalizeDbText(status));
            updateCommand.Parameters.AddWithValue("$processor", NormalizeDbText(processorName));
            updateCommand.Parameters.AddWithValue("$transferAreaName", NormalizeDbText(transferAreaName));
            updateCommand.Parameters.AddWithValue("$note", NormalizeDbText(note));
            updateCommand.Parameters.AddWithValue("$updatedAt", processedAt.ToString("O", CultureInfo.InvariantCulture));
            updateCommand.ExecuteNonQuery();
            var attachmentsToSave = generateInitialResultDocuments && GetProcessStepNumber(status) >= 5
                ? MergeInitialResultDocuments(connection, transaction, recordId.Value, recordCode, processedAt, attachments)
                : attachments;
            ReplaceAttachments(connection, transaction, recordId.Value, attachmentsToSave);

            var currentStep = GetProcessStepNumber(status);
            DeleteProcessHistoryFromStep(connection, transaction, recordId.Value, currentStep);

            for (var step = 1; step <= currentStep; step++)
            {
                var definition = GetProcessStepDefinition(step);
                var stepContent = step == currentStep
                    ? NormalizeDbText(content)
                    : $"Tự động ghi nhận bước {definition.Title.ToLower(CultureInfo.GetCultureInfo("vi-VN"))} khi hồ sơ được chuyển đến bước {status}.";
                var isCompleted = step < currentStep || currentStep >= 6;

                if (step < currentStep && HasProcessHistory(connection, transaction, recordId.Value, definition.Title))
                {
                    continue;
                }

                InsertProcessHistory(
                    connection,
                    transaction,
                    recordId.Value,
                    definition.Title,
                    processedAt,
                    NormalizeDbText(processorName),
                    stepContent,
                    isCompleted);
            }

            EnsureCatalogItem(connection, transaction, "ProcessorName", processorName);
            WriteDatabaseLog(connection, transaction, "Xử lý hồ sơ", "Sửa", recordCode, $"Cập nhật trạng thái hồ sơ thành \"{NormalizeDbText(status)}\".");

            transaction.Commit();
            NotifyCatalogChanged("ProcessorName");
        }

        private static IReadOnlyList<DashboardMetric> BuildProcessingQueueMetrics(SqliteConnection connection)
        {
            var allOpen = CountOpenProcessingRecords(connection);
            var needClassify = CountRecordsByStatuses(connection, null, null, "Mới tiếp nhận", "Đang phân loại");
            var processing = CountRecordsByStatuses(connection, null, null, "Đã phân công", "Đang xác minh");
            var waiting = CountRecordsByStatuses(connection, null, null, "Chờ kết quả", "Đang chờ bổ sung tài liệu");
            var dueSoon = CountDueSoonOpenRecords(connection);
            var overdue = CountOverdueOpenRecords(connection);
            var highPriority = CountHighPriorityOpenRecords(connection);
            var culture = CultureInfo.GetCultureInfo("vi-VN");

            return new List<DashboardMetric>
            {
                new DashboardMetric { Title = "TẤT CẢ", Value = allOpen.ToString("N0", culture), Delta = "Hồ sơ đang mở", IconGlyph = "\uE8FD", AccentColor = "#2E3A59", FilterKey = "All" },
                new DashboardMetric { Title = "CẦN PHÂN LOẠI", Value = needClassify.ToString("N0", culture), Delta = "Hồ sơ mới/chưa phân loại", IconGlyph = "\uE8F1", AccentColor = "#0B5CFF", FilterKey = "NeedClassify" },
                new DashboardMetric { Title = "ĐANG XỬ LÝ", Value = processing.ToString("N0", culture), Delta = "Đã phân công, xác minh", IconGlyph = "\uE823", AccentColor = "#F28C18", FilterKey = "Processing" },
                new DashboardMetric { Title = "CHỜ BỔ SUNG", Value = waiting.ToString("N0", culture), Delta = "Đang chờ tài liệu/kết quả", IconGlyph = "\uE916", AccentColor = "#7147D8", FilterKey = "Waiting" },
                new DashboardMetric { Title = "SẮP ĐẾN HẠN", Value = dueSoon.ToString("N0", culture), Delta = "Còn hạn trong 7 ngày", IconGlyph = "\uE8A7", AccentColor = "#008A8A", FilterKey = "DueSoon" },
                new DashboardMetric { Title = "QUÁ HẠN", Value = overdue.ToString("N0", culture), Delta = "Chưa hoàn tất theo hạn", IconGlyph = "\uE7BA", AccentColor = "#D13438", FilterKey = "Overdue" },
                new DashboardMetric { Title = "MỨC ĐỘ CAO", Value = highPriority.ToString("N0", culture), Delta = "Ưu tiên/khẩn cần theo dõi", IconGlyph = "\uE7BF", AccentColor = "#B146C2", FilterKey = "HighPriority" }
            };
        }

        public IReadOnlyList<DashboardMetric> GetProcessingQueueMetrics()
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<DashboardMetric>>("processing/metrics", new { });
            }

            using var connection = OpenConnection();
            return BuildProcessingQueueMetrics(connection);
        }

        public IReadOnlyList<ProcessingQueueRecord> GetProcessingQueueRecords(
            string searchText = null,
            string status = null,
            string areaName = null,
            string severityLevel = null,
            string cardFilterKey = null,
            int skip = 0,
            int take = 20)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<ProcessingQueueRecord>>("processing/list", new ProcessingQueueRequest
                {
                    SearchText = searchText,
                    Status = status,
                    AreaName = areaName,
                    SeverityLevel = severityLevel,
                    CardFilterKey = cardFilterKey,
                    Skip = skip,
                    Take = take
                });
            }

            var stopwatch = Stopwatch.StartNew();
            using var connection = OpenConnection();
            var result = new List<ProcessingQueueRecord>();
            using var command = connection.CreateCommand();
            var conditions = new List<string> { "Status <> 'Đã giải quyết'" };
            ApplyUserRecordScope(command, conditions);
            AddProcessingCardFilter(command, conditions, cardFilterKey);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                conditions.Add("(RecordCode LIKE $search OR SenderName LIKE $search OR Content LIKE $search)");
                command.Parameters.AddWithValue("$search", $"%{searchText.Trim()}%");
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                conditions.Add("Status = $status");
                command.Parameters.AddWithValue("$status", status);
            }

            AddOptionalAreaFilter(command, conditions, areaName);

            if (!string.IsNullOrWhiteSpace(severityLevel) && severityLevel != "Tất cả")
            {
                conditions.Add("SeverityLevel = $severityLevel");
                command.Parameters.AddWithValue("$severityLevel", severityLevel);
            }

            command.CommandText = $@"
SELECT RecordCode, ReceivedDate, SenderName, AreaName, CaseType, SeverityLevel, Status, UpdatedAt, ExpectedResultDate
FROM Records
WHERE {string.Join(" AND ", conditions)}
ORDER BY
  CASE
    WHEN ExpectedResultDate <> '' AND ExpectedResultDate < $todayOrder THEN 0
    WHEN ExpectedResultDate <> '' AND ExpectedResultDate <= $dueSoonOrder THEN 1
    WHEN Status IN ('Mới tiếp nhận', 'Đang phân loại') THEN 2
    ELSE 3
  END,
  ExpectedResultDate ASC,
  UpdatedAt DESC,
  Id DESC
LIMIT $take OFFSET $skip;";
            command.Parameters.AddWithValue("$take", Math.Max(1, take));
            command.Parameters.AddWithValue("$skip", Math.Max(0, skip));
            command.Parameters.AddWithValue("$todayOrder", DateTime.Today.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$dueSoonOrder", DateTime.Today.AddDays(7).Date.AddDays(1).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture));

            using var reader = command.ExecuteReader();
            var index = skip + 1;
            while (reader.Read())
            {
                var currentStatus = reader.GetString(6);
                var expectedResultDate = reader.GetString(8);
                result.Add(new ProcessingQueueRecord
                {
                    Index = index++,
                    RecordCode = reader.GetString(0),
                    ReceivedDate = FormatDate(reader.GetString(1)),
                    SenderName = reader.GetString(2),
                    AreaName = reader.GetString(3),
                    CaseType = reader.GetString(4),
                    SeverityLevel = reader.GetString(5),
                    Status = currentStatus,
                    UpdatedAt = FormatDateTime(reader.GetString(7)),
                    ExpectedResultDate = FormatDate(expectedResultDate),
                    DeadlineText = BuildProcessingDeadlineText(expectedResultDate),
                    DeadlineBrush = BuildProcessingDeadlineBrush(expectedResultDate),
                    WorkLabel = BuildProcessingWorkLabel(currentStatus)
                });
            }

            LogElapsed("Database", "GetProcessingQueueRecords", stopwatch, result.Count);
            return result;
        }

        public int CountProcessingQueueRecords(
            string searchText = null,
            string status = null,
            string areaName = null,
            string severityLevel = null,
            string cardFilterKey = null)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<int>("processing/count", new ProcessingQueueRequest
                {
                    SearchText = searchText,
                    Status = status,
                    AreaName = areaName,
                    SeverityLevel = severityLevel,
                    CardFilterKey = cardFilterKey
                });
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            var conditions = new List<string> { "Status <> 'Đã giải quyết'" };
            ApplyUserRecordScope(command, conditions);
            AddProcessingCardFilter(command, conditions, cardFilterKey);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                conditions.Add("(RecordCode LIKE $search OR SenderName LIKE $search OR Content LIKE $search)");
                command.Parameters.AddWithValue("$search", $"%{searchText.Trim()}%");
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                conditions.Add("Status = $status");
                command.Parameters.AddWithValue("$status", status);
            }

            AddOptionalAreaFilter(command, conditions, areaName);

            if (!string.IsNullOrWhiteSpace(severityLevel) && severityLevel != "Tất cả")
            {
                conditions.Add("SeverityLevel = $severityLevel");
                command.Parameters.AddWithValue("$severityLevel", severityLevel);
            }

            command.CommandText = $"SELECT COUNT(*) FROM Records WHERE {string.Join(" AND ", conditions)};";
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        public IReadOnlyList<ExportRecordPreview> GetExportPreview(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string status = null,
            string caseType = null,
            string field = null,
            string areaName = null,
            string processorName = null,
            string searchText = null,
            string sortOption = null,
            int take = 50)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<IReadOnlyList<ExportRecordPreview>>("records/export-preview", new FilteredRecordsRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Status = status,
                    CaseType = caseType,
                    Field = field,
                    AreaName = areaName,
                    ProcessorName = processorName,
                    SearchText = searchText,
                    SortOption = sortOption,
                    Take = take
                });
            }

            var stopwatch = Stopwatch.StartNew();
            using var connection = OpenConnection();
            var result = new List<ExportRecordPreview>();
            using var command = connection.CreateCommand();
            var whereClause = BuildExportWhere(command, fromDate, toDate, status, caseType, field, areaName, processorName, searchText, applyUserScope: true);
            command.CommandText = $@"
SELECT RecordCode, ReceivedDate, SenderName, AreaName, CaseType, Field, Status, UpdatedAt, ProcessorName
FROM Records
{whereClause}
ORDER BY {BuildExportOrderBy(sortOption)}
LIMIT $take;";
            command.Parameters.AddWithValue("$take", Math.Max(1, take));

            using var reader = command.ExecuteReader();
            var index = 1;
            while (reader.Read())
            {
                result.Add(new ExportRecordPreview
                {
                    Index = index++,
                    RecordCode = reader.GetString(0),
                    ReceivedDate = FormatDate(reader.GetString(1)),
                    SenderName = reader.GetString(2),
                    AreaName = reader.GetString(3),
                    CaseType = reader.GetString(4),
                    Field = reader.GetString(5),
                    Status = reader.GetString(6),
                    UpdatedAt = FormatDateTime(reader.GetString(7)),
                    ProcessorName = reader.GetString(8)
                });
            }

            LogElapsed("Database", "GetExportPreview", stopwatch, result.Count);
            return result;
        }

        public int CountExportRecords(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string status = null,
            string caseType = null,
            string field = null,
            string areaName = null,
            string processorName = null,
            string searchText = null)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<int>("records/export-count", new FilteredRecordsRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Status = status,
                    CaseType = caseType,
                    Field = field,
                    AreaName = areaName,
                    ProcessorName = processorName,
                    SearchText = searchText
                });
            }

            var stopwatch = Stopwatch.StartNew();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            var whereClause = BuildExportWhere(command, fromDate, toDate, status, caseType, field, areaName, processorName, searchText, applyUserScope: true);
            command.CommandText = $"SELECT COUNT(*) FROM Records {whereClause};";
            var count = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            LogElapsed("Database", "CountExportRecords", stopwatch, count);
            return count;
        }

        public int CountFilteredRecords(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string status = null,
            string caseType = null,
            string field = null,
            string areaName = null,
            string processorName = null,
            string searchText = null)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<int>("records/count", new FilteredRecordsRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Status = status,
                    CaseType = caseType,
                    Field = field,
                    AreaName = areaName,
                    ProcessorName = processorName,
                    SearchText = searchText
                });
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            var whereClause = BuildExportWhere(command, fromDate, toDate, status, caseType, field, areaName, processorName, searchText, applyUserScope: true);
            command.CommandText = $"SELECT COUNT(*) FROM Records {whereClause};";
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        public int CountRecords(DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<int>("records/total", new DateRangeRequest { FromDate = fromDate, ToDate = toDate });
            }

            using var connection = OpenConnection();
            return CountRecords(connection, fromDate, toDate);
        }

        public void BackupDatabase(string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException("Destination path is required.", nameof(destinationPath));
            }

            var destinationFolder = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            using var source = OpenConnection();
            using var destination = OpenDatabaseFile(destinationPath);
            source.BackupDatabase(destination);
            ValidateDatabaseConnection(destination);
        }

        public string CreateBackupFile(string fileName)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<string>("settings/backup/create", new CreateBackupRequest { FileName = fileName });
            }

            EnsureAdmin();
            var safeFileName = Path.GetFileName(string.IsNullOrWhiteSpace(fileName)
                ? $"quanlyhoso_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
                : fileName);
            var destinationPath = Path.Combine(GetDefaultBackupFolder(), safeFileName);
            BackupDatabase(destinationPath);
            return destinationPath;
        }

        public InternalUpdatePackageInfo GetInternalUpdatePackageInfo()
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                return _lanClient.Call<InternalUpdatePackageInfo>("settings/update/latest", null);
            }

            var package = FindLatestInternalUpdatePackage();
            if (package == null)
            {
                return new InternalUpdatePackageInfo { HasPackage = false };
            }

            return new InternalUpdatePackageInfo
            {
                HasPackage = true,
                Version = package.Version.ToString(),
                FileName = package.FileInfo.Name,
                SizeBytes = package.FileInfo.Length,
                PublishedAt = package.FileInfo.LastWriteTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture)
            };
        }

        public string GetInternalUpdatePackagePath(string fileName)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                throw new InvalidOperationException("Client must download update packages through the LAN API.");
            }

            var safeFileName = Path.GetFileName(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                throw new FileNotFoundException("Update package was not found.");
            }

            var packagePath = Path.Combine(GetInternalUpdatePackageFolder(), safeFileName);
            if (!File.Exists(packagePath))
            {
                throw new FileNotFoundException("Update package was not found.", packagePath);
            }

            return packagePath;
        }

        public void DownloadInternalUpdatePackage(string fileName, string destinationPath)
        {
            if (AppPathSettings.Current.IsClientMode)
            {
                _lanClient.DownloadFile("settings/update/download", new InternalUpdateDownloadRequest { FileName = fileName }, destinationPath);
                return;
            }

            File.Copy(GetInternalUpdatePackagePath(fileName), destinationPath, true);
        }

        public void RestoreDatabaseFromFile(string sourcePath, string safetyBackupPath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Backup database file was not found.", sourcePath);
            }

            ValidateDatabaseFile(sourcePath);
            BackupDatabase(safetyBackupPath);

            using var source = OpenDatabaseFile(sourcePath);
            using var destination = OpenConnection();
            source.BackupDatabase(destination);
            ValidateDatabaseConnection(destination);
            WriteDatabaseLog(destination, null, "Sao lÆ°u", "KhÃ´i phá»¥c", DatabasePath, $"KhÃ´i phá»¥c dá»¯ liá»‡u tá»« {sourcePath}.");
        }

        public static void ValidateDatabaseFile(string databasePath)
        {
            using var connection = OpenDatabaseFile(databasePath);
            ValidateDatabaseConnection(connection);
        }

        private SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private static string GetInternalUpdatePackageFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuanLyHoSo",
                "Updates",
                "Packages");
        }

        private static InternalUpdatePackage FindLatestInternalUpdatePackage()
        {
            var packageFolder = GetInternalUpdatePackageFolder();
            if (!Directory.Exists(packageFolder))
            {
                return null;
            }

            return Directory.EnumerateFiles(packageFolder, "*.zip")
                .Select(path => new FileInfo(path))
                .Select(file => new InternalUpdatePackage(file, TryParseVersionFromFileName(file.Name)))
                .Where(package => package.Version != null)
                .OrderByDescending(package => package.Version)
                .ThenByDescending(package => package.FileInfo.LastWriteTime)
                .FirstOrDefault();
        }

        private static Version TryParseVersionFromFileName(string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
            var match = Regex.Match(name, @"(?<!\d)v?(?<version>\d+\.\d+\.\d+(?:\.\d+)?)(?!\d)", RegexOptions.IgnoreCase);
            return match.Success && Version.TryParse(match.Groups["version"].Value, out var version)
                ? version
                : null;
        }

        private sealed class InternalUpdatePackage
        {
            public InternalUpdatePackage(FileInfo fileInfo, Version version)
            {
                FileInfo = fileInfo;
                Version = version;
            }

            public FileInfo FileInfo { get; }
            public Version Version { get; }
        }

        private static SqliteConnection OpenDatabaseFile(string databasePath)
        {
            var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            return connection;
        }

        private static void ValidateDatabaseConnection(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA quick_check;";
            var result = command.ExecuteScalar()?.ToString();
            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"SQLite database validation failed: {result}");
            }
        }

        private static void CreateSchema(SqliteConnection connection)
        {
            ExecuteNonQuery(connection, @"
CREATE TABLE IF NOT EXISTS Areas (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    AreaType TEXT NOT NULL,
    DisplayOrder INTEGER NOT NULL,
    IsArranged INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS CatalogItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CatalogType TEXT NOT NULL,
    Name TEXT NOT NULL,
    DisplayOrder INTEGER NOT NULL,
    IsActive INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS Records (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RecordCode TEXT NOT NULL UNIQUE,
    ReceivedDate TEXT NOT NULL,
    ReceiveSource TEXT NOT NULL,
    ReceiverName TEXT NOT NULL,
    SenderName TEXT NOT NULL,
    SenderPhone TEXT NOT NULL,
    ContactAddress TEXT NOT NULL,
    AreaName TEXT NOT NULL,
    IncidentAddress TEXT NOT NULL,
    Content TEXT NOT NULL,
    CaseType TEXT NOT NULL,
    ContentGroup TEXT NOT NULL,
    Field TEXT NOT NULL,
    RelatedPerson TEXT NOT NULL,
    ExpectedHandlingMethod TEXT NOT NULL,
    SenderExpectedHandlingMethod TEXT NOT NULL DEFAULT '',
    SeverityLevel TEXT NOT NULL,
    ExpectedResultDate TEXT NOT NULL,
    Status TEXT NOT NULL,
    ProcessorName TEXT NOT NULL,
    Note TEXT NOT NULL,
    AdditionalNote TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS RecordAttachments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RecordId INTEGER NOT NULL,
    FileName TEXT NOT NULL,
    FileSize TEXT NOT NULL,
    FilePath TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (RecordId) REFERENCES Records(Id)
);

CREATE TABLE IF NOT EXISTS ProcessHistories (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RecordId INTEGER NOT NULL,
    Title TEXT NOT NULL,
    ProcessedAt TEXT NOT NULL,
    ProcessorName TEXT NOT NULL,
    Content TEXT NOT NULL,
    IsCompleted INTEGER NOT NULL,
    FOREIGN KEY (RecordId) REFERENCES Records(Id)
);

CREATE TABLE IF NOT EXISTS SystemLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    UserName TEXT NOT NULL,
    Module TEXT NOT NULL,
    Action TEXT NOT NULL,
    Target TEXT NOT NULL,
    Detail TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS LeadershipNotices (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    SenderName TEXT NOT NULL,
    Scope TEXT NOT NULL,
    TargetName TEXT NOT NULL,
    KpiTarget TEXT NOT NULL,
    Message TEXT NOT NULL,
    ReadBy TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS LeadershipKpiTargets (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    SenderName TEXT NOT NULL,
    Scope TEXT NOT NULL,
    TargetName TEXT NOT NULL,
    KpiTarget TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserName TEXT NOT NULL UNIQUE,
    DisplayName TEXT NOT NULL,
    Role TEXT NOT NULL,
    PasswordHash TEXT NOT NULL,
    IsActive INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");
            TryAddColumn(connection, "Records", "SenderExpectedHandlingMethod", "TEXT NOT NULL DEFAULT ''");
            TryAddColumn(connection, "RecordAttachments", "FilePath", "TEXT NOT NULL DEFAULT ''");
            TryAddColumn(connection, "LeadershipNotices", "ReadBy", "TEXT NOT NULL DEFAULT ''");
            CreateIndexes(connection);
        }

        private static void MigrateLegacyPriorityColumnIfNeeded(SqliteConnection connection)
        {
            if (!ColumnExists(connection, "Records", "PriorityLevel"))
            {
                return;
            }

            try
            {
                using (var fkOff = connection.CreateCommand())
                {
                    fkOff.CommandText = "PRAGMA foreign_keys = OFF;";
                    fkOff.ExecuteNonQuery();
                }

                using var transaction = connection.BeginTransaction();
                ExecuteNonQuery(connection, "ALTER TABLE Records RENAME TO Records_Legacy;");
                ExecuteNonQuery(connection, @"
CREATE TABLE Records (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RecordCode TEXT NOT NULL UNIQUE,
    ReceivedDate TEXT NOT NULL,
    ReceiveSource TEXT NOT NULL,
    ReceiverName TEXT NOT NULL,
    SenderName TEXT NOT NULL,
    SenderPhone TEXT NOT NULL,
    ContactAddress TEXT NOT NULL,
    AreaName TEXT NOT NULL,
    IncidentAddress TEXT NOT NULL,
    Content TEXT NOT NULL,
    CaseType TEXT NOT NULL,
    ContentGroup TEXT NOT NULL,
    Field TEXT NOT NULL,
    RelatedPerson TEXT NOT NULL,
    ExpectedHandlingMethod TEXT NOT NULL,
    SenderExpectedHandlingMethod TEXT NOT NULL DEFAULT '',
    SeverityLevel TEXT NOT NULL,
    ExpectedResultDate TEXT NOT NULL,
    Status TEXT NOT NULL,
    ProcessorName TEXT NOT NULL,
    Note TEXT NOT NULL,
    AdditionalNote TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

                ExecuteNonQuery(connection, @"
INSERT INTO Records (
    Id, RecordCode, ReceivedDate, ReceiveSource, ReceiverName, SenderName, SenderPhone, ContactAddress,
    AreaName, IncidentAddress, Content, CaseType, ContentGroup, Field, RelatedPerson,
    ExpectedHandlingMethod, SenderExpectedHandlingMethod, SeverityLevel, ExpectedResultDate,
    Status, ProcessorName, Note, AdditionalNote, CreatedAt, UpdatedAt)
SELECT
    Id, RecordCode, ReceivedDate, ReceiveSource, ReceiverName, SenderName, SenderPhone, ContactAddress,
    AreaName, IncidentAddress, Content, CaseType, ContentGroup, Field, RelatedPerson,
    ExpectedHandlingMethod, SenderExpectedHandlingMethod, SeverityLevel, ExpectedResultDate,
    Status, ProcessorName, Note, AdditionalNote, CreatedAt, UpdatedAt
FROM Records_Legacy;");

                ExecuteNonQuery(connection, "DROP TABLE Records_Legacy;");
                transaction.Commit();
            }
            catch
            {
                try
                {
                    using var rollback = connection.BeginTransaction();
                    rollback.Rollback();
                }
                catch
                {
                    // Ignore rollback failures during a failed migration.
                }

                throw;
            }
            finally
            {
                using var fkOn = connection.CreateCommand();
                fkOn.CommandText = "PRAGMA foreign_keys = ON;";
                fkOn.ExecuteNonQuery();
            }
        }

        private static void RepairRecordForeignKeys(SqliteConnection connection)
        {
            var needsRepair = TableSqlContains(connection, "RecordAttachments", "Records_Legacy")
                || TableSqlContains(connection, "ProcessHistories", "Records_Legacy");
            if (!needsRepair)
            {
                return;
            }

            SqliteTransaction transaction = null;
            try
            {
                ExecuteNonQuery(connection, "PRAGMA foreign_keys = OFF;");
                transaction = connection.BeginTransaction();
                ExecuteNonQuery(connection, "ALTER TABLE RecordAttachments RENAME TO RecordAttachments_Legacy;");
                ExecuteNonQuery(connection, "ALTER TABLE ProcessHistories RENAME TO ProcessHistories_Legacy;");
                ExecuteNonQuery(connection, @"
CREATE TABLE RecordAttachments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RecordId INTEGER NOT NULL,
    FileName TEXT NOT NULL,
    FileSize TEXT NOT NULL,
    FilePath TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (RecordId) REFERENCES Records(Id)
);
CREATE TABLE ProcessHistories (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RecordId INTEGER NOT NULL,
    Title TEXT NOT NULL,
    ProcessedAt TEXT NOT NULL,
    ProcessorName TEXT NOT NULL,
    Content TEXT NOT NULL,
    IsCompleted INTEGER NOT NULL,
    FOREIGN KEY (RecordId) REFERENCES Records(Id)
);");
                ExecuteNonQuery(connection, @"
INSERT INTO RecordAttachments (Id, RecordId, FileName, FileSize, FilePath)
SELECT Id, RecordId, FileName, FileSize, FilePath FROM RecordAttachments_Legacy;
INSERT INTO ProcessHistories (Id, RecordId, Title, ProcessedAt, ProcessorName, Content, IsCompleted)
SELECT Id, RecordId, Title, ProcessedAt, ProcessorName, Content, IsCompleted FROM ProcessHistories_Legacy;
DROP TABLE RecordAttachments_Legacy;
DROP TABLE ProcessHistories_Legacy;");
                transaction.Commit();
            }
            catch
            {
                transaction?.Rollback();
                throw;
            }
            finally
            {
                transaction?.Dispose();
                ExecuteNonQuery(connection, "PRAGMA foreign_keys = ON;");
            }
        }

        private static bool TableSqlContains(SqliteConnection connection, string tableName, string value)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = $tableName;";
            command.Parameters.AddWithValue("$tableName", tableName);
            return command.ExecuteScalar()?.ToString()?.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void NormalizeSeverityCatalog(SqliteConnection connection)
        {
            var severityMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Bình thường"] = "Ít nghiêm trọng",
                ["Ưu tiên"] = "Nghiêm trọng",
                ["Khẩn"] = "Rất nghiêm trọng",
                ["Ít nghiêm trọng"] = "Ít nghiêm trọng",
                ["Nghiêm trọng"] = "Nghiêm trọng",
                ["Rất nghiêm trọng"] = "Rất nghiêm trọng",
                ["Đặc biệt nghiêm trọng"] = "Đặc biệt nghiêm trọng"
            };

            var severityValues = new[]
            {
                "Ít nghiêm trọng",
                "Nghiêm trọng",
                "Rất nghiêm trọng",
                "Đặc biệt nghiêm trọng"
            };

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name FROM CatalogItems WHERE CatalogType = 'Priority';";
            using var reader = command.ExecuteReader();
            var existing = new List<(int Id, string Name)>();
            while (reader.Read())
            {
                existing.Add((reader.GetInt32(0), reader.GetString(1)));
            }

            foreach (var item in existing)
            {
                var normalized = severityMap.TryGetValue(item.Name, out var value) ? value : item.Name;
                using var updateCommand = connection.CreateCommand();
                updateCommand.CommandText = "UPDATE CatalogItems SET Name = $name WHERE Id = $id;";
                updateCommand.Parameters.AddWithValue("$name", normalized);
                updateCommand.Parameters.AddWithValue("$id", item.Id);
                updateCommand.ExecuteNonQuery();
            }

            foreach (var severity in severityValues)
            {
                using var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT COUNT(*) FROM CatalogItems WHERE CatalogType = 'Priority' AND Name = $name;";
                checkCommand.Parameters.AddWithValue("$name", severity);
                var count = Convert.ToInt32(checkCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
                if (count > 0)
                {
                    continue;
                }

                using var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = "INSERT INTO CatalogItems (CatalogType, Name, DisplayOrder, IsActive) VALUES ('Priority', $name, (SELECT COALESCE(MAX(DisplayOrder), 0) + 1 FROM CatalogItems WHERE CatalogType = 'Priority'), 1);";
                insertCommand.Parameters.AddWithValue("$name", severity);
                insertCommand.ExecuteNonQuery();
            }
        }

        private static void NormalizeRecordSeverity(SqliteConnection connection)
        {
            var severityValues = new[]
            {
                "Ít nghiêm trọng",
                "Nghiêm trọng",
                "Rất nghiêm trọng",
                "Đặc biệt nghiêm trọng"
            };
            var legacyValues = new[] { "Bình thường", "Ưu tiên", "Khẩn" };
            var placeholders = string.Join(", ", legacyValues.Select((_, index) => $"$legacy{index}"));
            var recordIds = new List<int>();

            using (var selectCommand = connection.CreateCommand())
            {
                selectCommand.CommandText = $"SELECT Id FROM Records WHERE SeverityLevel IN ({placeholders}) ORDER BY Id;";
                for (var index = 0; index < legacyValues.Length; index++)
                {
                    selectCommand.Parameters.AddWithValue($"$legacy{index}", legacyValues[index]);
                }

                using var reader = selectCommand.ExecuteReader();
                while (reader.Read())
                {
                    recordIds.Add(reader.GetInt32(0));
                }
            }

            if (recordIds.Count == 0)
            {
                return;
            }

            var random = new Random();
            using var transaction = connection.BeginTransaction();
            using var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = "UPDATE Records SET SeverityLevel = $severity WHERE Id = $id;";
            var severityParameter = updateCommand.Parameters.Add("$severity", SqliteType.Text);
            var idParameter = updateCommand.Parameters.Add("$id", SqliteType.Integer);

            foreach (var recordId in recordIds)
            {
                severityParameter.Value = severityValues[random.Next(severityValues.Length)];
                idParameter.Value = recordId;
                updateCommand.ExecuteNonQuery();
            }

            transaction.Commit();
            AppLogger.Info("Database", "NormalizeRecordSeverity", $"Updated {recordIds.Count} legacy record severity value(s).");
        }

        private static void NormalizeProcessingHistoryTitles(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
UPDATE ProcessHistories
SET Title = 'Kết quả xử lý ban đầu'
WHERE Title = 'Gia hạn';";
            var updatedRows = command.ExecuteNonQuery();
            if (updatedRows > 0)
            {
                AppLogger.Info("Database", "NormalizeProcessingHistoryTitles", $"Updated {updatedRows} legacy processing history title(s).");
            }
        }

        private static void CreateIndexes(SqliteConnection connection)
        {
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Records_ReceivedDate_Id ON Records (ReceivedDate, Id);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Records_UpdatedAt_Id ON Records (UpdatedAt, Id);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Records_Status_UpdatedAt_Id ON Records (Status, UpdatedAt, Id);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Records_AreaName_UpdatedAt_Id ON Records (AreaName, UpdatedAt, Id);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Records_CaseType_ReceivedDate_Id ON Records (CaseType, ReceivedDate, Id);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Records_Field_ReceivedDate_Id ON Records (Field, ReceivedDate, Id);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Records_ProcessorName_UpdatedAt_Id ON Records (ProcessorName, UpdatedAt, Id);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Records_SeverityLevel_UpdatedAt_Id ON Records (SeverityLevel, UpdatedAt, Id);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_RecordAttachments_RecordId ON RecordAttachments (RecordId);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_ProcessHistories_RecordId_ProcessedAt ON ProcessHistories (RecordId, ProcessedAt);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_CatalogItems_Type_Active_Order ON CatalogItems (CatalogType, IsActive, DisplayOrder);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_SystemLogs_Id ON SystemLogs (Id);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Users_UserName ON Users (UserName);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_LeadershipNotices_Target_Created ON LeadershipNotices (Scope, TargetName, CreatedAt, Id);");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_LeadershipKpiTargets_Target_Created ON LeadershipKpiTargets (Scope, TargetName, CreatedAt, Id);");
        }

        private static void SeedUsers(SqliteConnection connection)
        {
            if (CountRows(connection, "Users") > 0)
            {
                return;
            }

            var now = DateTime.Now.ToString("O", CultureInfo.InvariantCulture);
            using var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO Users (UserName, DisplayName, Role, PasswordHash, IsActive, CreatedAt, UpdatedAt)
VALUES ($userName, $displayName, $role, $passwordHash, 1, $now, $now);";
            command.Parameters.AddWithValue("$userName", "admin");
            command.Parameters.AddWithValue("$displayName", "Quản trị hệ thống");
            command.Parameters.AddWithValue("$role", UserRoles.Admin);
            command.Parameters.AddWithValue("$passwordHash", PasswordHasher.HashPassword("admin123"));
            command.Parameters.AddWithValue("$now", now);
            command.ExecuteNonQuery();
        }

        private static void SeedAreas(SqliteConnection connection)
        {
            if (CountRows(connection, "Areas") > 0)
            {
                return;
            }

            var areas = new List<(string Type, string Name, bool IsArranged)>();
            var arrangedCommunes = new[]
            {
                "An Phú","Vĩnh Hậu","Nhơn Hội","Khánh Bình","Phú Hữu","Tân An","Châu Phong","Vĩnh Xương","Phú Tân","Phú An",
                "Bình Thạnh Đông","Chợ Vàm","Hòa Lạc","Phú Lâm","Châu Phú","Mỹ Đức","Vĩnh Thạnh Trung","Bình Mỹ","Thạnh Mỹ Tây","An Cư",
                "Núi Cấm","Ba Chúc","Tri Tôn","Ô Lâm","Cô Tô","Vĩnh Gia","An Châu","Bình Hòa","Cần Đăng","Vĩnh Hanh",
                "Vĩnh An","Chợ Mới","Cù Lao Giêng","Hội An","Long Điền","Nhơn Mỹ","Long Kiến","Thoại Sơn","Óc Eo","Định Mỹ",
                "Phú Hòa","Vĩnh Trạch","Tây Phú","Vĩnh Bình","Vĩnh Thuận","Vĩnh Phong","Vĩnh Hòa","U Minh Thượng","Đông Hòa","Tân Thạnh",
                "Đông Hưng","An Minh","Vân Khánh","Tây Yên","Đông Thái","An Biên","Định Hòa","Gò Quao","Vĩnh Hòa Hưng","Vĩnh Tuy",
                "Giồng Riềng","Thạnh Hưng","Long Thạnh","Hòa Hưng","Ngọc Chúc","Hòa Thuận","Tân Hội","Tân Hiệp","Thạnh Đông","Thạnh Lộc",
                "Châu Thành","Bình An","Hòn Đất","Sơn Kiên","Mỹ Thuận","Hòa Điền","Kiên Lương","Giang Thành","Vĩnh Điều"
            };
            foreach (var name in arrangedCommunes)
            {
                areas.Add(("Xã", name, true));
            }

            foreach (var name in new[] { "Mỹ Hòa Hưng", "Bình Giang", "Bình Sơn", "Hòn Nghệ", "Sơn Hải", "Tiên Hải" })
            {
                areas.Add(("Xã", name, false));
            }

            foreach (var name in new[] { "Long Xuyên", "Bình Đức", "Mỹ Thới", "Châu Đốc", "Vĩnh Tế", "Tân Châu", "Long Phú", "Tịnh Biên", "Thới Sơn", "Chi Lăng", "Vĩnh Thông", "Rạch Giá", "Hà Tiên", "Tô Châu" })
            {
                areas.Add(("Phường", name, true));
            }

            foreach (var name in new[] { "Kiên Hải", "Phú Quốc", "Thổ Châu" })
            {
                areas.Add(("Đặc khu", name, true));
            }

            using var transaction = connection.BeginTransaction();
            for (var i = 0; i < areas.Count; i++)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO Areas (Name, AreaType, DisplayOrder, IsArranged) VALUES ($name, $type, $displayOrder, $isArranged);";
                command.Parameters.AddWithValue("$name", areas[i].Name);
                command.Parameters.AddWithValue("$type", areas[i].Type);
                command.Parameters.AddWithValue("$displayOrder", i + 1);
                command.Parameters.AddWithValue("$isArranged", areas[i].IsArranged ? 1 : 0);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static void EnsureStandardOrganizationAreas(SqliteConnection connection)
        {
            var standardAreas = AreaSelectionOptions.GetStandardOrganizationAreas();
            using var transaction = connection.BeginTransaction();
            foreach (var area in standardAreas)
            {
                if (AreaExists(connection, transaction, area.AreaType, area.Name))
                {
                    continue;
                }

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO Areas (Name, AreaType, DisplayOrder, IsArranged)
VALUES (
    $name,
    $type,
    COALESCE((SELECT MAX(DisplayOrder) + 1 FROM Areas), 1),
    1
);";
                command.Parameters.AddWithValue("$name", area.Name);
                command.Parameters.AddWithValue("$type", area.AreaType);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static void SeedCatalogs(SqliteConnection connection)
        {
            if (CountRows(connection, "CatalogItems") > 0)
            {
                return;
            }

            var catalogs = new List<(string Type, string Name)>
            {
                ("ReceiveSource", "Trực tiếp"),
                ("ReceiveSource", "Qua bưu điện"),
                ("ReceiveSource", "Cổng thông tin"),
                ("ReceiveSource", "Cơ quan chuyển đến"),
                ("CaseType", "Khiếu nại"),
                ("CaseType", "Tố cáo"),
                ("CaseType", "Kiến nghị"),
                ("CaseType", "Phản ánh"),
                ("Field", "Quản lý đất đai"),
                ("Field", "Xây dựng"),
                ("Field", "Tài nguyên môi trường"),
                ("Field", "Trật tự đô thị"),
                ("ContentGroup", "Đất đai - Xây dựng"),
                ("ContentGroup", "Môi trường"),
                ("ContentGroup", "An ninh trật tự"),
                ("ContentGroup", "Khác"),
                ("Priority", "Ít nghiêm trọng"),
                ("Priority", "Nghiêm trọng"),
                ("Priority", "Rất nghiêm trọng"),
                ("Priority", "Đặc biệt nghiêm trọng"),
                ("ProcessorName", "Trần Văn B"),
                ("ProcessorName", "Trần Văn C"),
                ("ProcessorName", "Lê Thị D"),
                ("ProcessorName", "Lê Võ Mỹ Ý"),
                ("ProcessorName", "Nguyễn Minh Thắng"),
                ("ProcessorName", "Nguyễn Thị H"),
                ("ProcessorName", "Phạm Văn K"),
                ("ExpectedHandlingMethod", "Đề nghị kiểm tra, xử lý"),
                ("ExpectedHandlingMethod", "Chuyển cơ quan có thẩm quyền"),
                ("ExpectedHandlingMethod", "Theo dõi, tổng hợp")
            };

            using var transaction = connection.BeginTransaction();
            var orderByType = new Dictionary<string, int>();
            foreach (var catalog in catalogs)
            {
                orderByType.TryGetValue(catalog.Type, out var order);
                orderByType[catalog.Type] = ++order;

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO CatalogItems (CatalogType, Name, DisplayOrder, IsActive) VALUES ($type, $name, $displayOrder, 1);";
                command.Parameters.AddWithValue("$type", catalog.Type);
                command.Parameters.AddWithValue("$name", catalog.Name);
                command.Parameters.AddWithValue("$displayOrder", order);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static void SeedRecords(SqliteConnection connection)
        {
            if (CountRows(connection, "Records") > 0)
            {
                return;
            }

            const int recordsPerProcessor = 15;
            var random = new Random(20260905);
            var areaNames = ReadAreaDisplayNames(connection);
            var receiveSources = ReadCatalog(connection, "ReceiveSource");
            var caseTypes = ReadCatalog(connection, "CaseType");
            var fields = ReadCatalog(connection, "Field");
            var contentGroups = ReadCatalog(connection, "ContentGroup");
            var severities = new[]
            {
                "Ít nghiêm trọng",
                "Nghiêm trọng",
                "Rất nghiêm trọng",
                "Đặc biệt nghiêm trọng"
            };
            var methods = ReadCatalog(connection, "ExpectedHandlingMethod");
            var statuses = new[] { "Mới tiếp nhận", "Đang phân loại", "Đã phân công", "Đang xác minh", "Chờ kết quả", "Đang chờ bổ sung tài liệu", "Đã giải quyết", "Chuyển cơ quan khác" };
            var processors = new[]
            {
                "Lê Thị D",
                "Lê Võ Mỹ Ý",
                "Nguyễn Minh Thắng",
                "Nguyễn Thị H",
                "Phạm Văn K",
                "Trần Văn B",
                "Trần Văn C"
            };
            var processorProfiles = new Dictionary<string, (int CompletedChance, int HighOnTimeChance, int PendingChance, int OverdueChance)>
            {
                ["Lê Thị D"] = (7, 1, 1, 1),
                ["Lê Võ Mỹ Ý"] = (9, 1, 0, 0),
                ["Nguyễn Minh Thắng"] = (8, 1, 1, 0),
                ["Nguyễn Thị H"] = (6, 1, 1, 2),
                ["Phạm Văn K"] = (5, 1, 1, 3),
                ["Trần Văn B"] = (7, 1, 1, 1),
                ["Trần Văn C"] = (6, 1, 1, 2)
            };
            var senders = new[] { "Nguyễn Văn A", "Trần Thị B", "Lê Văn C", "Phạm Thị D", "Võ Văn E", "Huỳnh Văn F", "Đặng Thị G", "Bùi Văn H" };
            var relatedPersons = new[] { "Trần Văn C", "Lê Thị D", "Nguyễn Văn M", "Công ty TNHH An Phú", "Hộ dân liền kề" };

            using var transaction = connection.BeginTransaction();
            var totalSeedRecords = processors.Length * recordsPerProcessor;
            for (var i = 1; i <= totalSeedRecords; i++)
            {
                var now = DateTime.Now;
                var receivedDate = DateTime.Today.AddDays(-random.Next(0, 90)).AddHours(random.Next(8, 17)).AddMinutes(random.Next(0, 60));
                if (receivedDate > now)
                {
                    receivedDate = now;
                }

                var processorIndex = (i - 1) % processors.Length;
                var recordIndexForProcessor = (i - 1) / processors.Length;
                var processor = processors[processorIndex];
                var profile = processorProfiles[processor];
                var profileRoll = (recordIndexForProcessor * 3 + processorIndex) % 10;
                string status;
                if (profileRoll < profile.CompletedChance)
                {
                    status = "Đã giải quyết";
                }
                else if (profileRoll < profile.CompletedChance + profile.HighOnTimeChance)
                {
                    status = "Đang xác minh";
                }
                else if (profileRoll < profile.CompletedChance + profile.HighOnTimeChance + profile.PendingChance)
                {
                    status = "Chờ kết quả";
                }
                else if (profileRoll < profile.CompletedChance + profile.HighOnTimeChance + profile.PendingChance + profile.OverdueChance)
                {
                    status = "Đang chờ bổ sung tài liệu";
                }
                else
                {
                    status = "Mới tiếp nhận";
                }

                var areaName = areaNames[random.Next(areaNames.Count)];
                var updatedAt = receivedDate.AddDays(random.Next(0, 12)).AddHours(random.Next(0, 8));
                if (updatedAt > now)
                {
                    updatedAt = now;
                }

                DateTime expectedDate;
                if (status == "Đã giải quyết")
                {
                    var isCompletedLate = (recordIndexForProcessor + processorIndex) % 5 == 0;
                    expectedDate = isCompletedLate
                        ? updatedAt.Date.AddDays(-random.Next(1, 4))
                        : updatedAt.Date.AddDays(random.Next(1, 5));
                }
                else
                {
                    var deadlineBucket = (recordIndexForProcessor + processorIndex) % 3;
                    expectedDate = deadlineBucket switch
                    {
                        0 => DateTime.Today.AddDays(-random.Next(1, 11)),
                        1 => DateTime.Today.AddDays(random.Next(1, 8)),
                        _ => DateTime.Today.AddDays(random.Next(8, 31))
                    };
                }

                var caseType = caseTypes[random.Next(caseTypes.Count)];
                var field = fields[random.Next(fields.Count)];
                var recordCode = $"HS-{DateTime.Today:yyyy}-{i:000000}";

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO Records (
    RecordCode, ReceivedDate, ReceiveSource, ReceiverName, SenderName, SenderPhone, ContactAddress,
    AreaName, IncidentAddress, Content, CaseType, ContentGroup, Field, RelatedPerson,
    ExpectedHandlingMethod, SenderExpectedHandlingMethod, SeverityLevel, ExpectedResultDate, Status, ProcessorName,
    Note, AdditionalNote, CreatedAt, UpdatedAt)
VALUES (
    $recordCode, $receivedDate, $receiveSource, $receiverName, $senderName, $senderPhone, $contactAddress,
    $areaName, $incidentAddress, $content, $caseType, $contentGroup, $field, $relatedPerson,
    $method, $senderMethod, $severity, $expectedDate, $status, $processor,
    $note, $additionalNote, $createdAt, $updatedAt);
SELECT last_insert_rowid();";
                command.Parameters.AddWithValue("$recordCode", recordCode);
                command.Parameters.AddWithValue("$receivedDate", receivedDate.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$receiveSource", receiveSources[random.Next(receiveSources.Count)]);
                command.Parameters.AddWithValue("$receiverName", processors[random.Next(processors.Length)]);
                command.Parameters.AddWithValue("$senderName", senders[random.Next(senders.Length)]);
                command.Parameters.AddWithValue("$senderPhone", $"09{random.Next(10, 99)} {random.Next(100, 999)} {random.Next(100, 999)}");
                command.Parameters.AddWithValue("$contactAddress", $"Ấp {random.Next(1, 9)}, {areaName}, An Giang");
                command.Parameters.AddWithValue("$areaName", areaName);
                command.Parameters.AddWithValue("$incidentAddress", $"Khu vực {random.Next(1, 12)}, {areaName}, An Giang");
                command.Parameters.AddWithValue("$content", BuildRandomContent(caseType, field, areaName));
                command.Parameters.AddWithValue("$caseType", caseType);
                command.Parameters.AddWithValue("$contentGroup", contentGroups[random.Next(contentGroups.Count)]);
                command.Parameters.AddWithValue("$field", field);
                command.Parameters.AddWithValue("$relatedPerson", relatedPersons[random.Next(relatedPersons.Length)]);
                command.Parameters.AddWithValue("$method", methods[random.Next(methods.Count)]);
                command.Parameters.AddWithValue("$senderMethod", methods[random.Next(methods.Count)]);
                command.Parameters.AddWithValue("$severity", severities[random.Next(severities.Length)]);
                command.Parameters.AddWithValue("$expectedDate", expectedDate.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$status", status);
                command.Parameters.AddWithValue("$processor", processor);
                command.Parameters.AddWithValue("$note", "Hồ sơ mẫu được tạo tự động để phục vụ thiết kế giao diện.");
                command.Parameters.AddWithValue("$additionalNote", "Có thể thay thế bằng dữ liệu thật khi triển khai chức năng nhập liệu.");
                command.Parameters.AddWithValue("$createdAt", receivedDate.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$updatedAt", updatedAt.ToString("O", CultureInfo.InvariantCulture));
                var recordId = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);

                InsertAttachments(connection, transaction, recordId, random);
                InsertProcessHistory(connection, transaction, recordId, receivedDate, updatedAt, processor, status);
            }

            transaction.Commit();
        }

        private static void InsertAttachments(SqliteConnection connection, SqliteTransaction transaction, int recordId, Random random)
        {
            var files = new[]
            {
                ("Đơn khiếu nại.pdf", "512 KB"),
                ("Hình ảnh hiện trạng.png", "1.2 MB"),
                ("Giấy chứng nhận quyền sử dụng đất.pdf", "842 KB"),
                ("Biên bản làm việc.pdf", "620 KB")
            };

            var fileCount = random.Next(1, 4);
            for (var i = 0; i < fileCount; i++)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO RecordAttachments (RecordId, FileName, FileSize) VALUES ($recordId, $fileName, $fileSize);";
                command.Parameters.AddWithValue("$recordId", recordId);
                command.Parameters.AddWithValue("$fileName", files[i].Item1);
                command.Parameters.AddWithValue("$fileSize", files[i].Item2);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertProcessHistory(SqliteConnection connection, SqliteTransaction transaction, int recordId, DateTime receivedDate, DateTime updatedAt, string processor, string status)
        {
            var histories = new List<(string Title, DateTime Time, string Content, bool Done)>
            {
                ("Tiếp nhận", receivedDate, "Tiếp nhận hồ sơ và kiểm tra thông tin ban đầu.", true),
                ("Phân loại", receivedDate.AddHours(2), "Phân loại hồ sơ theo lĩnh vực và loại vụ việc.", true)
            };

            if (status != "Mới tiếp nhận" && status != "Đang phân loại")
            {
                histories.Add(("Phân công", receivedDate.AddHours(5), "Phân công cán bộ phụ trách xử lý hồ sơ.", true));
            }

            if (status == "Đang xác minh" || status == "Chờ kết quả" || status == "Đang chờ bổ sung tài liệu" || status == "Đã giải quyết")
            {
                histories.Add(("Xác minh", updatedAt, "Cập nhật tiến độ xác minh hồ sơ.", status != "Đang xác minh"));
            }

            if (status == "Đã giải quyết")
            {
                histories.Add(("Kết thúc", updatedAt.AddHours(2), "Hoàn tất xử lý và lưu hồ sơ.", true));
            }

            foreach (var history in histories)
            {
                var processedAt = history.Time > DateTime.Now ? DateTime.Now : history.Time;
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO ProcessHistories (RecordId, Title, ProcessedAt, ProcessorName, Content, IsCompleted)
VALUES ($recordId, $title, $processedAt, $processor, $content, $isCompleted);";
                command.Parameters.AddWithValue("$recordId", recordId);
                command.Parameters.AddWithValue("$title", history.Title);
                command.Parameters.AddWithValue("$processedAt", processedAt.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$processor", processor);
                command.Parameters.AddWithValue("$content", history.Content);
                command.Parameters.AddWithValue("$isCompleted", history.Done ? 1 : 0);
                command.ExecuteNonQuery();
            }
        }

        private static List<AttachmentDraft> GetAttachments(SqliteConnection connection, int recordId)
        {
            var result = new List<AttachmentDraft>();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT FileName, FileSize, FilePath FROM RecordAttachments WHERE RecordId = $recordId ORDER BY Id;";
            command.Parameters.AddWithValue("$recordId", recordId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new AttachmentDraft
                {
                    FileName = reader.GetString(0),
                    FileSize = reader.GetString(1),
                    FilePath = reader.GetString(2)
                });
            }

            return result;
        }

        private static List<ProcessHistoryItem> GetProcessHistory(SqliteConnection connection, int recordId, string status)
        {
            var result = new List<ProcessHistoryItem>();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Title, ProcessedAt, ProcessorName, Content, IsCompleted
FROM ProcessHistories
WHERE RecordId = $recordId
ORDER BY ProcessedAt;";
            command.Parameters.AddWithValue("$recordId", recordId);
            using var reader = command.ExecuteReader();
            var currentStepTitle = GetProcessStepDefinition(GetProcessStepNumber(status)).Title;
            while (reader.Read())
            {
                var title = reader.GetString(0);
                result.Add(new ProcessHistoryItem
                {
                    Title = title,
                    ProcessedAt = FormatDateTime(reader.GetString(1)),
                    ProcessorName = reader.GetString(2),
                    Content = reader.GetString(3),
                    IsCompleted = reader.GetInt32(4) == 1,
                    IsCurrent = title == currentStepTitle,
                    HasDetails = true
                });
            }

            AddPendingProcessHistoryItems(result, status);
            result.Sort((left, right) => GetHistoryStepOrder(left.Title).CompareTo(GetHistoryStepOrder(right.Title)));
            ApplyHistoryConnectorState(result, status);
            return result;
        }

        private static List<ProcessStep> BuildProcessSteps(string status, IReadOnlyList<ProcessHistoryItem> history)
        {
            var currentStep = GetProcessStepNumber(status);

            return new List<ProcessStep>
            {
                CreateStep(1, currentStep, history),
                CreateStep(2, currentStep, history),
                CreateStep(3, currentStep, history),
                CreateStep(4, currentStep, history),
                CreateStep(5, currentStep, history),
                CreateStep(6, currentStep, history),
                CreateStep(7, currentStep, history)
            };
        }

        private static ProcessStep CreateStep(int stepNumber, int currentStep, IReadOnlyList<ProcessHistoryItem> history)
        {
            var definition = GetProcessStepDefinition(stepNumber);
            var historyItem = history?
                .Where(item => item.Title == definition.Title && !IsPendingProcessHistory(item))
                .LastOrDefault();
            return new ProcessStep
            {
                StepNumber = stepNumber,
                IconGlyph = definition.IconGlyph,
                IconFontFamily = definition.IconFontFamily,
                Title = definition.Title,
                DateText = historyItem?.ProcessedAt?.Split(' ').FirstOrDefault() ?? (stepNumber <= currentStep ? "Đã thực hiện" : "Chưa thực hiện"),
                TimeText = historyItem?.ProcessedAt?.Contains(" ") == true
                    ? historyItem.ProcessedAt.Split(' ').Last()
                    : stepNumber == currentStep ? "Đang thực hiện" : string.Empty,
                IsDone = stepNumber < currentStep,
                IsCurrent = stepNumber == currentStep,
                HasPreviousStep = stepNumber > 1,
                HasNextStep = stepNumber < 7,
                IsPreviousConnectorDone = stepNumber > 1 && stepNumber <= currentStep,
                IsNextConnectorDone = stepNumber < 7 && stepNumber < currentStep
            };
        }

        private static void ApplyHistoryConnectorState(IReadOnlyList<ProcessHistoryItem> history, string status)
        {
            var currentStep = GetProcessStepNumber(status);
            for (var index = 0; index < history.Count; index++)
            {
                var item = history[index];
                var step = GetHistoryStepOrder(item.Title);
                if (step < currentStep)
                {
                    item.IsCompleted = true;
                }

                item.HasNextItem = index < history.Count - 1;
                item.IsNextConnectorDone = step < currentStep;
            }
        }

        private static int GetProcessStepNumber(string status)
        {
            return status switch
            {
                "Mới tiếp nhận" => 1,
                "Đang phân loại" => 2,
                "Đã phân công" => 3,
                "Đang xác minh" => 4,
                "Đang chờ bổ sung tài liệu" => 5,
                "Chờ kết quả" => 6,
                "Đã giải quyết" => 7,
                _ => 4
            };
        }

        private static void EnsureCanUpdateProcessingStatus(string status)
        {
            if (AuthContext.IsOfficer && GetProcessStepNumber(status) < 3)
            {
                throw new UnauthorizedAccessException("Can bo khong duoc lui quy trinh ve Moi tiep nhan hoac Dang phan loai.");
            }
        }

        private static (string IconGlyph, string IconFontFamily, string Title) GetProcessStepDefinition(int stepNumber)
        {
            return stepNumber switch
            {
                1 => ("\uE8A5", "Segoe MDL2 Assets", "Tiếp nhận"),
                2 => ("\uE8FD", "Segoe MDL2 Assets", "Phân loại"),
                3 => ("\uE77B", "Segoe MDL2 Assets", "Phân công"),
                4 => ("\uE721", "Segoe MDL2 Assets", "Xác minh"),
                5 => ("analytics", "pack://application:,,,/Assets/Fonts/#Material Symbols Outlined", "Kết quả xử lý ban đầu"),
                6 => ("\uE73E", "Segoe MDL2 Assets", "Kết thúc"),
                7 => ("\uE74E", "Segoe MDL2 Assets", "Lưu hồ sơ"),
                _ => ("\uE8A5", "Segoe MDL2 Assets", "Tiếp nhận")
            };
        }

        private static int GetHistoryStepOrder(string title)
        {
            for (var step = 1; step <= 7; step++)
            {
                if (GetProcessStepDefinition(step).Title == title)
                {
                    return step;
                }
            }

            return 99;
        }

        private static bool IsPendingProcessHistory(ProcessHistoryItem item)
        {
            return item.ProcessedAt == "Chưa thực hiện";
        }

        private static bool HasProcessHistory(SqliteConnection connection, SqliteTransaction transaction, int recordId, string title)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT COUNT(*) FROM ProcessHistories WHERE RecordId = $recordId AND Title = $title;";
            command.Parameters.AddWithValue("$recordId", recordId);
            command.Parameters.AddWithValue("$title", title);
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        private static void InsertProcessHistory(SqliteConnection connection, SqliteTransaction transaction, int recordId, string title, DateTime processedAt, string processor, string content, bool isCompleted)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO ProcessHistories (RecordId, Title, ProcessedAt, ProcessorName, Content, IsCompleted)
VALUES ($recordId, $title, $processedAt, $processor, $content, $isCompleted);";
            command.Parameters.AddWithValue("$recordId", recordId);
            command.Parameters.AddWithValue("$title", title);
            command.Parameters.AddWithValue("$processedAt", processedAt.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$processor", processor);
            command.Parameters.AddWithValue("$content", content);
            command.Parameters.AddWithValue("$isCompleted", isCompleted ? 1 : 0);
            command.ExecuteNonQuery();
        }

        private static void AddPendingProcessHistoryItems(List<ProcessHistoryItem> result, string status)
        {
            var currentStep = GetProcessStepNumber(status);
            for (var step = 1; step <= 7; step++)
            {
                var definition = GetProcessStepDefinition(step);
                if (result.Any(item => item.Title == definition.Title))
                {
                    continue;
                }

                result.Add(new ProcessHistoryItem
                {
                    Title = definition.Title,
                    ProcessedAt = "Chưa thực hiện",
                    ProcessorName = string.Empty,
                    Content = string.Empty,
                    IsCompleted = false,
                    IsCurrent = step == currentStep,
                    HasDetails = false
                });
            }
        }

        private static void DeleteProcessHistoryFromStep(SqliteConnection connection, SqliteTransaction transaction, int recordId, int firstStep)
        {
            var titles = new List<string>();
            for (var step = firstStep; step <= 7; step++)
            {
                titles.Add(GetProcessStepDefinition(step).Title);
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
DELETE FROM ProcessHistories
WHERE RecordId = $recordId
  AND Title IN ({string.Join(", ", titles.Select((_, index) => "$title" + index))});";
            command.Parameters.AddWithValue("$recordId", recordId);
            for (var index = 0; index < titles.Count; index++)
            {
                command.Parameters.AddWithValue("$title" + index, titles[index]);
            }

            command.ExecuteNonQuery();
        }

        private static string BuildRandomContent(string caseType, string field, string areaName)
        {
            return $"{caseType} liên quan đến lĩnh vực {field.ToLower(CultureInfo.GetCultureInfo("vi-VN"))} tại {areaName}. Nội dung cần được tiếp nhận, phân loại và theo dõi xử lý theo quy trình.";
        }

        private static List<string> ReadAreaDisplayNames(SqliteConnection connection)
        {
            var result = new List<string>();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT AreaType, Name FROM Areas ORDER BY DisplayOrder;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(FormatAreaDisplayName(reader.GetString(0), reader.GetString(1)));
            }

            return result;
        }

        private static List<string> ReadCatalog(SqliteConnection connection, string catalogType)
        {
            var result = new List<string>();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Name FROM CatalogItems WHERE CatalogType = $catalogType AND IsActive = 1 ORDER BY DisplayOrder;";
            command.Parameters.AddWithValue("$catalogType", catalogType);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader.GetString(0));
            }

            return result;
        }

        private static void SyncProcessorCatalogFromRecords(SqliteConnection connection)
        {
            using var readerCommand = connection.CreateCommand();
            readerCommand.CommandText = @"
SELECT DISTINCT ProcessorName
FROM Records
WHERE trim(ProcessorName) <> ''
ORDER BY ProcessorName;";

            var processorNames = new List<string>();
            using (var reader = readerCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    processorNames.Add(reader.GetString(0));
                }
            }

            using var transaction = connection.BeginTransaction();
            foreach (var processorName in processorNames)
            {
                EnsureCatalogItem(connection, transaction, "ProcessorName", processorName);
            }

            transaction.Commit();
        }

        private void NotifyCatalogChanged(string catalogType)
        {
            if (string.IsNullOrWhiteSpace(catalogType))
            {
                return;
            }

            var handlers = CatalogChanged;
            if (handlers == null)
            {
                return;
            }

            if (UiDispatcher != null)
            {
                UiDispatcher(() => handlers.Invoke(catalogType));
                return;
            }

            handlers.Invoke(catalogType);
        }

        private static void EnsureCatalogItem(SqliteConnection connection, SqliteTransaction transaction, string catalogType, string name)
        {
            var normalizedName = NormalizeDbText(name);
            if (string.IsNullOrWhiteSpace(normalizedName) || CatalogNameExists(connection, catalogType, normalizedName, transaction: transaction))
            {
                return;
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO CatalogItems (CatalogType, Name, DisplayOrder, IsActive)
VALUES (
    $catalogType,
    $name,
    COALESCE((SELECT MAX(DisplayOrder) + 1 FROM CatalogItems WHERE CatalogType = $catalogType), 1),
    1
);";
            command.Parameters.AddWithValue("$catalogType", catalogType);
            command.Parameters.AddWithValue("$name", normalizedName);
            command.ExecuteNonQuery();
        }

        private static string GetCatalogType(SqliteConnection connection, int id)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT CatalogType FROM CatalogItems WHERE Id = $id LIMIT 1;";
            command.Parameters.AddWithValue("$id", id);
            return command.ExecuteScalar() as string;
        }

        private static string GetCatalogItemName(SqliteConnection connection, int id)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Name FROM CatalogItems WHERE Id = $id LIMIT 1;";
            command.Parameters.AddWithValue("$id", id);
            return command.ExecuteScalar()?.ToString() ?? string.Empty;
        }

        private static void WriteDatabaseLog(SqliteConnection connection, SqliteTransaction transaction, string module, string action, string target, string detail)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO SystemLogs (CreatedAt, UserName, Module, Action, Target, Detail)
VALUES ($createdAt, $userName, $module, $action, $target, $detail);";
                command.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$userName", AuthContext.CurrentUser?.UserName ?? Environment.UserName);
                command.Parameters.AddWithValue("$module", NormalizeDbText(module));
                command.Parameters.AddWithValue("$action", NormalizeDbText(action));
                command.Parameters.AddWithValue("$target", NormalizeDbText(target));
                command.Parameters.AddWithValue("$detail", NormalizeDbText(detail));
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                AppLogger.Warning("Database", "WriteDatabaseLog", "Could not write database audit log.", ex);
            }
        }

        private static bool CatalogNameExists(SqliteConnection connection, string catalogType, string name, int exceptId = 0, SqliteTransaction transaction = null)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
SELECT COUNT(*)
FROM CatalogItems
WHERE CatalogType = $catalogType
  AND IsActive = 1
  AND lower(Name) = lower($name)
  AND Id <> $exceptId;";
            command.Parameters.AddWithValue("$catalogType", catalogType);
            command.Parameters.AddWithValue("$name", name);
            command.Parameters.AddWithValue("$exceptId", exceptId);
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        private static int CountRows(SqliteConnection connection, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {tableName};";
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static int CountRecords(SqliteConnection connection, DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM Records {BuildScopedDateWhere(command, fromDate, toDate)};";
            AddDateParameters(command, fromDate, toDate);
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static string GenerateNextRecordCode(SqliteConnection connection, SqliteTransaction transaction, int year)
        {
            var prefix = $"{RecordCodePrefix}-{year}-";
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
SELECT COALESCE(MAX(CAST(SUBSTR(RecordCode, $sequenceStart) AS INTEGER)), 0)
FROM Records
WHERE RecordCode LIKE $prefixLike;";
            command.Parameters.AddWithValue("$sequenceStart", prefix.Length + 1);
            command.Parameters.AddWithValue("$prefixLike", prefix + "%");

            var maxSequence = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            var nextSequence = maxSequence + 1;
            var sequenceText = nextSequence.ToString(
                new string('0', RecordCodeSequenceWidth),
                CultureInfo.InvariantCulture);
            return prefix + sequenceText;
        }

        private static IReadOnlyList<TrendStat> BuildDailyTrend(IReadOnlyList<(DateTime ReceivedDate, bool IsResolved)> records, DateTime startDate, DateTime endDate)
        {
            var counts = records
                .GroupBy(record => record.ReceivedDate.Date)
                .ToDictionary(
                    group => group.Key,
                    group => (Received: group.Count(), Resolved: group.Count(record => record.IsResolved)));
            var maxValue = counts.Count == 0 ? 0 : counts.Values.Max(item => Math.Max(item.Received, item.Resolved));
            var tickStep = CalculateNiceTrendAxisStep(maxValue);
            var max = Math.Max(tickStep, (int)Math.Ceiling(maxValue / (double)tickStep) * tickStep);
            var result = new List<TrendStat>();
            var index = 0;

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                counts.TryGetValue(date, out var count);
                var item = CreateTrendStat(
                    date.ToString("dd/MM", CultureInfo.GetCultureInfo("vi-VN")),
                    count.Received,
                    count.Resolved,
                    max,
                    index++);
                result.Add(item);
            }

            return result;
        }

        private static IReadOnlyList<TrendStat> BuildMonthlyTrend(IReadOnlyList<(DateTime ReceivedDate, bool IsResolved)> records, DateTime startDate, DateTime endDate)
        {
            var counts = records
                .GroupBy(record => new DateTime(record.ReceivedDate.Year, record.ReceivedDate.Month, 1))
                .ToDictionary(
                    group => group.Key,
                    group => (Received: group.Count(), Resolved: group.Count(record => record.IsResolved)));
            var maxValue = counts.Count == 0 ? 0 : counts.Values.Max(item => Math.Max(item.Received, item.Resolved));
            var tickStep = CalculateNiceTrendAxisStep(maxValue);
            var max = Math.Max(tickStep, (int)Math.Ceiling(maxValue / (double)tickStep) * tickStep);
            var result = new List<TrendStat>();
            var index = 0;

            for (var month = new DateTime(startDate.Year, startDate.Month, 1);
                 month <= new DateTime(endDate.Year, endDate.Month, 1);
                 month = month.AddMonths(1))
            {
                counts.TryGetValue(month, out var count);
                var item = CreateTrendStat(
                    $"Tháng {month.Month}",
                    count.Received,
                    count.Resolved,
                    max,
                    index++);
                result.Add(item);
            }

            return result;
        }

        private static TrendStat CreateTrendStat(string label, int receivedCount, int resolvedCount, int max, int index)
        {
            const double chartHeight = 150.0;
            const double itemWidth = 78.0;
            var centerX = index * itemWidth + itemWidth / 2.0;
            var receivedHeight = Math.Max(6, (int)Math.Round(receivedCount * chartHeight / Math.Max(1, max)));
            var receivedTop = chartHeight - receivedHeight;
            var resolvedPointY = chartHeight - Math.Max(6, (int)Math.Round(resolvedCount * chartHeight / Math.Max(1, max)));
            return new TrendStat
            {
                Label = label,
                ReceivedCount = receivedCount,
                ResolvedCount = resolvedCount,
                ReceivedHeight = receivedHeight,
                ReceivedBarTop = receivedTop,
                ResolvedPointY = resolvedPointY,
                ResolvedPointTop = Math.Max(0, resolvedPointY - 5),
                ResolvedPointLeft = centerX - 5
            };
        }

        private static int CalculateNiceTrendAxisStep(int value)
        {
            if (value <= 0)
            {
                return 2;
            }

            var targetStep = value / 6.0;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(targetStep)));
            var normalized = targetStep / magnitude;
            double niceNormalized;

            if (normalized <= 1)
            {
                niceNormalized = 1;
            }
            else if (normalized <= 2)
            {
                niceNormalized = 2;
            }
            else if (normalized <= 5)
            {
                niceNormalized = 5;
            }
            else
            {
                niceNormalized = 10;
            }

            return (int)(niceNormalized * magnitude);
        }

        private static string BuildExportWhere(
            SqliteCommand command,
            DateTime? fromDate,
            DateTime? toDate,
            string status,
            string caseType,
            string field,
            string areaName,
            string processorName,
            string searchText,
            bool applyUserScope = false)
        {
            var conditions = new List<string>();
            if (fromDate.HasValue)
            {
                conditions.Add("ReceivedDate >= $fromDate");
                command.Parameters.AddWithValue("$fromDate", fromDate.Value.Date.ToString("O", CultureInfo.InvariantCulture));
            }

            if (toDate.HasValue)
            {
                conditions.Add("ReceivedDate <= $toDate");
                command.Parameters.AddWithValue("$toDate", toDate.Value.Date.AddDays(1).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture));
            }

            AddOptionalExportFilter(command, conditions, "Status", "$status", status);
            AddOptionalExportFilter(command, conditions, "CaseType", "$caseType", caseType);
            AddOptionalExportFilter(command, conditions, "Field", "$field", field);
            AddOptionalAreaFilter(command, conditions, areaName);
            AddOptionalExportFilter(command, conditions, "ProcessorName", "$processorName", processorName);
            if (applyUserScope)
            {
                ApplyUserRecordScope(command, conditions);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                conditions.Add("(RecordCode LIKE $search OR SenderName LIKE $search OR Content LIKE $search)");
                command.Parameters.AddWithValue("$search", $"%{searchText.Trim()}%");
            }

            return conditions.Count == 0
                ? string.Empty
                : $"WHERE {string.Join(" AND ", conditions)}";
        }

        private static void ApplyUserRecordScope(SqliteCommand command, List<string> conditions)
        {
            if (!AuthContext.IsOfficer)
            {
                return;
            }

            conditions.Add("ProcessorName = $currentProcessorName");
            command.Parameters.AddWithValue("$currentProcessorName", AuthContext.CurrentDisplayName);
        }

        private static void EnsureAdmin()
        {
            if (!AuthContext.IsAdmin)
            {
                throw new UnauthorizedAccessException("Chỉ admin được thực hiện thao tác này.");
            }
        }

        private static void EnsureCanWriteRecords()
        {
            if (!AuthContext.CanWrite)
            {
                throw new UnauthorizedAccessException("Tài khoản hiện tại chỉ được xem dữ liệu, không được chỉnh sửa.");
            }
        }

        private static void EnsureCanCreateRecords()
        {
            if (!AuthContext.CanCreateRecord)
            {
                throw new UnauthorizedAccessException("Chi admin duoc them moi ho so.");
            }
        }

        private static void EnsureCanDeleteRecords()
        {
            if (!AuthContext.CanDeleteRecord)
            {
                throw new UnauthorizedAccessException("Chi admin duoc xoa ho so.");
            }
        }

        private static void EnsureCanEditRecord(SqliteConnection connection, SqliteTransaction transaction, int recordId)
        {
            if (AuthContext.IsAdmin)
            {
                return;
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT ProcessorName FROM Records WHERE Id = $recordId LIMIT 1;";
            command.Parameters.AddWithValue("$recordId", recordId);
            var processorName = command.ExecuteScalar()?.ToString();
            if (!AuthContext.CanEditRecord(processorName))
            {
                throw new UnauthorizedAccessException("Bạn chỉ được chỉnh sửa hồ sơ đứng dưới tên mình.");
            }
        }

        private static string GetRecordProcessorName(SqliteConnection connection, string recordCode)
        {
            if (string.IsNullOrWhiteSpace(recordCode))
            {
                return string.Empty;
            }

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT ProcessorName FROM Records WHERE RecordCode = $recordCode LIMIT 1;";
            command.Parameters.AddWithValue("$recordCode", recordCode);
            return command.ExecuteScalar()?.ToString() ?? string.Empty;
        }

        private static string NormalizeRole(string role)
        {
            return role switch
            {
                UserRoles.Admin => UserRoles.Admin,
                UserRoles.Leader => UserRoles.Leader,
                _ => UserRoles.Officer
            };
        }

        private static void AddOptionalExportFilter(SqliteCommand command, List<string> conditions, string columnName, string parameterName, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "Tất cả")
            {
                return;
            }

            conditions.Add($"{columnName} = {parameterName}");
            command.Parameters.AddWithValue(parameterName, value);
        }

        private static void AddOptionalAreaFilter(SqliteCommand command, List<string> conditions, string areaName)
        {
            if (string.IsNullOrWhiteSpace(areaName) || areaName == "Tất cả")
            {
                return;
            }

            if (!AreaSelectionOptions.IsGroupFilter(areaName))
            {
                conditions.Add("AreaName = $areaName");
                command.Parameters.AddWithValue("$areaName", areaName);
                return;
            }

            conditions.Add(@"
AreaName IN (
    SELECT CASE
        WHEN AreaType IN ('Xã', 'Phường', 'Đặc khu') THEN AreaType || ' ' || Name
        ELSE Name
    END
    FROM Areas
    WHERE
        ($areaGroup = 'Cấp xã' AND AreaType IN ('Xã', 'Phường', 'Đặc khu'))
        OR AreaType = $areaGroup
)");
            command.Parameters.AddWithValue("$areaGroup", areaName);
        }

        private static bool AreaExists(SqliteConnection connection, SqliteTransaction transaction, string areaType, string name)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT COUNT(*) FROM Areas WHERE AreaType = $type AND Name = $name;";
            command.Parameters.AddWithValue("$type", areaType);
            command.Parameters.AddWithValue("$name", name);
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        private static string FormatAreaDisplayName(string areaType, string name)
        {
            return areaType == "Xã" || areaType == "Phường" || areaType == "Đặc khu"
                ? $"{areaType} {name}"
                : name;
        }

        private static string BuildExportOrderBy(string sortOption)
        {
            return sortOption switch
            {
                "Ngày tiếp nhận cũ nhất trước" => "ReceivedDate ASC, Id ASC",
                "Trạng thái" => "Status ASC, ReceivedDate DESC, Id DESC",
                "Địa bàn" => "AreaName ASC, ReceivedDate DESC, Id DESC",
                _ => "ReceivedDate DESC, Id DESC"
            };
        }

        private static void AddProcessingCardFilter(SqliteCommand command, List<string> conditions, string cardFilterKey)
        {
            switch (cardFilterKey)
            {
                case "NeedClassify":
                    conditions.Add("Status IN ('Mới tiếp nhận', 'Đang phân loại')");
                    break;
                case "Processing":
                    conditions.Add("Status IN ('Đã phân công', 'Đang xác minh')");
                    break;
                case "Waiting":
                    conditions.Add("Status IN ('Chờ kết quả', 'Đang chờ bổ sung tài liệu')");
                    break;
                case "DueSoon":
                    conditions.Add("ExpectedResultDate <> '' AND ExpectedResultDate >= $today AND ExpectedResultDate <= $dueSoon");
                    command.Parameters.AddWithValue("$today", DateTime.Today.ToString("O", CultureInfo.InvariantCulture));
                    command.Parameters.AddWithValue("$dueSoon", DateTime.Today.AddDays(7).Date.AddDays(1).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture));
                    break;
                case "Overdue":
                    conditions.Add("ExpectedResultDate <> '' AND ExpectedResultDate < $today");
                    command.Parameters.AddWithValue("$today", DateTime.Today.ToString("O", CultureInfo.InvariantCulture));
                    break;
                case "HighPriority":
                    conditions.Add("SeverityLevel IN ('Nghiêm trọng', 'Rất nghiêm trọng', 'Đặc biệt nghiêm trọng')");
                    break;
            }
        }

        private static string BuildProcessingWorkLabel(string status)
        {
            return status switch
            {
                "Mới tiếp nhận" => "Cần phân loại",
                "Đang phân loại" => "Hoàn tất phân loại",
                "Đã phân công" => "Bắt đầu xử lý",
                "Đang xác minh" => "Cập nhật xác minh",
                "Đang chờ bổ sung tài liệu" => "Theo dõi bổ sung",
                "Chờ kết quả" => "Theo dõi kết quả",
                "Chuyển cơ quan khác" => "Theo dõi chuyển tiếp",
                _ => "Cập nhật xử lý"
            };
        }

        private static string BuildProcessingDeadlineText(string expectedResultDate)
        {
            if (!TryParseStoredDate(expectedResultDate, out var deadline))
            {
                return "Chưa có hạn xử lý";
            }

            var today = DateTime.Today;
            var days = (deadline.Date - today).Days;
            if (days < 0)
            {
                return $"Quá hạn {Math.Abs(days)} ngày";
            }

            if (days == 0)
            {
                return "Hạn hôm nay";
            }

            if (days <= 7)
            {
                return $"Còn {days} ngày";
            }

            return $"Hạn {FormatDate(expectedResultDate)}";
        }

        private static string BuildProcessingDeadlineBrush(string expectedResultDate)
        {
            if (!TryParseStoredDate(expectedResultDate, out var deadline))
            {
                return "#6B7280";
            }

            var days = (deadline.Date - DateTime.Today).Days;
            if (days < 0)
            {
                return "#D13438";
            }

            if (days <= 7)
            {
                return "#F28C18";
            }

            return "#159A72";
        }

        private static bool TryParseStoredDate(string value, out DateTime date)
        {
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out date))
            {
                return true;
            }

            return DateTime.TryParse(value, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out date);
        }

        private static int CountOpenProcessingRecords(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM Records WHERE Status <> 'Đã giải quyết'{BuildUserRecordCondition(command)};";
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static int CountDueSoonOpenRecords(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT COUNT(*)
FROM Records
WHERE Status <> 'Đã giải quyết'
  AND ExpectedResultDate <> ''
  AND ExpectedResultDate >= $today
  AND ExpectedResultDate <= $dueSoon" + BuildUserRecordCondition(command) + ";";
            command.Parameters.AddWithValue("$today", DateTime.Today.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$dueSoon", DateTime.Today.AddDays(7).Date.AddDays(1).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture));
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static int CountHighPriorityOpenRecords(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT COUNT(*)
FROM Records
WHERE Status <> 'Đã giải quyết'
  AND SeverityLevel IN ('Nghiêm trọng', 'Rất nghiêm trọng', 'Đặc biệt nghiêm trọng')" + BuildUserRecordCondition(command) + ";";
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static int CountRecordsByStatuses(SqliteConnection connection, DateTime? fromDate, DateTime? toDate, params string[] statuses)
        {
            var parameters = new List<string>();
            using var command = connection.CreateCommand();
            for (var i = 0; i < statuses.Length; i++)
            {
                var parameterName = $"$status{i}";
                parameters.Add(parameterName);
                command.Parameters.AddWithValue(parameterName, statuses[i]);
            }

            command.CommandText = $"SELECT COUNT(*) FROM Records WHERE Status IN ({string.Join(",", parameters)}){BuildScopedDateCondition(command, fromDate, toDate)};";
            AddDateParameters(command, fromDate, toDate);
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static int CountOverdueOpenRecords(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT COUNT(*)
FROM Records
WHERE Status <> 'Đã giải quyết'
  AND ExpectedResultDate <> ''
  AND ExpectedResultDate < $today" + BuildUserRecordCondition(command) + ";";
            command.Parameters.AddWithValue("$today", DateTime.Today.ToString("O", CultureInfo.InvariantCulture));
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static string BuildDateWhere(DateTime? fromDate, DateTime? toDate)
        {
            var condition = BuildDateCondition(fromDate, toDate);
            return string.IsNullOrWhiteSpace(condition)
                ? string.Empty
                : $" WHERE{condition.Substring(4)}";
        }

        private static string BuildScopedDateWhere(SqliteCommand command, DateTime? fromDate, DateTime? toDate)
        {
            var conditions = new List<string>();
            if (fromDate.HasValue)
            {
                conditions.Add("ReceivedDate >= $fromDate");
            }

            if (toDate.HasValue)
            {
                conditions.Add("ReceivedDate <= $toDate");
            }

            if (AuthContext.IsOfficer)
            {
                conditions.Add("ProcessorName = $currentProcessorName");
                command.Parameters.AddWithValue("$currentProcessorName", AuthContext.CurrentDisplayName);
            }

            return conditions.Count == 0
                ? string.Empty
                : $"WHERE {string.Join(" AND ", conditions)}";
        }

        private static string BuildScopedDateCondition(SqliteCommand command, DateTime? fromDate, DateTime? toDate)
        {
            var condition = BuildDateCondition(fromDate, toDate);
            if (!AuthContext.IsOfficer)
            {
                return condition;
            }

            command.Parameters.AddWithValue("$currentProcessorName", AuthContext.CurrentDisplayName);
            return condition + " AND ProcessorName = $currentProcessorName";
        }

        private static string BuildUserRecordCondition(SqliteCommand command)
        {
            if (!AuthContext.IsOfficer)
            {
                return string.Empty;
            }

            command.Parameters.AddWithValue("$currentProcessorName", AuthContext.CurrentDisplayName);
            return " AND ProcessorName = $currentProcessorName";
        }

        private static string BuildDateCondition(DateTime? fromDate, DateTime? toDate)
        {
            var conditions = new List<string>();
            if (fromDate.HasValue)
            {
                conditions.Add("ReceivedDate >= $fromDate");
            }

            if (toDate.HasValue)
            {
                conditions.Add("ReceivedDate <= $toDate");
            }

            return conditions.Count == 0
                ? string.Empty
                : $" AND {string.Join(" AND ", conditions)}";
        }

        private static void AddDateParameters(SqliteCommand command, DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue)
            {
                command.Parameters.AddWithValue("$fromDate", fromDate.Value.Date.ToString("O", CultureInfo.InvariantCulture));
            }

            if (toDate.HasValue)
            {
                command.Parameters.AddWithValue("$toDate", toDate.Value.Date.AddDays(1).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture));
            }
        }

        private static void ExecuteNonQuery(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static void NormalizeFutureRecordDates(SqliteConnection connection)
        {
            var now = DateTime.Now.ToString("O", CultureInfo.InvariantCulture);
            using var transaction = connection.BeginTransaction();

            foreach (var columnName in new[] { "CreatedAt", "ReceivedDate", "UpdatedAt" })
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"UPDATE Records SET {columnName} = $now WHERE {columnName} > $now;";
                command.Parameters.AddWithValue("$now", now);
                command.ExecuteNonQuery();
            }

            using var historyCommand = connection.CreateCommand();
            historyCommand.Transaction = transaction;
            historyCommand.CommandText = "UPDATE ProcessHistories SET ProcessedAt = $now WHERE ProcessedAt > $now;";
            historyCommand.Parameters.AddWithValue("$now", now);
            historyCommand.ExecuteNonQuery();

            transaction.Commit();
        }

        private static void TryAddColumn(SqliteConnection connection, string tableName, string columnName, string definition)
        {
            if (ColumnExists(connection, tableName, columnName))
            {
                return;
            }

            ExecuteNonQuery(connection, $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};");
        }

        private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName});";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogElapsed(string module, string action, Stopwatch stopwatch, int? rowCount = null)
        {
            stopwatch.Stop();
            var message = rowCount.HasValue
                ? $"Completed in {stopwatch.ElapsedMilliseconds} ms. Rows: {rowCount.Value}."
                : $"Completed in {stopwatch.ElapsedMilliseconds} ms.";
            AppLogger.Info(module, action, message);
        }

        private static string FormatDate(string value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
                ? date.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"))
                : value;
        }

        private static string FormatDateTime(string value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
                ? date.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"))
                : value;
        }

        private static DateTime ParseDisplayDate(string value)
        {
            if (DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var exactDate))
            {
                return exactDate;
            }

            return DateTime.TryParse(value, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var date)
                ? date
                : DateTime.Today;
        }

        private static string GetStatusColor(string status)
        {
            return status switch
            {
                "Đã giải quyết" => "#24A148",
                "Đang xác minh" => "#2F73FF",
                "Đang phân loại" => "#F5B132",
                "Đã phân công" => "#0B5CFF",
                "Chờ kết quả" => "#7B4DE3",
                "Đang chờ bổ sung tài liệu" => "#FF5A1F",
                "Chuyển cơ quan khác" => "#1F4AB8",
                _ => "#5C6B91"
            };
        }
    }
}
