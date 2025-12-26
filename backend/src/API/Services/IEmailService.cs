namespace API.Services;

public interface IEmailService
{
    Task SendInvitationEmailAsync(string toEmail, string toName, string documentId, string documentTitle, string signingUrl);
    Task SendSigningConfirmationEmailAsync(string toEmail, string toName, string documentId, string documentTitle, byte[]? pdfBytes = null);
    Task SendOtpEmailAsync(string toEmail, string toName, string otpCode);
    Task SendEmailVerificationEmailAsync(string toEmail, string toName, string verificationToken, string verificationUrl);
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken, string resetUrl);
}

