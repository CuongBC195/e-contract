using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.IdentityModel.Tokens;
using Shared.Constants;
using Shared.Helpers;

namespace API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return null;

        if (!PasswordHelper.VerifyPassword(password, user.PasswordHash))
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return user;
    }

    public string GenerateJwtToken(User user)
    {
        var jwtSecret = _configuration["JWT:Secret"] 
            ?? throw new InvalidOperationException("JWT Secret not configured");
        var jwtIssuer = _configuration["JWT:Issuer"] ?? "https://api.yourdomain.com";
        var jwtAudience = _configuration["JWT:Audience"] ?? "https://app.yourdomain.com";
        var expirationMinutes = int.Parse(_configuration["JWT:ExpirationMinutes"] ?? "1440");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<User> RegisterAsync(string email, string password, string name)
    {
        // Check if email already exists
        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (existingUser != null)
            throw new InvalidOperationException("Email already exists");

        // Generate 6-digit OTP
        var random = new Random();
        var otpCode = random.Next(100000, 999999).ToString();

        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHelper.HashPassword(password),
            Name = name,
            EmailVerified = false,
            OtpCode = otpCode,
            OtpExpiry = DateTime.UtcNow.AddMinutes(10), // OTP expires in 10 minutes
            Role = Domain.Enums.UserRole.User
        };

        try
        {
            return await _userRepository.CreateAsync(user);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // Handle race condition: if email was added between check and create
            // Check again to see if user was created
            var duplicateUser = await _userRepository.GetByEmailAsync(email);
            if (duplicateUser != null)
                throw new InvalidOperationException("Email already exists");
            
            // Re-throw if it's a different database error
            throw;
        }
    }

    public async Task<bool> VerifyEmailAsync(string email, string otpCode)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return false;

        // Check if OTP matches and is not expired
        if (user.OtpCode != otpCode || 
            !user.OtpExpiry.HasValue || 
            user.OtpExpiry.Value <= DateTime.UtcNow)
            return false;

        // Verify email and clear OTP
        user.EmailVerified = true;
        user.OtpCode = null;
        user.OtpExpiry = null;
        await _userRepository.UpdateAsync(user);

        return true;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
            return false; // Don't reveal if email exists

        // 🔒 SECURITY: Only allow password reset if email is verified
        if (!user.EmailVerified)
            return false; // Don't reveal that email exists but is not verified

        user.PasswordResetToken = Guid.NewGuid().ToString();
        user.PasswordResetExpiry = DateTime.UtcNow.AddHours(1);
        await _userRepository.UpdateAsync(user);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
            return false;

        if (user.PasswordResetToken != token || 
            !user.PasswordResetExpiry.HasValue || 
            user.PasswordResetExpiry.Value < DateTime.UtcNow)
            return false;

        user.PasswordHash = PasswordHelper.HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetExpiry = null;
        await _userRepository.UpdateAsync(user);

        return true;
    }

    public async Task<User> UpdateProfileAsync(Guid userId, string? name)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        if (!string.IsNullOrWhiteSpace(name))
            user.Name = name.Trim();

        user.UpdatedAt = DateTime.UtcNow;
        return await _userRepository.UpdateAsync(user);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return false;

        // Verify current password
        if (!PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash))
            return false;

        // Validate new password
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            throw new InvalidOperationException("Mật khẩu mới phải có ít nhất 6 ký tự");

        // Update password
        user.PasswordHash = PasswordHelper.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return true;
    }

    public async Task<string> RequestDeleteAccountOtpAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        // Generate 6-digit OTP
        var random = new Random();
        var otpCode = random.Next(100000, 999999).ToString();

        // Store OTP in user record
        user.OtpCode = otpCode;
        user.OtpExpiry = DateTime.UtcNow.AddMinutes(10); // OTP expires in 10 minutes
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return otpCode;
    }

    public async Task<bool> VerifyDeleteAccountOtpAsync(Guid userId, string otpCode)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return false;

        // Check if OTP matches and is not expired
        if (user.OtpCode != otpCode || 
            !user.OtpExpiry.HasValue || 
            user.OtpExpiry.Value <= DateTime.UtcNow)
            return false;

        return true;
    }
}

