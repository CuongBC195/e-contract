using Domain.Entities;

namespace API.Services;

public interface IAuthService
{
    Task<User?> AuthenticateAsync(string email, string password);
    string GenerateJwtToken(User user);
    Task<User> RegisterAsync(string email, string password, string name);
    Task<bool> VerifyEmailAsync(string email, string otpCode);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    Task<User> UpdateProfileAsync(Guid userId, string? name);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<string> RequestDeleteAccountOtpAsync(Guid userId);
    Task<bool> VerifyDeleteAccountOtpAsync(Guid userId, string otpCode);
}

