using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.Infrastructure.Network
{
    public sealed class LanDataServer
    {
        private readonly AppDataService _dataService;
        private readonly HttpListener _listener = new HttpListener();
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        private CancellationTokenSource _cancellationTokenSource;

        public LanDataServer(AppDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            var prefix = AppPathSettings.Current.AdminServerUrl.TrimEnd('/') + "/";
            _listener.Prefixes.Add(prefix);
        }

        public void Start()
        {
            if (_listener.IsListening)
            {
                return;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _listener.Start();
                _ = Task.Run(() => ListenAsync(_cancellationTokenSource.Token));
                AppLogger.Info("LAN", "StartServer", $"Admin LAN server is listening at {AppPathSettings.Current.AdminServerUrl}.");
            }
            catch (Exception ex)
            {
                AppLogger.Error("LAN", "StartServer", ex, "Cannot start admin LAN server.");
            }
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleAsync(context), cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (HttpListenerException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    AppLogger.Error("LAN", "Listen", ex, "LAN server listener failed.");
                }
            }
        }

        private async Task HandleAsync(HttpListenerContext context)
        {
            try
            {
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteErrorAsync(context, 405, "Only POST is supported.");
                    return;
                }

                var route = context.Request.Url.AbsolutePath.Trim('/').Replace("api/", string.Empty);
                var result = Dispatch(route, await ReadBodyAsync(context.Request));
                await WriteJsonAsync(context, result);
            }
            catch (Exception ex)
            {
                AppLogger.Error("LAN", "HandleRequest", ex, "LAN request failed.");
                await WriteErrorAsync(context, 500, ex.Message);
            }
        }

        private object Dispatch(string route, string body)
        {
            if (string.Equals(route, "health", StringComparison.OrdinalIgnoreCase))
            {
                return new { ok = true, machine = Environment.MachineName };
            }

            var previousUser = AuthContext.CurrentUser;
            try
            {
                var user = ReadEnvelopeUser(body);
                if (user != null)
                {
                    AuthContext.SignIn(user);
                }
                else
                {
                    AuthContext.SignOut();
                }

                switch (route)
                {
                    case "auth/login":
                        var login = ReadData<LoginRequest>(body);
                        return _dataService.AuthenticateUser(login.UserName, login.Password);
                    case "catalog/areas":
                        return _dataService.GetAreaNames(ReadData<IncludeAllRequest>(body).IncludeAll);
                    case "catalog/values":
                        var catalog = ReadData<CatalogValuesRequest>(body);
                        return _dataService.GetCatalogValues(catalog.CatalogType, catalog.IncludeAll);
                    case "catalog/processors":
                        return _dataService.GetProcessorNames(ReadData<IncludeAllRequest>(body).IncludeAll);
                    case "dashboard/metrics":
                        var metrics = ReadData<DashboardMetricsRequest>(body);
                        return _dataService.GetDashboardMetrics(metrics.FromDate, metrics.ToDate, metrics.PreviousFromDate, metrics.PreviousToDate);
                    case "dashboard/status":
                        var status = ReadData<DateRangeRequest>(body);
                        return _dataService.GetStatusStats(status.FromDate, status.ToDate);
                    case "dashboard/areas":
                        var areas = ReadData<TopAreasRequest>(body);
                        return _dataService.GetTopAreas(areas.Take, areas.FromDate, areas.ToDate);
                    case "dashboard/trend":
                        var trend = ReadData<DateRangeRequest>(body);
                        return _dataService.GetReceivedTrendStats(trend.FromDate, trend.ToDate);
                    case "dashboard/recent":
                        var recent = ReadData<RecentRecordsRequest>(body);
                        return _dataService.GetRecentRecords(recent.Take, recent.FromDate, recent.ToDate, recent.Skip);
                    case "records/list":
                        var list = ReadData<FilteredRecordsRequest>(body);
                        return _dataService.GetFilteredRecords(list.FromDate, list.ToDate, list.Status, list.CaseType, list.Field, list.AreaName, list.ProcessorName, list.SearchText, list.SortOption, list.Take, list.Skip);
                    case "records/count":
                        var count = ReadData<FilteredRecordsRequest>(body);
                        return _dataService.CountFilteredRecords(count.FromDate, count.ToDate, count.Status, count.CaseType, count.Field, count.AreaName, count.ProcessorName, count.SearchText);
                    case "records/export-preview":
                        var export = ReadData<FilteredRecordsRequest>(body);
                        return _dataService.GetExportPreview(export.FromDate, export.ToDate, export.Status, export.CaseType, export.Field, export.AreaName, export.ProcessorName, export.SearchText, export.SortOption, export.Take);
                    case "records/export-count":
                        var exportCount = ReadData<FilteredRecordsRequest>(body);
                        return _dataService.CountExportRecords(exportCount.FromDate, exportCount.ToDate, exportCount.Status, exportCount.CaseType, exportCount.Field, exportCount.AreaName, exportCount.ProcessorName, exportCount.SearchText);
                    case "records/detail":
                        return _dataService.GetRecordForm(ReadData<RecordCodeRequest>(body).RecordCode);
                    case "records/delete":
                        return _dataService.DeleteRecord(ReadData<RecordCodeRequest>(body).RecordCode);
                    case "records/total":
                        var total = ReadData<DateRangeRequest>(body);
                        return _dataService.CountRecords(total.FromDate, total.ToDate);
                    case "processing/metrics":
                        return _dataService.GetProcessingQueueMetrics();
                    case "processing/list":
                        var queue = ReadData<ProcessingQueueRequest>(body);
                        return _dataService.GetProcessingQueueRecords(queue.SearchText, queue.Status, queue.AreaName, queue.SeverityLevel, queue.CardFilterKey, queue.Skip, queue.Take);
                    case "processing/count":
                        var queueCount = ReadData<ProcessingQueueRequest>(body);
                        return _dataService.CountProcessingQueueRecords(queueCount.SearchText, queueCount.Status, queueCount.AreaName, queueCount.SeverityLevel, queueCount.CardFilterKey);
                    case "processing/detail":
                        return _dataService.GetProcessingRecordDetail(ReadData<RecordCodeRequest>(body).RecordCode);
                    case "processing/update":
                        var update = ReadData<UpdateProcessingRequest>(body);
                        _dataService.UpdateProcessingRecord(update.RecordCode, update.Status, update.ProcessedAt, update.ProcessorName, update.Content, update.Note, update.TransferAreaName, update.Attachments, update.GenerateInitialResultDocuments);
                        return true;
                    case "staff/performance":
                        var staffPerformance = ReadData<DateRangeRequest>(body);
                        return _dataService.GetStaffPerformanceRows(staffPerformance.FromDate, staffPerformance.ToDate);
                    case "staff/deadlines":
                        var staffDeadlines = ReadData<DateRangeRequest>(body);
                        return _dataService.GetStaffDeadlineStats(staffDeadlines.FromDate, staffDeadlines.ToDate);
                    case "staff/active-records":
                        var staffActiveRecords = ReadData<StaffActiveRecordsRequest>(body);
                        return _dataService.GetStaffActiveRecords(staffActiveRecords.ProcessorName, staffActiveRecords.FromDate, staffActiveRecords.ToDate, staffActiveRecords.Take);
                    case "leadership-notices/latest":
                        var noticeRequest = ReadData<LeadershipNoticeRequest>(body);
                        var notice = _dataService.GetLatestLeadershipNotice(noticeRequest.OfficerName);
                        return new LeadershipNoticeResponse { Message = notice.Message, ReceivedText = notice.ReceivedText };
                    case "leadership-notices/save":
                        var saveNotice = ReadData<SaveLeadershipNoticeRequest>(body);
                        _dataService.SaveLeadershipNotice(saveNotice.Scope, saveNotice.TargetName, saveNotice.KpiTarget, saveNotice.Message);
                        return true;
                    default:
                        throw new InvalidOperationException($"Unknown LAN API route: {route}");
                }
            }
            finally
            {
                if (previousUser == null)
                {
                    AuthContext.SignOut();
                }
                else
                {
                    AuthContext.SignIn(previousUser);
                }
            }
        }

        private AppUser ReadEnvelopeUser(string body)
        {
            return JsonSerializer.Deserialize<LanApiEnvelope<JsonElement>>(body, _jsonOptions)?.User;
        }

        private T ReadData<T>(string body)
        {
            var envelope = JsonSerializer.Deserialize<LanApiEnvelope<T>>(body, _jsonOptions);
            return envelope == null ? default : envelope.Data;
        }

        private static async Task<string> ReadBodyAsync(HttpListenerRequest request)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        private async Task WriteJsonAsync(HttpListenerContext context, object value)
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
        }

        private static async Task WriteErrorAsync(HttpListenerContext context, int statusCode, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message ?? string.Empty);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
        }
    }
}
