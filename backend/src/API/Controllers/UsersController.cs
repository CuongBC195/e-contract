using API.DTOs;
using API.Services;
using Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Helpers;
using Shared.Models;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserRepository userRepository,
        IDocumentRepository documentRepository,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _documentRepository = documentRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponseDto<UserResponseDto>>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 4,
        [FromQuery] string? search = null)
    {
        try
        {
            var users = await _userRepository.GetAllAsync(page, pageSize, search);
            var totalCount = await _userRepository.GetTotalCountAsync(search);

            var userDtos = new List<UserResponseDto>();
            foreach (var user in users)
            {
                var documentCount = await _documentRepository.GetTotalCountAsync(user.Id);
                userDtos.Add(new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Role = user.Role.ToString(),
                    EmailVerified = user.EmailVerified,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    DocumentCount = documentCount
                });
            }

            var paginatedResponse = new PaginatedResponseDto<UserResponseDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
                // TotalPages is computed automatically from TotalCount and PageSize
            };

            return Ok(ApiResponse<PaginatedResponseDto<UserResponseDto>>.Success(paginatedResponse, "Lấy danh sách người dùng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, ApiResponse<PaginatedResponseDto<UserResponseDto>>.Error("Lỗi khi lấy danh sách người dùng", new List<string> { ex.Message }));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetUser(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<UserResponseDto>.NotFound("Không tìm thấy người dùng"));

        var documentCount = await _documentRepository.GetTotalCountAsync(user.Id);

        var userDto = new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString(),
            EmailVerified = user.EmailVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            DocumentCount = documentCount
        };

        return Ok(ApiResponse<UserResponseDto>.Success(userDto, "Lấy thông tin người dùng thành công"));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> CreateUser([FromBody] CreateUserRequestDto request)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(ApiResponse<UserResponseDto>.Error("Email, tên và mật khẩu là bắt buộc", new List<string> { "Email, tên và mật khẩu là bắt buộc" }));

        if (request.Password.Length < 6)
            return BadRequest(ApiResponse<UserResponseDto>.Error("Mật khẩu phải có ít nhất 6 ký tự", new List<string> { "Mật khẩu phải có ít nhất 6 ký tự" }));

        // Check if email already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(ApiResponse<UserResponseDto>.Error("Email đã tồn tại", new List<string> { "Email đã tồn tại" }));

        // Parse role
        if (!Enum.TryParse<UserRole>(request.Role, true, out var userRole))
            userRole = UserRole.User;

        // Create user
        var user = new Domain.Entities.User
        {
            Email = request.Email,
            Name = request.Name,
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            Role = userRole,
            EmailVerified = true, // Admin created users are verified by default
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            var createdUser = await _userRepository.CreateAsync(user);
            var documentCount = await _documentRepository.GetTotalCountAsync(createdUser.Id);

            var userDto = new UserResponseDto
            {
                Id = createdUser.Id,
                Email = createdUser.Email,
                Name = createdUser.Name,
                Role = createdUser.Role.ToString(),
                EmailVerified = createdUser.EmailVerified,
                CreatedAt = createdUser.CreatedAt,
                LastLoginAt = createdUser.LastLoginAt,
                DocumentCount = documentCount
            };

            return StatusCode(201, ApiResponse<UserResponseDto>.Created(userDto, "Tạo người dùng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, ApiResponse<UserResponseDto>.Error("Lỗi khi tạo người dùng", new List<string> { ex.Message }));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> UpdateUser(Guid id, [FromBody] UpdateUserRequestDto request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<UserResponseDto>.NotFound("Không tìm thấy người dùng"));

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.Name))
            user.Name = request.Name;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (request.Password.Length < 6)
                return BadRequest(ApiResponse<UserResponseDto>.Error("Mật khẩu phải có ít nhất 6 ký tự", new List<string> { "Mật khẩu phải có ít nhất 6 ký tự" }));
            user.PasswordHash = PasswordHelper.HashPassword(request.Password);
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            if (Enum.TryParse<UserRole>(request.Role, true, out var userRole))
                user.Role = userRole;
        }

        if (request.EmailVerified.HasValue)
            user.EmailVerified = request.EmailVerified.Value;

        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            var updatedUser = await _userRepository.UpdateAsync(user);
            var documentCount = await _documentRepository.GetTotalCountAsync(updatedUser.Id);

            var userDto = new UserResponseDto
            {
                Id = updatedUser.Id,
                Email = updatedUser.Email,
                Name = updatedUser.Name,
                Role = updatedUser.Role.ToString(),
                EmailVerified = updatedUser.EmailVerified,
                CreatedAt = updatedUser.CreatedAt,
                LastLoginAt = updatedUser.LastLoginAt,
                DocumentCount = documentCount
            };

            return Ok(ApiResponse<UserResponseDto>.Success(userDto, "Cập nhật người dùng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            return StatusCode(500, ApiResponse<UserResponseDto>.Error("Lỗi khi cập nhật người dùng", new List<string> { ex.Message }));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteUser(Guid id)
    {
        var success = await _userRepository.DeleteAsync(id);
        if (!success)
            return NotFound(ApiResponse.NotFound("Không tìm thấy người dùng"));

        return NoContent();
    }
}

