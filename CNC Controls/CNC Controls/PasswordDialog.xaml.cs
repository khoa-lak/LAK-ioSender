using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CNC.Controls
{
    public partial class PasswordDialog : Window
    {
        private bool isPasswordVisible = false;

        public PasswordDialog(string appName = null)
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(appName))
            {
                Title = $"Xác thực truy cập - {appName}";
                txtHeaderSub.Text = $"Vui lòng nhập mật khẩu để khởi chạy {appName}";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtPassword.Focus();
        }

        private string GetCurrentPassword()
        {
            return isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;
        }

        private void BtnToggleEye_Click(object sender, RoutedEventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;
            if (isPasswordVisible)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPasswordVisible.Focus();
                txtPasswordVisible.CaretIndex = txtPasswordVisible.Text.Length;
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                txtPassword.Focus();
            }
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            ValidateAndSubmit();
        }

        private void ValidateAndSubmit()
        {
            string entered = GetCurrentPassword();
            if (string.IsNullOrEmpty(entered))
            {
                ShowError("Vui lòng nhập mật khẩu!");
                return;
            }

            if (AppSecurity.VerifyPassword(entered))
            {
                if (chkRemember.IsChecked == true)
                {
                    AppSecurity.SaveRememberToken(entered);
                }
                else
                {
                    AppSecurity.ClearRememberToken();
                }

                DialogResult = true;
                Close();
            }
            else
            {
                ShowError("Mật khẩu không chính xác! Vui lòng thử lại.");
                if (isPasswordVisible)
                {
                    txtPasswordVisible.SelectAll();
                    txtPasswordVisible.Focus();
                }
                else
                {
                    txtPassword.SelectAll();
                    txtPassword.Focus();
                }
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
            txtPassword.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            txtPasswordVisible.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
        }

        private void ClearError()
        {
            txtError.Visibility = Visibility.Collapsed;
            txtPassword.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            txtPasswordVisible.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            ClearError();
            if (e.Key == Key.Enter)
            {
                ValidateAndSubmit();
            }
        }

        private void TxtPasswordVisible_KeyDown(object sender, KeyEventArgs e)
        {
            ClearError();
            if (e.Key == Key.Enter)
            {
                ValidateAndSubmit();
            }
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ClearError();
        }

        private void TxtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearError();
        }
    }
}
