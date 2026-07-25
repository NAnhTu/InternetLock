using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using InternetLock.Commands;
using InternetLock.Models;
using InternetLock.Services;
using InternetLock.Views;

namespace InternetLock.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly INetworkAdapterService _adapterService;
        private readonly IPasswordService _passwordService;
        private readonly IStateStorageService _stateStorageService;
        private readonly ILoggerService _loggerService;

        private ObservableCollection<NetworkAdapterInfo> _adapters = new ObservableCollection<NetworkAdapterInfo>();
        private InternetStatus _currentStatus = InternetStatus.Unknown;
        private string _statusText = "Đang kiểm tra trạng thái...";
        private string _statusDescription = "Vui lòng chờ trong khi tải danh sách card mạng.";
        private bool _isBusy;
        private string _busyMessage = "Đang xử lý...";
        private bool _isToggleChecked;
        private string? _lastOperationSummary;

        public MainViewModel(
            INetworkAdapterService adapterService,
            IPasswordService passwordService,
            IStateStorageService stateStorageService,
            ILoggerService loggerService)
        {
            _adapterService = adapterService;
            _passwordService = passwordService;
            _stateStorageService = stateStorageService;
            _loggerService = loggerService;

            Adapters = new ObservableCollection<NetworkAdapterInfo>();

            RefreshCommand = new AsyncRelayCommand(RefreshAdaptersAsync, () => !IsBusy);
            ToggleLockCommand = new AsyncRelayCommand(ExecuteToggleLockAsync, () => !IsBusy);
            DisableAllCommand = new AsyncRelayCommand(ExecuteDisableAllAsync, () => !IsBusy);
            EnableSavedCommand = new AsyncRelayCommand(ExecuteEnableSavedAsync, () => !IsBusy);
            ChangePasswordCommand = new AsyncRelayCommand(ExecuteChangePasswordAsync, () => !IsBusy);
            CloseAppCommand = new RelayCommand(ExecuteCloseApp);
        }

        public ObservableCollection<NetworkAdapterInfo> Adapters
        {
            get => _adapters;
            set => SetProperty(ref _adapters, value);
        }

        public InternetStatus CurrentStatus
        {
            get => _currentStatus;
            set => SetProperty(ref _currentStatus, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string StatusDescription
        {
            get => _statusDescription;
            set => SetProperty(ref _statusDescription, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        public bool IsToggleChecked
        {
            get => _isToggleChecked;
            set => SetProperty(ref _isToggleChecked, value);
        }

        public string? LastOperationSummary
        {
            get => _lastOperationSummary;
            set => SetProperty(ref _lastOperationSummary, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ToggleLockCommand { get; }
        public ICommand DisableAllCommand { get; }
        public ICommand EnableSavedCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand CloseAppCommand { get; }

        public async Task InitializeAsync()
        {
            // Ensure password is configured on first run
            if (!_passwordService.IsPasswordSet())
            {
                var setupWindow = new PasswordSetupWindow(_passwordService);
                bool? setupResult = setupWindow.ShowDialog();
                if (setupResult != true)
                {
                    Application.Current.Shutdown();
                    return;
                }
            }

            await RefreshAdaptersAsync();
        }

        public async Task RefreshAdaptersAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            BusyMessage = "Đang tải danh sách card mạng...";

            try
            {
                var list = await _adapterService.GetNetworkAdaptersAsync();
                Adapters.Clear();
                foreach (var item in list)
                {
                    Adapters.Add(item);
                }

                UpdateStatusFromAdapters();
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync("Lỗi khi tải danh sách adapter.", ex);
                StatusText = "Lỗi tải card mạng";
                StatusDescription = "Không thể lấy thông tin card mạng từ hệ thống.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateStatusFromAdapters()
        {
            var manageable = Adapters.Where(a => a.IsManageable).ToList();

            if (manageable.Count == 0)
            {
                CurrentStatus = InternetStatus.Unknown;
                StatusText = "Không tìm thấy card mạng hợp lệ";
                StatusDescription = "Hệ thống không tìm thấy card mạng nào có thể quản lý.";
                IsToggleChecked = false;
                return;
            }

            int enabledCount = manageable.Count(a => a.IsEnabled);
            int totalCount = manageable.Count;

            if (enabledCount == totalCount)
            {
                CurrentStatus = InternetStatus.FullyEnabled;
                StatusText = "Internet đang được bật";
                StatusDescription = $"Toàn bộ {totalCount} card mạng hợp lệ đang hoạt động bình thường.";
                IsToggleChecked = true;
            }
            else if (enabledCount == 0)
            {
                CurrentStatus = InternetStatus.FullyDisabled;
                StatusText = "Internet đang bị khóa";
                StatusDescription = $"Tất cả {totalCount} card mạng hợp lệ đã bị vô hiệu hóa.";
                IsToggleChecked = false;
            }
            else
            {
                CurrentStatus = InternetStatus.PartiallyEnabled;
                StatusText = "Một phần card mạng đang hoạt động";
                StatusDescription = $"{enabledCount}/{totalCount} card mạng đang bật. Bạn có thể khóa toàn bộ hoặc mở lại card mạng do ứng dụng đã tắt.";
                IsToggleChecked = true;
            }
        }

        private async Task ExecuteToggleLockAsync()
        {
            if (IsBusy) return;

            // Target state: If currently Enabled or PartiallyEnabled, user wants to LOCK (OFF).
            // If currently Disabled, user wants to UNLOCK (ON).
            if (CurrentStatus == InternetStatus.FullyEnabled || CurrentStatus == InternetStatus.PartiallyEnabled)
            {
                await ExecuteDisableAllAsync();
            }
            else
            {
                await ExecuteEnableSavedAsync();
            }
        }

        public async Task ExecuteDisableAllAsync()
        {
            // Confirmation dialog
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn khóa toàn bộ kết nối Internet không?",
                "Xác nhận khóa Internet",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                UpdateStatusFromAdapters();
                return;
            }

            IsBusy = true;
            BusyMessage = "Đang vô hiệu hóa các card mạng...";

            var succeededList = new List<string>();
            var failedList = new List<string>();
            var savedStates = new List<AdapterSavedState>();

            try
            {
                var manageableAdapters = Adapters.Where(a => a.IsManageable).ToList();
                var disabledTime = DateTime.Now;

                foreach (var adapter in manageableAdapters)
                {
                    bool originalState = adapter.IsEnabled;
                    bool disableSuccess = false;

                    if (adapter.IsEnabled)
                    {
                        disableSuccess = await _adapterService.DisableAdapterAsync(adapter);
                        if (disableSuccess)
                        {
                            succeededList.Add(adapter.Name);
                        }
                        else
                        {
                            failedList.Add(adapter.Name);
                        }
                    }

                    // Record state regardless to know which ones were touched by application
                    savedStates.Add(new AdapterSavedState
                    {
                        AdapterId = adapter.Id,
                        AdapterName = adapter.Name,
                        InterfaceGuid = adapter.InterfaceGuid,
                        InterfaceDescription = adapter.InterfaceDescription,
                        WasEnabledBeforeLock = originalState,
                        DisabledTimestamp = disabledTime,
                        DisableSuccessful = disableSuccess || !originalState
                    });
                }

                // Persist state to JSON
                await _stateStorageService.SaveAdapterStateAsync(savedStates);

                string summary = $"Đã tắt thành công: {succeededList.Count} card mạng.";
                if (failedList.Count > 0)
                {
                    summary += $" Thất bại: {string.Join(", ", failedList)}.";
                }
                LastOperationSummary = summary;

                await _loggerService.LogInfoAsync($"Internet Lock OFF completed. Succeeded: {succeededList.Count}, Failed: {failedList.Count}");
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync("Lỗi trong quá trình tắt card mạng.", ex);
                MessageBox.Show($"Xảy ra lỗi khi vô hiệu hóa card mạng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                await RefreshAdaptersAsync();
            }
        }

        public async Task ExecuteEnableSavedAsync()
        {
            // Prompt password dialog first
            var confirmWindow = new PasswordConfirmWindow(_passwordService, _loggerService);
            bool? dialogResult = confirmWindow.ShowDialog();

            if (dialogResult != true)
            {
                // User cancelled or authentication failed
                UpdateStatusFromAdapters();
                return;
            }

            IsBusy = true;
            BusyMessage = "Mật khẩu chính xác. Đang kích hoạt lại card mạng...";

            var succeededList = new List<string>();
            var failedList = new List<string>();

            try
            {
                // Load previously saved states
                var savedStates = await _stateStorageService.LoadAdapterStateAsync();
                var currentAdapters = await _adapterService.GetNetworkAdaptersAsync();

                if (savedStates.Count == 0)
                {
                    // Fallback: If state file is empty, enable all manageable adapters currently disabled
                    var disabledAdapters = currentAdapters.Where(a => a.IsManageable && !a.IsEnabled).ToList();
                    foreach (var adapter in disabledAdapters)
                    {
                        bool success = await _adapterService.EnableAdapterAsync(adapter);
                        if (success) succeededList.Add(adapter.Name);
                        else failedList.Add(adapter.Name);
                    }
                }
                else
                {
                    // Only enable adapters that were originally enabled before InternetLock turned them OFF
                    foreach (var state in savedStates)
                    {
                        if (!state.WasEnabledBeforeLock) continue;

                        // Match current adapter by InterfaceGuid, Id, or Description
                        var match = currentAdapters.FirstOrDefault(a =>
                            (!string.IsNullOrEmpty(state.InterfaceGuid) && string.Equals(a.InterfaceGuid, state.InterfaceGuid, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(state.AdapterId) && string.Equals(a.Id, state.AdapterId, StringComparison.OrdinalIgnoreCase)) ||
                            string.Equals(a.InterfaceDescription, state.InterfaceDescription, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(a.Name, state.AdapterName, StringComparison.OrdinalIgnoreCase));

                        if (match != null)
                        {
                            bool success = await _adapterService.EnableAdapterAsync(match);
                            if (success) succeededList.Add(match.Name);
                            else failedList.Add(match.Name);
                        }
                    }
                }

                string summary = $"Đã bật thành công: {succeededList.Count} card mạng.";
                if (failedList.Count > 0)
                {
                    summary += $" Thất bại: {string.Join(", ", failedList)}.";
                }
                LastOperationSummary = summary;

                await _loggerService.LogInfoAsync($"Internet Lock ON completed. Succeeded: {succeededList.Count}, Failed: {failedList.Count}");
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync("Lỗi trong quá trình bật lại card mạng.", ex);
                MessageBox.Show($"Xảy ra lỗi khi mở lại card mạng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                await RefreshAdaptersAsync();
            }
        }

        private async Task ExecuteChangePasswordAsync()
        {
            var changeWindow = new ChangePasswordWindow(_passwordService, _loggerService);
            changeWindow.ShowDialog();
            await Task.CompletedTask;
        }

        private void ExecuteCloseApp()
        {
            Application.Current.Shutdown();
        }
    }
}
