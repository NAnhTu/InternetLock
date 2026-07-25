namespace InternetLock.Models
{
    /// <summary>
    /// Represents detailed information about a network adapter in the system.
    /// </summary>
    public class NetworkAdapterInfo
    {
        /// <summary>
        /// Unique Identifier for the adapter (Interface GUID or Index-based ID).
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the network adapter (e.g., "Wi-Fi", "Ethernet").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the physical or virtual hardware device.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Exact Windows Interface Description used in PowerShell cmdlets.
        /// </summary>
        public string InterfaceDescription { get; set; } = string.Empty;

        /// <summary>
        /// Interface GUID if available.
        /// </summary>
        public string InterfaceGuid { get; set; } = string.Empty;

        /// <summary>
        /// Windows Interface Index.
        /// </summary>
        public int InterfaceIndex { get; set; }

        /// <summary>
        /// True if the adapter is currently Enabled; False if Disabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Human-readable connection status (e.g., "Connected", "Disconnected", "Disabled").
        /// </summary>
        public string ConnectionStatus { get; set; } = string.Empty;

        /// <summary>
        /// Classification type (e.g., "Ethernet", "Wi-Fi", "VPN", "Virtual", "Hyper-V", "USB", "Other").
        /// </summary>
        public string AdapterType { get; set; } = string.Empty;

        /// <summary>
        /// Determines whether this adapter can be safely managed (enabled/disabled) by InternetLock.
        /// </summary>
        public bool IsManageable { get; set; }
    }
}
