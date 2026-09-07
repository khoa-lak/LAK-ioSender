using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using CNC.Core;

namespace CNC.Controls
{
    public static class AppSecurity
    {
        private static string GetStoragePath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDir = Path.Combine(localAppData, AppSecurityConfig.AppName);
            if (!Directory.Exists(appDir))
            {
                Directory.CreateDirectory(appDir);
            }
            return Path.Combine(appDir, "license.dat");
        }

        private static string ComputeHash(string input)
        {
            if (input == null) input = string.Empty;
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input + "_LAK_SALT_2026"));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static bool VerifyPassword(string input)
        {
            if (!AppSecurityConfig.IsPasswordRequired)
                return true;

            string expected = AppSecurityConfig.ExpectedPassword;
            if (string.IsNullOrEmpty(expected))
                return true;

            return string.Equals(input, expected, StringComparison.Ordinal);
        }

        public static bool HasValidSavedToken()
        {
            if (!AppSecurityConfig.IsPasswordRequired)
                return true;

            try
            {
                string path = GetStoragePath();
                if (File.Exists(path))
                {
                    string savedHash = File.ReadAllText(path).Trim();
                    string expectedHash = ComputeHash(AppSecurityConfig.ExpectedPassword);
                    return string.Equals(savedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                // Fallback to prompting password on error
            }
            return false;
        }

        public static void SaveRememberToken(string password)
        {
            try
            {
                string path = GetStoragePath();
                string hash = ComputeHash(password);
                File.WriteAllText(path, hash);
            }
            catch
            {
                // Ignore storage write errors
            }
        }

        public static void ClearRememberToken()
        {
            try
            {
                string path = GetStoragePath();
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Ignore delete errors
            }
        }

        /// <summary>
        /// Thực hiện xác thực bảo mật khi mở ứng dụng
        /// </summary>
        public static bool Authenticate(string appName = null)
        {
            if (!AppSecurityConfig.IsPasswordRequired)
                return true;

            if (HasValidSavedToken())
                return true;

            PasswordDialog dlg = new PasswordDialog(appName ?? AppSecurityConfig.AppName);
            bool? result = dlg.ShowDialog();
            return result == true;
        }
    }
}
