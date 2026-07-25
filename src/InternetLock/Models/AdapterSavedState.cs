using System;

namespace InternetLock.Models
{
    /// <summary>
    /// Represents the persisted state of an adapter before it was disabled by InternetLock.
    /// </summary>
    public class AdapterSavedState
    {
        public string AdapterId { get; set; } = string.Empty;
        public string AdapterName { get; set; } = string.Empty;
        public string InterfaceGuid { get; set; } = string.Empty;
        public string InterfaceDescription { get; set; } = string.Empty;
        public bool WasEnabledBeforeLock { get; set; }
        public DateTime DisabledTimestamp { get; set; }
        public bool DisableSuccessful { get; set; }
    }
}
