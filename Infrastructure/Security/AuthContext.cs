using System;
using System.Threading;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.Infrastructure.Security
{
    public static class AuthContext
    {
        private static readonly AsyncLocal<AppUser> ScopedUser = new AsyncLocal<AppUser>();
        private static AppUser _currentUser;

        public static AppUser CurrentUser => ScopedUser.Value ?? _currentUser;

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
            _currentUser = user;
        }

        public static void SignOut()
        {
            _currentUser = null;
        }

        public static IDisposable BeginRequestScope(AppUser user)
        {
            return new UserScope(user);
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

        private sealed class UserScope : IDisposable
        {
            private readonly AppUser _previousUser;
            private bool _isDisposed;

            public UserScope(AppUser user)
            {
                _previousUser = ScopedUser.Value;
                ScopedUser.Value = user;
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                ScopedUser.Value = _previousUser;
                _isDisposed = true;
            }
        }
    }
}
