using System.Threading.Tasks;

namespace InternetLock.Services
{
    public interface IPasswordService
    {
        bool IsPasswordSet();
        Task<bool> SetPasswordAsync(string password);
        Task<bool> VerifyPasswordAsync(string password);
        Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
    }
}
