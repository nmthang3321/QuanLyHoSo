using System;
using System.Security.Cryptography;

namespace QuanLyHoSo.Infrastructure.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100000;

        public static string HashPassword(string password)
        {
            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using var deriveBytes = new Rfc2898DeriveBytes(password ?? string.Empty, salt, Iterations, HashAlgorithmName.SHA256);
            var key = deriveBytes.GetBytes(KeySize);
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            var parts = storedHash.Split('.');
            if (parts.Length != 3 ||
                !int.TryParse(parts[0], out var iterations))
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[1]);
            var expectedKey = Convert.FromBase64String(parts[2]);
            using var deriveBytes = new Rfc2898DeriveBytes(password ?? string.Empty, salt, iterations, HashAlgorithmName.SHA256);
            var actualKey = deriveBytes.GetBytes(expectedKey.Length);
            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }
    }
}
