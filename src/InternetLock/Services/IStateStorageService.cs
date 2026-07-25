using System.Collections.Generic;
using System.Threading.Tasks;
using InternetLock.Models;

namespace InternetLock.Services
{
    public interface IStateStorageService
    {
        Task SaveAdapterStateAsync(List<AdapterSavedState> states);
        Task<List<AdapterSavedState>> LoadAdapterStateAsync();
        Task ClearAdapterStateAsync();
    }
}
