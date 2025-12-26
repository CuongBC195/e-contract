namespace Shared.Constants;

public static class AppConstants
{
    public const string JwtCookieName = "jwt_token";
    public const int JwtExpirationMinutes = 1440; // 24 hours
    
    // Rate limiting
    public const int LoginMaxAttempts = 5;
    public const int LoginWindowMinutes = 15;
    public const int SigningMaxAttempts = 3;
    public const int SigningWindowMinutes = 1;
    
    // Document ID formats
    public const string ReceiptPrefix = "REC-";
    public const string ContractPrefix = "3DO-";
    public const string PdfPrefix = "PDF-";
    
    // Email
    public const string FromEmail = "3docorp@gmail.com";
    public const string FromName = "E-Contract - Hệ thống Biên lai điện tử";
}

