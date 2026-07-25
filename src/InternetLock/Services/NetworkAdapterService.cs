using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using InternetLock.Models;

namespace InternetLock.Services
{
    public class NetworkAdapterService : INetworkAdapterService
    {
        private readonly ILoggerService _logger;
        private const int CommandTimeoutMs = 15000; // 15s per PowerShell command

        public NetworkAdapterService(ILoggerService logger)
        {
            _logger = logger;
        }

        public async Task<List<NetworkAdapterInfo>> GetNetworkAdaptersAsync(CancellationToken cancellationToken = default)
        {
            var adapters = new List<NetworkAdapterInfo>();

            try
            {
                // Attempt 1: Retrieve adapters using PowerShell Get-NetAdapter for comprehensive WMI/CIM info
                var psScript = "Get-NetAdapter | Select-Object Name, InterfaceDescription, InterfaceGuid, InterfaceIndex, Status, AdminStatus, MediaConnectionState, PhysicalMediaType, Virtual | ConvertTo-Json -Compress";
                var psResult = await RunPowerShellCommandAsync(psScript, cancellationToken);

                if (psResult.Success && !string.IsNullOrWhiteSpace(psResult.Output))
                {
                    adapters = ParsePowerShellAdapters(psResult.Output);
                }

                // Fallback / Enriched sync with .NET NetworkInterface if PowerShell produced 0 adapters or failed
                if (adapters.Count == 0)
                {
                    await _logger.LogWarningAsync("PowerShell Get-NetAdapter returned empty or failed. Falling back to System.Net.NetworkInformation.");
                    adapters = GetFallbackNetworkInterfaces();
                }

                // Evaluate manageable status for each adapter
                foreach (var adapter in adapters)
                {
                    adapter.IsManageable = IsManageableAdapter(adapter);
                }

                await _logger.LogInfoAsync($"Discovered {adapters.Count} total network adapters ({adapters.Count(a => a.IsManageable)} manageable).");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Failed to query network adapters.", ex);
                adapters = GetFallbackNetworkInterfaces();
                foreach (var adapter in adapters)
                {
                    adapter.IsManageable = IsManageableAdapter(adapter);
                }
            }

            return adapters;
        }

        public async Task<bool> DisableAdapterAsync(NetworkAdapterInfo adapter, CancellationToken cancellationToken = default)
        {
            if (adapter == null || string.IsNullOrWhiteSpace(adapter.Id) && string.IsNullOrWhiteSpace(adapter.InterfaceDescription))
            {
                return false;
            }

            try
            {
                string script;
                if (!string.IsNullOrWhiteSpace(adapter.InterfaceGuid))
                {
                    var cleanGuid = adapter.InterfaceGuid.Trim('{', '}');
                    script = $"Disable-NetAdapter -InterfaceGuid '{{{cleanGuid}}}' -Confirm:$false";
                }
                else
                {
                    var escapedDesc = EscapePowerShellString(adapter.InterfaceDescription);
                    script = $"Disable-NetAdapter -InterfaceDescription \"{escapedDesc}\" -Confirm:$false";
                }

                var result = await RunPowerShellCommandAsync(script, cancellationToken);
                await _logger.LogOperationAsync("DISABLE", adapter.Name, result.Success, result.Error);
                return result.Success;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Exception disabling adapter '{adapter.Name}'.", ex);
                return false;
            }
        }

        public async Task<bool> EnableAdapterAsync(NetworkAdapterInfo adapter, CancellationToken cancellationToken = default)
        {
            if (adapter == null || string.IsNullOrWhiteSpace(adapter.Id) && string.IsNullOrWhiteSpace(adapter.InterfaceDescription))
            {
                return false;
            }

            try
            {
                string script;
                if (!string.IsNullOrWhiteSpace(adapter.InterfaceGuid))
                {
                    var cleanGuid = adapter.InterfaceGuid.Trim('{', '}');
                    script = $"Enable-NetAdapter -InterfaceGuid '{{{cleanGuid}}}' -Confirm:$false";
                }
                else
                {
                    var escapedDesc = EscapePowerShellString(adapter.InterfaceDescription);
                    script = $"Enable-NetAdapter -InterfaceDescription \"{escapedDesc}\" -Confirm:$false";
                }

                var result = await RunPowerShellCommandAsync(script, cancellationToken);
                await _logger.LogOperationAsync("ENABLE", adapter.Name, result.Success, result.Error);
                return result.Success;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Exception enabling adapter '{adapter.Name}'.", ex);
                return false;
            }
        }

        public bool IsManageableAdapter(NetworkAdapterInfo adapter)
        {
            if (adapter == null) return false;

            var name = adapter.Name?.ToLowerInvariant() ?? "";
            var desc = adapter.Description?.ToLowerInvariant() ?? "";
            var interfaceDesc = adapter.InterfaceDescription?.ToLowerInvariant() ?? "";
            var type = adapter.AdapterType?.ToLowerInvariant() ?? "";

            // Exclude Loopback adapters
            if (type.Contains("loopback") || name.Contains("loopback") || desc.Contains("loopback") || interfaceDesc.Contains("loopback"))
            {
                return false;
            }

            // Exclude pseudo-kernel / internal system loopbacks
            if (name == "lo" || desc.Contains("software loopback") || interfaceDesc.Contains("software loopback"))
            {
                return false;
            }

            // Exclude Npcap Loopback or Wi-Fi Direct Virtual Adapters if dangerous to disable
            if (desc.Contains("npcap loopback") || interfaceDesc.Contains("npcap loopback"))
            {
                return false;
            }

            return true;
        }

