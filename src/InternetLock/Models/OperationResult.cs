using System.Collections.Generic;

namespace InternetLock.Models
{
    /// <summary>
    /// Represents the result of a batch enable or disable operation across network adapters.
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> SucceededAdapters { get; set; } = new List<string>();
        public List<string> FailedAdapters { get; set; } = new List<string>();
    }
}
