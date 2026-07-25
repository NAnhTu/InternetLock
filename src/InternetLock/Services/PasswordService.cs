using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using InternetLock.Helpers;

namespace InternetLock.Services
{
    public class PasswordService : IPasswordService
    {
        private const int SaltSize = 16; // 128 bits
        private const int HashSize = 32; // 256 bits
        private const int Iterations = 100_000;
        private readonly ILoggerService _logger;

        public PasswordService(ILoggerService logger)
        {
            _logger = logger;
        }

        public bool IsPasswordSet()
        {
            return File.Exists(AppPaths.PasswordFilePath) && new FileInfo(AppPaths.PasswordFilePath).Length > 0;
        }

        public async Task<bool> SetPasswordAsync(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return false;
            }

            try
            {
                AppPaths.EnsureDirectoriesExist();

                byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
                byte[] hash = KeyDerivation(password, salt);

                // Combine salt (16 bytes) + hash (32 bytes) = 48 bytes
                byte[] payload = new byte[SaltSize + HashSize];
                Array.Copy(salt, 0, payload, 0, SaltSize);
                Array.Copy(hash, 0, payload, SaltSize, HashSize);

                // Protect using DPAPI CurrentUser scope
                byte[] protectedData = ProtectedData.Protect(payload, null, DataProtectionScope.CurrentUser);

                await File.WriteAllBytesAsync(AppPaths.PasswordFilePath, protectedData);
                await _logger.LogInfoAsync("Application password set successfully.");
                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error setting application password.", ex);
                return false;
            }
        }

        public async Task<bool> VerifyPasswordAsync(string password)
        {
            if (!IsPasswordSet() || string.IsNullOrEmpty(password))
            {
                return false;
            }

            try
            {
                byte[] protectedData = await File.ReadAllBytesAsync(AppPaths.PasswordFilePath);
                byte[] payload = ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);

                if (payload.Length != SaltSize + HashSize)
                {
                    await _logger.LogWarningAsync("Invalid password data payload size.");
                    return false;
                }

                byte[] salt = new byte[SaltSize];
                byte[] storedHash = new byte[HashSize];

                Array.Copy(payload, 0, salt, 0, SaltSize);
                Array.Copy(payload, SaltSize, storedHash, 0, HashSize);

                byte[] computedHash = KeyDerivation(password, salt);

                bool isMatch = CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
                if (!isMatch)
                {
                    await _logger.LogWarningAsync("Failed password verification attempt.");
                }
                return isMatch;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error verifying application password.", ex);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            bool isCurrentValid = await VerifyPasswordAsync(currentPassword);
            if (!isCurrentValid)
            {
                return false;
            }

            return await SetPasswordAsync(newPassword);
        }

        private static byte[] KeyDerivation(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(HashSize);
        }
    }
}
