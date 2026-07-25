using System.Windows;
using InternetLock.Services;

namespace InternetLock.Views
{
    public partial class PasswordSetupWindow : Window
    {
        private readonly IPasswordService _passwordService;

        public PasswordSetupWindow(IPasswordService passwordService)
        {
            InitializeComponent();
            _passwordService = passwordService;
            NewPasswordBox.Focus();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string password = NewPasswordBox.Password;
            string confirm = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Vui lòng nhập mật khẩu.");
                return;
            }

            if (password.Length < 6)
            {
                ShowError("Mật khẩu phải có độ dài tối thiểu 6 ký tự.");
                return;
            }

            if (password != confirm)
            {
                ShowError("Mật khẩu xác nhận không khớp.");
                return;
            }

            bool success = await _passwordService.SetPasswordAsync(password);
            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ShowError("Lưu mật khẩu thất bại. Vui lòng thử lại.");
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
