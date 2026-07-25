using System.Windows;
using InternetLock.Services;

namespace InternetLock.Views
{
    public partial class ChangePasswordWindow : Window
    {
        private readonly IPasswordService _passwordService;
        private readonly ILoggerService _loggerService;

        public ChangePasswordWindow(IPasswordService passwordService, ILoggerService loggerService)
        {
            InitializeComponent();
            _passwordService = passwordService;
            _loggerService = loggerService;
            CurrentPasswordBox.Focus();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string current = CurrentPasswordBox.Password;
            string newPass = NewPasswordBox.Password;
            string confirmNew = ConfirmNewPasswordBox.Password;

            if (string.IsNullOrEmpty(current))
            {
                ShowError("Vui lòng nhập mật khẩu hiện tại.");
                return;
            }

            if (string.IsNullOrEmpty(newPass) || newPass.Length < 6)
            {
                ShowError("Mật khẩu mới phải có độ dài tối thiểu 6 ký tự.");
                return;
            }

            if (newPass != confirmNew)
            {
                ShowError("Mật khẩu mới và mật khẩu xác nhận không khớp.");
                return;
            }

            bool isCurrentValid = await _passwordService.VerifyPasswordAsync(current);
            if (!isCurrentValid)
            {
                ShowError("Mật khẩu hiện tại không chính xác.");
                await _loggerService.LogWarningAsync("Thử đổi mật khẩu không thành công do sai mật khẩu hiện tại.");
                return;
            }

            bool changeSuccess = await _passwordService.ChangePasswordAsync(current, newPass);
            if (changeSuccess)
            {
                MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                ShowError("Không thể thay đổi mật khẩu. Vui lòng thử lại.");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }
    }
}
