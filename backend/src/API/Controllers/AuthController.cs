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
}
