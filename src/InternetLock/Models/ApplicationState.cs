namespace InternetLock.Models
{
    /// <summary>
    /// Enum representing the overall status of Internet connectivity based on manageable adapters.
    /// </summary>
    public enum InternetStatus
    {
        /// <summary>
        /// Internet đang được bật (All manageable adapters enabled)
        /// </summary>
        FullyEnabled,

        /// <summary>
        /// Internet đang bị khóa (All manageable adapters disabled)
        /// </summary>
        FullyDisabled,

        /// <summary>
        /// Một phần card mạng đang hoạt động (Some manageable adapters enabled, others disabled)
        /// </summary>
        PartiallyEnabled,

        /// <summary>
        /// Trạng thái chưa xác định hoặc đang tải
        /// </summary>
        Unknown
    }
}
