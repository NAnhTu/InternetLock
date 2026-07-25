using System;
using System.Windows;

using System.Windows.Threading;
using InternetLock.Services;

namespace InternetLock.Views
{
    public partial class PasswordConfirmWindow : Window
    {
        private readonly IPasswordService _passwordService;
        private readonly ILoggerService _loggerService;

        private static int _failedAttempts = 0;
        private static DateTime? _lockoutEndTime = null;

        private DispatcherTimer? _countdownTimer;
        private int _secondsRemaining;

        public PasswordConfirmWindow(IPasswordService passwordService, ILoggerService loggerService)
        {
            InitializeComponent();
            _passwordService = passwordService;
            _loggerService = loggerService;

            CheckLockoutState();
            ConfirmPasswordBox.Focus();
        }

        private void CheckLockoutState()
        {
            if (_lockoutEndTime.HasValue && _lockoutEndTime.Value > DateTime.Now)
            {
                var remaining = (_lockoutEndTime.Value - DateTime.Now).TotalSeconds;
                StartLockoutTimer((int)Math.Ceiling(remaining));
            }
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lockoutEndTime.HasValue && _lockoutEndTime.Value > DateTime.Now)
            {
                return;
            }

            string inputPassword = ShowPasswordCheckBox.IsChecked == true
                ? ConfirmTextBox.Text
                : ConfirmPasswordBox.Password;

            if (string.IsNullOrEmpty(inputPassword))
            {
                ShowError("Vui lòng nhập mật khẩu.");
                return;
            }

            bool isValid = await _passwordService.VerifyPasswordAsync(inputPassword);
            if (isValid)
            {
                _failedAttempts = 0;
                _lockoutEndTime = null;
                DialogResult = true;
                Close();
            }
            else
            {
                _failedAttempts++;
                await _loggerService.LogWarningAsync($"Nhập sai mật khẩu lần {_failedAttempts}.");

                if (_failedAttempts >= 5)
                {
                    _lockoutEndTime = DateTime.Now.AddSeconds(30);
                    StartLockoutTimer(30);
                }
                else
                {
                    ShowError($"Mật khẩu không chính xác. Bạn còn {5 - _failedAttempts} lần thử.");
                    ConfirmPasswordBox.Clear();
                    ConfirmTextBox.Clear();
                }
            }
        }

        private void StartLockoutTimer(int seconds)
        {
            _secondsRemaining = seconds;
            ConfirmBtn.IsEnabled = false;
            ConfirmPasswordBox.IsEnabled = false;
            ConfirmTextBox.IsEnabled = false;

            ShowError($"Nhập sai quá 5 lần. Chức năng bị khóa trong {_secondsRemaining} giây.");

            _countdownTimer?.Stop();
            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _countdownTimer.Tick += (s, e) =>
            {
                _secondsRemaining--;
                if (_secondsRemaining <= 0)
                {
                    _countdownTimer.Stop();
                    _lockoutEndTime = null;
                    ConfirmBtn.IsEnabled = true;
                    ConfirmPasswordBox.IsEnabled = true;
                    ConfirmTextBox.IsEnabled = true;
                    ErrorBorder.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ShowError($"Nhập sai quá 5 lần. Chức năng bị khóa trong {_secondsRemaining} giây.");
                }
            };

            _countdownTimer.Start();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowPasswordCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (ShowPasswordCheckBox.IsChecked == true)
            {
                ConfirmTextBox.Text = ConfirmPasswordBox.Password;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
                ConfirmTextBox.Visibility = Visibility.Visible;
                ConfirmTextBox.Focus();
                ConfirmTextBox.CaretIndex = ConfirmTextBox.Text.Length;
            }
            else
            {
                ConfirmPasswordBox.Password = ConfirmTextBox.Text;
                ConfirmTextBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordBox.Visibility = Visibility.Visible;
                ConfirmPasswordBox.Focus();
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }
    }
}
