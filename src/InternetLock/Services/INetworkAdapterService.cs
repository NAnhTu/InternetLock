using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InternetLock.Models;

namespace InternetLock.Services
{
    public interface INetworkAdapterService
    {
        Task<List<NetworkAdapterInfo>> GetNetworkAdaptersAsync(CancellationToken cancellationToken = default);
        Task<bool> DisableAdapterAsync(NetworkAdapterInfo adapter, CancellationToken cancellationToken = default);
        Task<bool> EnableAdapterAsync(NetworkAdapterInfo adapter, CancellationToken cancellationToken = default);
        bool IsManageableAdapter(NetworkAdapterInfo adapter);
    }
}
