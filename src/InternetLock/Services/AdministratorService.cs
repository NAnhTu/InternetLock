using System.Security.Principal;

namespace InternetLock.Services
{
    public static class AdministratorService
    {
        /// <summary>
        /// Checks if the current process is running with Administrator privileges.
        /// </summary>
        public static bool IsRunAsAdmin()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}
