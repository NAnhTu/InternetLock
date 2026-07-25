using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using InternetLock.Helpers;
using InternetLock.Models;

namespace InternetLock.Services
{
    public class StateStorageService : IStateStorageService
    {
        private readonly ILoggerService _logger;

        public StateStorageService(ILoggerService logger)
        {
            _logger = logger;
        }

        public async Task SaveAdapterStateAsync(List<AdapterSavedState> states)
        {
            try
            {
                AppPaths.EnsureDirectoriesExist();
                await JsonFileHelper.SaveAtomicAsync(AppPaths.AdapterStateFilePath, states);
                await _logger.LogInfoAsync($"Saved adapter state ({states.Count} items) to {AppPaths.AdapterStateFilePath}");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Failed to save adapter state to file.", ex);
            }
        }

        public async Task<List<AdapterSavedState>> LoadAdapterStateAsync()
        {
            try
            {
                var result = await JsonFileHelper.LoadAsync<List<AdapterSavedState>>(AppPaths.AdapterStateFilePath);
                return result ?? new List<AdapterSavedState>();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Failed to load adapter state from file.", ex);
                return new List<AdapterSavedState>();
            }
        }

        public async Task ClearAdapterStateAsync()
        {
            try
            {
                if (File.Exists(AppPaths.AdapterStateFilePath))
                {
                    File.Delete(AppPaths.AdapterStateFilePath);
                    await _logger.LogInfoAsync("Cleared adapter state file.");
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Failed to clear adapter state file.", ex);
            }
        }
    }
}
