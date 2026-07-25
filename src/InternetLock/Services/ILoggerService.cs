using System;
using System.Threading.Tasks;

namespace InternetLock.Services
{
    public interface ILoggerService
    {
        Task LogInfoAsync(string message);
        Task LogWarningAsync(string message);
        Task LogErrorAsync(string message, Exception? exception = null);
        Task LogOperationAsync(string operationType, string adapterName, bool success, string details = "");
    }
}
