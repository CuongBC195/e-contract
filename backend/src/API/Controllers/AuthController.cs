using API.DTOs;
using API.Services;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService, 
        IEmailService emailService,
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _emailService = emailService;
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto request)
    {
        var user = await _authService.AuthenticateAsync(request.Email, request.Password);
        if (user == null)
            return Unauthorized(ApiResponse<AuthResponseDto>.Unauthorized("Email hoặc mật khẩu không đúng"));

        var token = _authService.GenerateJwtToken(user);

        // Set HttpOnly cookie
        Response.Cookies.Append("jwt_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(1)
        });

        var response = new AuthResponseDto
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role.ToString(),
                EmailVerified = user.EmailVerified
            }
        };

        return Ok(ApiResponse<AuthResponseDto>.Success(response, "Đăng nhập thành công"));
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse>> Register([FromBody] RegisterRequestDto request)
    {
        var user = await _authService.RegisterAsync(request.Email, request.Password, request.Name);

        // Send OTP email (DO NOT login user - they must verify OTP first)
        try
        {
            await _emailService.SendOtpEmailAsync(
                user.Email,
                user.Name,
                user.OtpCode ?? ""
            );
            
            _logger.LogInformation("OTP email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email}", user.Email);
            // Continue registration even if email fails
        }

        // DO NOT set cookie - user must verify OTP before login
        return StatusCode(201, ApiResponse.Success("Đăng ký thành công. Vui lòng kiểm tra email để lấy mã OTP xác thực tài khoản."));
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> VerifyEmail([FromBody] VerifyEmailRequestDto request)
    {
        var success = await _authService.VerifyEmailAsync(request.Email, request.OtpCode);
        if (!success)
            return BadRequest(ApiResponse<object>.Error("Mã OTP không hợp lệ hoặc đã hết hạn", new List<string> { "Mã OTP không hợp lệ hoặc đã hết hạn" }));

        // After OTP verification, get user and generate token (now user can login)
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
            return BadRequest(ApiResponse<object>.Error("Không tìm thấy người dùng", new List<string> { "Không tìm thấy người dùng" }));

        var token = _authService.GenerateJwtToken(user);

        // Set HttpOnly cookie
        Response.Cookies.Append("jwt_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(1)
        });

        var response = new AuthResponseDto
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role.ToString(),
                EmailVerified = user.EmailVerified
            }
        };

        return Ok(ApiResponse<AuthResponseDto>.Success(response, "Xác thực email thành công"));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMe()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<UserDto>.Unauthorized());

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound(ApiResponse<UserDto>.NotFound("Không tìm thấy người dùng"));

        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString(),
            EmailVerified = user.EmailVerified
        };

        return Ok(ApiResponse<UserDto>.Success(userDto, "Lấy thông tin người dùng thành công"));
    }

    [HttpPost("logout")]
    [Authorize]
    public ActionResult<ApiResponse> Logout()
    {
        Response.Cookies.Delete("jwt_token");
        return Ok(ApiResponse.Success("Đăng xuất thành công"));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse>> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var success = await _authService.RequestPasswordResetAsync(request.Email);
        
        // Always return success to prevent email enumeration
        var baseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
        
        if (success)
        {
            // Get user to send email
            var userByEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (userByEmail != null)
            {
                var user = await _authService.GetUserByIdAsync(userByEmail.Id);
                
                if (user != null && !string.IsNullOrEmpty(user.PasswordResetToken))
                {
                    try
                    {
                        var resetUrl = $"{baseUrl}/user/reset-password?token={user.PasswordResetToken}&email={Uri.EscapeDataString(user.Email)}";
                        
                        await _emailService.SendPasswordResetEmailAsync(
                            user.Email,
                            user.Name,
                            user.PasswordResetToken,
                            resetUrl
                        );
                        
                        _logger.LogInformation("Password reset email sent to {Email}", user.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                    }
                }
            }
        }
        
        // Always return success message (security best practice)
        return Ok(ApiResponse.Success("Nếu email tồn tại, chúng tôi đã gửi link đặt lại mật khẩu đến email của bạn."));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse>> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(ApiResponse<object>.Error("Mật khẩu phải có ít nhất 6 ký tự", new List<string> { "Mật khẩu phải có ít nhất 6 ký tự" }));

        var success = await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
        if (!success)
            return BadRequest(ApiResponse<object>.Error("Token không hợp lệ hoặc đã hết hạn", new List<string> { "Token không hợp lệ hoặc đã hết hạn" }));

        return Ok(ApiResponse.Success("Đặt lại mật khẩu thành công"));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<UserDto>.Unauthorized());

        try
        {
            var updatedUser = await _authService.UpdateProfileAsync(userId, request.Name);
            
            var userDto = new UserDto
            {
                Id = updatedUser.Id,
                Email = updatedUser.Email,
                Name = updatedUser.Name,
                Role = updatedUser.Role.ToString(),
                EmailVerified = updatedUser.EmailVerified
            };

            return Ok(ApiResponse<UserDto>.Success(userDto, "Cập nhật thông tin thành công"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<UserDto>.Error(ex.Message, new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile");
            return StatusCode(500, ApiResponse<UserDto>.Error("Lỗi khi cập nhật thông tin", new List<string> { ex.Message }));
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.Unauthorized());

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(ApiResponse<object>.Error("Mật khẩu mới phải có ít nhất 6 ký tự", new List<string> { "Mật khẩu mới phải có ít nhất 6 ký tự" }));

        try
        {
            var success = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
            if (!success)
                return BadRequest(ApiResponse<object>.Error("Mật khẩu hiện tại không đúng", new List<string> { "Mật khẩu hiện tại không đúng" }));

            return Ok(ApiResponse.Success("Đổi mật khẩu thành công"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Error(ex.Message, new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, ApiResponse.Error("Lỗi khi đổi mật khẩu", new List<string> { ex.Message }));
        }
    }

    [HttpPost("request-delete-otp")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> RequestDeleteAccountOtp()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.Unauthorized());

        try
        {
            var otpCode = await _authService.RequestDeleteAccountOtpAsync(userId);
            var user = await _authService.GetUserByIdAsync(userId);
            
            if (user != null)
            {
                // Send OTP email
                try
                {
                    await _emailService.SendDeleteAccountOtpEmailAsync(user.Email, user.Name, otpCode);
                    _logger.LogInformation("Delete account OTP email sent to {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send delete account OTP email to {Email}", user.Email);
                    // Continue even if email fails
                }
            }

            return Ok(ApiResponse.Success("Đã gửi mã OTP đến email của bạn. Vui lòng kiểm tra email."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Error(ex.Message, new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting delete account OTP");
            return StatusCode(500, ApiResponse.Error("Lỗi khi gửi mã OTP", new List<string> { ex.Message }));
        }
    }

    [HttpDelete("account")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> DeleteAccount([FromBody] DeleteAccountRequestDto? request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.Unauthorized());

        // Require OTP
        if (request == null || string.IsNullOrWhiteSpace(request.OtpCode))
            return BadRequest(ApiResponse.Error("Mã OTP là bắt buộc", new List<string> { "Vui lòng nhập mã OTP để xác nhận xóa tài khoản" }));

        try
        {
            // Verify OTP
            var otpValid = await _authService.VerifyDeleteAccountOtpAsync(userId, request.OtpCode);
            if (!otpValid)
                return BadRequest(ApiResponse.Error("Mã OTP không hợp lệ hoặc đã hết hạn", new List<string> { "Mã OTP không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu mã OTP mới." }));

            // Delete account
            var success = await _userRepository.DeleteAsync(userId);
            if (!success)
                return BadRequest(ApiResponse.Error("Không thể xóa tài khoản", new List<string> { "Không tìm thấy tài khoản hoặc tài khoản đã bị xóa" }));

            // Delete cookie
            Response.Cookies.Delete("jwt_token");

            return Ok(ApiResponse.Success("Đã xóa tài khoản thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account");
            return StatusCode(500, ApiResponse.Error("Lỗi khi xóa tài khoản", new List<string> { ex.Message }));
        }
    }
}