        private List<NetworkAdapterInfo> ParsePowerShellAdapters(string jsonOutput)
        {
            var list = new List<NetworkAdapterInfo>();
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                List<PsAdapterDto> rawList;
                var trimmed = jsonOutput.Trim();

                if (trimmed.StartsWith("["))
                {
                    rawList = JsonSerializer.Deserialize<List<PsAdapterDto>>(trimmed, options) ?? new List<PsAdapterDto>();
                }
                else if (trimmed.StartsWith("{"))
                {
                    var single = JsonSerializer.Deserialize<PsAdapterDto>(trimmed, options);
                    rawList = single != null ? new List<PsAdapterDto> { single } : new List<PsAdapterDto>();
                }
                else
                {
                    return list;
                }

                foreach (var dto in rawList)
                {
                    bool isEnabled = dto.AdminStatus == 1 ||
                                     string.Equals(dto.AdminStatusStr, "Up", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(dto.Status, "Up", StringComparison.OrdinalIgnoreCase);

                    string connStatus = !string.IsNullOrWhiteSpace(dto.Status) ? dto.Status : (isEnabled ? "Enabled" : "Disabled");
                    string adapterType = DetermineAdapterType(dto.Name, dto.InterfaceDescription, dto.PhysicalMediaType, dto.Virtual);

                    var info = new NetworkAdapterInfo
                    {
                        Id = !string.IsNullOrWhiteSpace(dto.InterfaceGuid) ? dto.InterfaceGuid : dto.InterfaceIndex.ToString(),
                        Name = dto.Name ?? "Unknown Adapter",
                        Description = dto.InterfaceDescription ?? dto.Name ?? "Network Adapter",
                        InterfaceDescription = dto.InterfaceDescription ?? dto.Name ?? "",
                        InterfaceGuid = dto.InterfaceGuid ?? "",
                        InterfaceIndex = dto.InterfaceIndex,
                        IsEnabled = isEnabled,
                        ConnectionStatus = connStatus,
                        AdapterType = adapterType
                    };

                    list.Add(info);
                }
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync("Failed to parse PowerShell JSON output for adapters.", ex);
            }

            return list;
        }

        private List<NetworkAdapterInfo> GetFallbackNetworkInterfaces()
        {
            var list = new List<NetworkAdapterInfo>();
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var nic in interfaces)
                {
                    bool isEnabled = nic.OperationalStatus == OperationalStatus.Up;

                    string adapterType = nic.NetworkInterfaceType switch
                    {
                        NetworkInterfaceType.Wireless80211 => "Wi-Fi",
                        NetworkInterfaceType.Ethernet => "Ethernet",
                        NetworkInterfaceType.Tunnel => "VPN / Tunnel",
                        NetworkInterfaceType.Loopback => "Loopback",
                        _ => "Other"
                    };

                    list.Add(new NetworkAdapterInfo
                    {
                        Id = nic.Id,
                        Name = nic.Name,
                        Description = nic.Description,
                        InterfaceDescription = nic.Description,
                        InterfaceGuid = nic.Id,
                        InterfaceIndex = 0,
                        IsEnabled = isEnabled,
                        ConnectionStatus = nic.OperationalStatus.ToString(),
                        AdapterType = adapterType
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogErrorAsync("Error during fallback NetworkInterface retrieval.", ex);
            }

            return list;
        }

        private static string DetermineAdapterType(string? name, string? desc, string? mediaType, bool? isVirtual)
        {
            var combined = $"{name ?? ""} {desc ?? ""} {mediaType ?? ""}".ToLowerInvariant();

            if (combined.Contains("wi-fi") || combined.Contains("wireless") || combined.Contains("wlan") || combined.Contains("802.11"))
                return "Wi-Fi";
            if (combined.Contains("vpn") || combined.Contains("tunnel") || combined.Contains("tap") || combined.Contains("tun"))
                return "VPN";
            if (combined.Contains("hyper-v") || combined.Contains("vethernet"))
                return "Hyper-V";
            if (combined.Contains("usb"))
                return "USB Network";
            if (combined.Contains("ethernet") || combined.Contains("gigabit") || combined.Contains("realtek") || combined.Contains("intel"))
                return "Ethernet";
            if (isVirtual == true || combined.Contains("virtual"))
                return "Virtual Adapter";

            return "Network Adapter";
        }

        private static string EscapePowerShellString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("`", "``").Replace("\"", "`\"").Replace("$", "`$");
        }

        private async Task<(bool Success, string Output, string Error)> RunPowerShellCommandAsync(string script, CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using var process = new Process { StartInfo = psi };
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
                process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var processTask = process.WaitForExitAsync(cancellationToken);
                var timeoutTask = Task.Delay(CommandTimeoutMs, cancellationToken);

                var completedTask = await Task.WhenAny(processTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    try { process.Kill(); } catch { }
                    return (false, "", "Command timed out after 15 seconds.");
                }

                bool success = process.ExitCode == 0;
                return (success, outputBuilder.ToString().Trim(), errorBuilder.ToString().Trim());
            }
            catch (Exception ex)
            {
                return (false, "", ex.Message);
            }
        }

        private class PsAdapterDto
        {
            public string? Name { get; set; }
            public string? InterfaceDescription { get; set; }
            public string? InterfaceGuid { get; set; }
            public int InterfaceIndex { get; set; }
            public string? Status { get; set; }
            public int? AdminStatus { get; set; }

            [JsonPropertyName("AdminStatus")]
            public string? AdminStatusStr { get; set; }
            public string? PhysicalMediaType { get; set; }
            public bool? Virtual { get; set; }
        }
    }
}
