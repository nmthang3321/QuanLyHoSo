using System;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.Infrastructure.Security
{
    public static class AuthContext
    {
        public static AppUser CurrentUser { get; private set; }

        public static bool IsAuthenticated => CurrentUser != null;
        public static bool IsAdmin => string.Equals(CurrentUser?.Role, UserRoles.Admin, StringComparison.Ordinal);
        public static bool IsLeader => string.Equals(CurrentUser?.Role, UserRoles.Leader, StringComparison.Ordinal);
        public static bool IsOfficer => string.Equals(CurrentUser?.Role, UserRoles.Officer, StringComparison.Ordinal);
        public static bool CanWrite => IsAdmin || IsOfficer;
        public static bool CanCreateRecord => IsAdmin;
        public static bool CanDeleteRecord => IsAdmin;
        public static bool CanManageUsers => IsAdmin;
        public static string CurrentDisplayName => CurrentUser?.DisplayName ?? string.Empty;

        public static void SignIn(AppUser user)
        {
            CurrentUser = user;
        }

        public static void SignOut()
        {
            CurrentUser = null;
        }

        public static bool CanAccessRecord(string processorName)
        {
            if (IsAdmin || IsLeader)
            {
                return true;
            }

            return IsOfficer &&
                string.Equals(
                    (processorName ?? string.Empty).Trim(),
                    CurrentDisplayName.Trim(),
                    StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool CanEditRecord(string processorName)
        {
            return IsAdmin || (IsOfficer && CanAccessRecord(processorName));
        }
    }
}
