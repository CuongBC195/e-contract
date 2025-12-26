namespace API.DTOs;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int DocumentCount { get; set; }
}

public class CreateUserRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}

public class UpdateUserRequestDto
{
    public string? Name { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }
    public bool? EmailVerified { get; set; }
}

public class UpdateProfileRequestDto
{
    public string? Name { get; set; }
}

public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class DeleteAccountRequestDto
{
    public string OtpCode { get; set; } = string.Empty;
}

