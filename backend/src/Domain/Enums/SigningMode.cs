namespace Domain.Enums;

public enum SigningMode
{
    Public,        // Chỉ cần click link là ký được (không cần đăng nhập)
    RequiredLogin  // Cần đăng nhập mới ký được
}

