using System;
using System.IO;

namespace InternetLock.Helpers
{
    /// <summary>
    /// Provides application folder paths and ensures their existence.
    /// </summary>
    public static class AppPaths
    {
        public static string BaseDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InternetLock");

        public static string AdapterStateFilePath =>
            Path.Combine(BaseDirectory, "adapter-state.json");

        public static string PasswordFilePath =>
            Path.Combine(BaseDirectory, "password.dat");

        public static string LogsDirectory =>
            Path.Combine(BaseDirectory, "Logs");

        public static void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(BaseDirectory))
            {
                Directory.CreateDirectory(BaseDirectory);
            }

            if (!Directory.Exists(LogsDirectory))
            {
                Directory.CreateDirectory(LogsDirectory);
            }
        }
    }
}
