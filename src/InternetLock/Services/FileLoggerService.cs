using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InternetLock.Helpers;

namespace InternetLock.Services
{
    public class FileLoggerService : ILoggerService
    {
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

        public async Task LogInfoAsync(string message)
        {
            await WriteLogEntryAsync("INFO", message);
        }

        public async Task LogWarningAsync(string message)
        {
            await WriteLogEntryAsync("WARN", message);
        }

        public async Task LogErrorAsync(string message, Exception? exception = null)
        {
            var logMessage = exception != null
                ? $"{message} | Exception: {exception.GetType().Name} - {exception.Message}\n{exception.StackTrace}"
                : message;
            await WriteLogEntryAsync("ERROR", logMessage);
        }

        public async Task LogOperationAsync(string operationType, string adapterName, bool success, string details = "")
        {
            var status = success ? "SUCCESS" : "FAILED";
            var message = $"[Operation: {operationType}] Adapter: '{adapterName}' Status: {status}";
            if (!string.IsNullOrWhiteSpace(details))
            {
                message += $" | Details: {details}";
            }
            await WriteLogEntryAsync("OP", message);
        }

        private async Task WriteLogEntryAsync(string level, string message)
        {
            try
            {
                AppPaths.EnsureDirectoriesExist();
                var fileName = $"InternetLock_{DateTime.Now:yyyy-MM-dd}.log";
                var filePath = Path.Combine(AppPaths.LogsDirectory, fileName);

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var line = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";

                await Semaphore.WaitAsync();
                try
                {
                    await File.AppendAllTextAsync(filePath, line, Encoding.UTF8);
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch
            {
                // Silently ignore logging failures to prevent app crash
            }
        }
    }
}
