using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Shared.Constants;

namespace API.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendInvitationEmailAsync(string toEmail, string toName, string documentId, string documentTitle, string signingUrl)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var smtpUsername = _configuration["Email:SmtpUsername"] ?? throw new InvalidOperationException("Email:SmtpUsername not configured");
        var smtpPassword = _configuration["Email:SmtpPassword"] ?? throw new InvalidOperationException("Email:SmtpPassword not configured");
        var fromEmail = _configuration["Email:FromEmail"] ?? AppConstants.FromEmail;
        var fromName = _configuration["Email:FromName"] ?? AppConstants.FromName;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"Yêu cầu ký tài liệu: {documentTitle}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = GetInvitationEmailHtml(toName, documentTitle, signingUrl),
            TextBody = GetInvitationEmailText(toName, documentTitle, signingUrl)
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Invitation email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invitation email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendSigningConfirmationEmailAsync(string toEmail, string toName, string documentId, string documentTitle, byte[]? pdfBytes = null)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var smtpUsername = _configuration["Email:SmtpUsername"] ?? throw new InvalidOperationException("Email:SmtpUsername not configured");
        var smtpPassword = _configuration["Email:SmtpPassword"] ?? throw new InvalidOperationException("Email:SmtpPassword not configured");
        var fromEmail = _configuration["Email:FromEmail"] ?? AppConstants.FromEmail;
        var fromName = _configuration["Email:FromName"] ?? AppConstants.FromName;
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
        var viewUrl = $"{frontendBaseUrl}/?id={documentId}";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"Tài liệu đã được ký: {documentTitle}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = GetConfirmationEmailHtml(toName, documentTitle, documentId, viewUrl),
            TextBody = GetConfirmationEmailText(toName, documentTitle, documentId, viewUrl)
        };

        // Note: We don't attach PDF to email. Instead, we provide a link to view the document online,
        // where users can download the PDF directly from the web view (ensures PDF matches web version exactly)

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Confirmation email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending confirmation email to {Email}", toEmail);
            throw;
        }
    }

    private string GetInvitationEmailHtml(string name, string documentTitle, string signingUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Yêu cầu ký tài liệu</title>
</head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background: linear-gradient(135deg, #f5f7fa 0%, #e4e8ec 100%); padding: 20px; margin: 0;"">
    <div style=""max-width: 600px; margin: 0 auto; background: rgba(255, 255, 255, 0.85); backdrop-filter: blur(12px); border: 1px solid rgba(0, 0, 0, 0.08); border-radius: 24px; padding: 40px; box-shadow: 0 8px 32px rgba(0, 0, 0, 0.08);"">
        <!-- Header -->
        <div style=""text-align: center; margin-bottom: 32px;"">
            <h1 style=""color: #1a1a1a; font-size: 24px; font-weight: bold; margin: 0; margin-bottom: 8px;"">Yêu cầu ký tài liệu</h1>
            <p style=""color: #6b7280; font-size: 14px; margin: 0;"">Bạn đã nhận được yêu cầu ký tài liệu</p>
        </div>
        
        <!-- Content -->
        <div style=""margin-bottom: 32px;"">
            <p style=""color: #6b7280; font-size: 16px; line-height: 1.6; margin-bottom: 16px;"">
                Xin chào <strong style=""color: #1a1a1a;"">{name}</strong>,
            </p>
            <p style=""color: #6b7280; font-size: 16px; line-height: 1.6; margin-bottom: 24px;"">
                Bạn đã nhận được yêu cầu ký tài liệu: <strong style=""color: #1a1a1a;"">{documentTitle}</strong>
            </p>
        </div>
        
        <!-- Button -->
        <div style=""text-align: center; margin: 40px 0;"">
            <a href=""{signingUrl}"" style=""display: inline-block; background: rgba(0, 0, 0, 0.9); color: white; text-decoration: none; padding: 14px 32px; border-radius: 12px; font-weight: 600; font-size: 16px; transition: all 0.2s ease; box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);"">Ký tài liệu ngay</a>
        </div>
        
        <!-- Footer -->
        <p style=""color: #9ca3af; font-size: 14px; line-height: 1.6; margin-top: 32px; text-align: center;"">
            Nếu bạn không yêu cầu email này, vui lòng bỏ qua.
        </p>
    </div>
</body>
</html>";
    }

    private string GetInvitationEmailText(string name, string documentTitle, string signingUrl)
    {
        return $@"
Yêu cầu ký tài liệu

Xin chào {name},

Bạn đã nhận được yêu cầu ký tài liệu: {documentTitle}

Vui lòng truy cập link sau để ký: {signingUrl}

Nếu bạn không yêu cầu email này, vui lòng bỏ qua.
";
    }

    private string GetConfirmationEmailHtml(string name, string documentTitle, string documentId, string viewUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Tài liệu đã được ký</title>
</head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background: linear-gradient(135deg, #f5f7fa 0%, #e4e8ec 100%); padding: 20px; margin: 0;"">
    <div style=""max-width: 600px; margin: 0 auto; background: rgba(255, 255, 255, 0.85); backdrop-filter: blur(12px); border: 1px solid rgba(0, 0, 0, 0.08); border-radius: 24px; padding: 40px; box-shadow: 0 8px 32px rgba(0, 0, 0, 0.08);"">
        <!-- Header -->
        <div style=""text-align: center; margin-bottom: 32px;"">
            <h1 style=""color: #1a1a1a; font-size: 24px; font-weight: bold; margin: 0; margin-bottom: 8px;"">Tài liệu đã được ký thành công</h1>
            <p style=""color: #6b7280; font-size: 14px; margin: 0;"">Xác nhận hoàn thành</p>
        </div>
        
        <!-- Content -->
        <div style=""margin-bottom: 32px;"">
            <p style=""color: #6b7280; font-size: 16px; line-height: 1.6; margin-bottom: 16px;"">
                Xin chào <strong style=""color: #1a1a1a;"">{name}</strong>,
            </p>
            <p style=""color: #6b7280; font-size: 16px; line-height: 1.6; margin-bottom: 16px;"">
                Tài liệu <strong style=""color: #1a1a1a;"">{documentTitle}</strong> đã được ký đầy đủ bởi tất cả các bên.
            </p>
            <p style=""color: #6b7280; font-size: 16px; line-height: 1.6; margin-bottom: 24px;"">
                Vui lòng truy cập link bên dưới để xem và tải xuống file PDF chính thức.
            </p>
        </div>
        
        <!-- Button -->
        <div style=""text-align: center; margin: 40px 0;"">
            <a href=""{viewUrl}"" style=""display: inline-block; background: rgba(0, 0, 0, 0.9); color: white; text-decoration: none; padding: 14px 32px; border-radius: 12px; font-weight: 600; font-size: 16px; transition: all 0.2s ease; box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);"">Xem và tải PDF</a>
        </div>
        
        <div style=""margin-top: 24px; padding: 16px; background: rgba(0, 0, 0, 0.03); border-radius: 12px; border-left: 4px solid rgba(0, 0, 0, 0.1);"">
            <p style=""color: #6b7280; font-size: 14px; line-height: 1.6; margin: 0 0 12px 0;"">
                <strong style=""color: #1a1a1a;"">Lưu ý:</strong>
            </p>
            <ul style=""color: #6b7280; font-size: 14px; line-height: 1.8; margin: 0; padding-left: 20px;"">
                <li>Vui lòng lưu trữ file PDF này ở nơi an toàn để tham khảo sau này</li>
                <li>Link xem hợp đồng có thể được truy cập bất cứ lúc nào bằng cách click vào nút phía trên</li>
                <li>Nếu bạn có bất kỳ câu hỏi nào về tài liệu, vui lòng liên hệ với người tạo tài liệu</li>
            </ul>
        </div>
        
        <!-- Footer -->
        <p style=""color: #9ca3af; font-size: 14px; line-height: 1.6; margin-top: 32px; text-align: center;"">
            Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi.
        </p>
    </div>
</body>
</html>";
    }

    private string GetConfirmationEmailText(string name, string documentTitle, string documentId, string viewUrl)
    {
        return $@"
Tài liệu đã được ký thành công

Xin chào {name},

Tài liệu {documentTitle} đã được ký đầy đủ bởi tất cả các bên.

Vui lòng truy cập link sau để xem và tải xuống file PDF chính thức:
{viewUrl}

Lưu ý:
- Vui lòng lưu trữ file PDF này ở nơi an toàn để tham khảo sau này
- Link xem hợp đồng có thể được truy cập bất cứ lúc nào
- Nếu bạn có bất kỳ câu hỏi nào về tài liệu, vui lòng liên hệ với người tạo tài liệu

Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi.
";
    }

    public async Task SendOtpEmailAsync(string toEmail, string toName, string otpCode)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var smtpUsername = _configuration["Email:SmtpUsername"] ?? throw new InvalidOperationException("Email:SmtpUsername not configured");
        var smtpPassword = _configuration["Email:SmtpPassword"] ?? throw new InvalidOperationException("Email:SmtpPassword not configured");
        var fromEmail = _configuration["Email:FromEmail"] ?? AppConstants.FromEmail;
        var fromName = _configuration["Email:FromName"] ?? AppConstants.FromName;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "Mã OTP xác thực email";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = GetOtpEmailHtml(toName, otpCode),
            TextBody = GetOtpEmailText(toName, otpCode)
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("OTP email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendEmailVerificationEmailAsync(string toEmail, string toName, string verificationToken, string verificationUrl)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var smtpUsername = _configuration["Email:SmtpUsername"] ?? throw new InvalidOperationException("Email:SmtpUsername not configured");
        var smtpPassword = _configuration["Email:SmtpPassword"] ?? throw new InvalidOperationException("Email:SmtpPassword not configured");
        var fromEmail = _configuration["Email:FromEmail"] ?? AppConstants.FromEmail;
        var fromName = _configuration["Email:FromName"] ?? AppConstants.FromName;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "Xác thực email của bạn";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = GetEmailVerificationHtml(toName, verificationUrl),
            TextBody = GetEmailVerificationText(toName, verificationUrl)
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Verification email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken, string resetUrl)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var smtpUsername = _configuration["Email:SmtpUsername"] ?? throw new InvalidOperationException("Email:SmtpUsername not configured");
        var smtpPassword = _configuration["Email:SmtpPassword"] ?? throw new InvalidOperationException("Email:SmtpPassword not configured");
        var fromEmail = _configuration["Email:FromEmail"] ?? AppConstants.FromEmail;
        var fromName = _configuration["Email:FromName"] ?? AppConstants.FromName;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "Đặt lại mật khẩu";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = GetPasswordResetHtml(toName, resetUrl),
            TextBody = GetPasswordResetText(toName, resetUrl)
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Password reset email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to {Email}", toEmail);
            throw;
        }
    }

    private string GetOtpEmailHtml(string name, string otpCode)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Mã OTP xác thực email</title>
</head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background: linear-gradient(135deg, #f5f7fa 0%, #e4e8ec 100%); padding: 20px; margin: 0;"">
    <div style=""max-width: 600px; margin: 0 auto; background: rgba(255, 255, 255, 0.85); backdrop-filter: blur(12px); border: 1px solid rgba(0, 0, 0, 0.08); border-radius: 24px; padding: 40px; box-shadow: 0 8px 32px rgba(0, 0, 0, 0.08);"">
        <!-- Header -->
        <div style=""text-align: center; margin-bottom: 32px;"">
            <h1 style=""color: #1a1a1a; font-size: 24px; font-weight: bold; margin: 0; margin-bottom: 8px;"">Mã OTP xác thực email</h1>
            <p style=""color: #6b7280; font-size: 14px; margin: 0;"">Nhập mã OTP để xác thực tài khoản của bạn</p>
        </div>
        
        <!-- Content -->
        <div style=""margin-bottom: 32px;"">
            <p style=""color: #6b7280; font-size: 16px; line-height: 1.6; margin-bottom: 16px;"">
                Xin chào <strong style=""color: #1a1a1a;"">{name}</strong>,
            </p>
            <p style=""color: #6b7280; font-size: 16px; line-height: 1.6; margin-bottom: 24px;"">
                Cảm ơn bạn đã đăng ký tài khoản. Mã OTP của bạn là:
            </p>
            <div style=""text-align: center; margin: 32px 0;"">
                <div style=""display: inline-block; background: rgba(0, 0, 0, 0.9); color: white; padding: 16px 32px; border-radius: 12px; font-weight: bold; font-size: 32px; letter-spacing: 8px; font-family: 'Courier New', monospace;"">{otpCode}</div>
            </div>
            <p style=""color: #6b7280; font-size: 14px; line-height: 1.6; margin-top: 24px; text-align: center;"">
                Mã OTP này sẽ hết hạn sau 10 phút.
            </p>
        </div>
        
        <!-- Footer -->
        <p style=""color: #9ca3af; font-size: 14px; line-height: 1.6; margin-top: 32px; text-align: center;"">
            Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.
        </p>
    </div>
</body>
</html>";
    }

    private string GetOtpEmailText(string name, string otpCode)
    {
        return $@"
Mã OTP xác thực email

Xin chào {name},

Cảm ơn bạn đã đăng ký tài khoản. Mã OTP của bạn là:

{otpCode}

Mã OTP này sẽ hết hạn sau 10 phút.

Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.
";
    }

    private string GetEmailVerificationHtml(string name, string verificationUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Xác thực email</title>
</head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background: linear-gradient(135deg, #f5f7fa 0%, #e4e8ec 100%); padding: 20px; margin: 0;"">
    <div style=""max-width: 600px; margin: 0 auto; background: rgba(255, 255, 255, 0.85); backdrop-filter: blur(12px); border: 1px solid rgba(0, 0, 0, 0.08); border-radius: 24px; padding: 40px; box-shadow: 0 8px 32px rgba(0, 0, 0, 0.08);"">
        <!-- Header -->
        <div style=""text-align: center; margin-bottom: 32px;"">
            <h1 style=""color: #1a1a1a; font-size: 24px; font-weight: bold; margin: 0; margin-bottom: 8px;"">Xác thực email</h1>
            <p style=""color: #6b7280; font-size: 14px; margin: 0;"">Xác nhận địa chỉ email của bạn</p>
        </div>
        
        <!-- Content -->
        <div style=""margin-bottom: 32px;"">
            <p style=""color: #6b7280; font-size: 16px; line-height: 1.6; margin-bottom: 16px;"">
                Xin chào <strong style=""color: #1a1a1a;"">{name}</strong>,
            </p>
            <p style=""color: #6b7280; font-size: 16px; line-height: 1.6; margin-bottom: 24px;"">
                Cảm ơn bạn đã đăng ký tài khoản. Vui lòng xác thực địa chỉ email của bạn bằng cách nhấp vào nút bên dưới.
            </p>
        </div>
        
        <!-- Button -->
        <div style=""text-align: center; margin: 40px 0;"">
            <a href=""{verificationUrl}"" style=""display: inline-block; background: rgba(0, 0, 0, 0.9); color: white; text-decoration: none; padding: 14px 32px; border-radius: 12px; font-weight: 600; font-size: 16px; transition: all 0.2s ease; box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);"">Xác thực email</a>
        </div>
        
        <!-- Footer -->
        <p style=""color: #9ca3af; font-size: 14px; line-height: 1.6; margin-top: 32px; text-align: center;"">
            Link này sẽ hết hạn sau 7 ngày. Nếu bạn không yêu cầu email này, vui lòng bỏ qua.
        </p>
    </div>
</body>
</html>";
    }

    private string GetEmailVerificationText(string name, string verificationUrl)
    {
        return $@"
Xác thực email

Xin chào {name},

Cảm ơn bạn đã đăng ký tài khoản. Vui lòng xác thực địa chỉ email của bạn bằng cách truy cập link sau:

{verificationUrl}

Link này sẽ hết hạn sau 7 ngày. Nếu bạn không yêu cầu email này, vui lòng bỏ qua.
";
    }

    private string GetPasswordResetHtml(string name, string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Đặt lại mật khẩu</title>
</head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background: linear-gradient(135deg, #f5f7fa 0%, #e4e8ec 100%); padding: 20px; margin: 0;"">
    <div style=""max-width: 600px; margin: 0 auto; background: rgba(255, 255, 255, 0.85); backdrop-filter: blur(12px); border: 1px solid rgba(0, 0, 0, 0.08); border-radius: 24px; padding: 40px; box-shadow: 0 8px 32px rgba(0, 0, 0, 0.08);"">
        <!-- Header -->
        <div style=""text-align: center; margin-bottom: 32px;"">
            <h1 style=""color: #1a1a1a; font-size: 24px; font-weight: bold; margin: 0; margin-bottom: 8px;"">Đặt lại mật khẩu</h1>
            <p style=""color: #6b7280; font-size: 14px; margin: 0;"">Yêu cầu thay đổi mật khẩu</p>
        </div>
        
        <!-- Content -->
        <div style=""margin-bottom: 32px;"">
            <p style=""color: #6b7280; font-size: 16px; line-height: 1.6; margin-bottom: 16px;"">
                Xin chào <strong style=""color: #1a1a1a;"">{name}</strong>,
            </p>
            <p style=""color: #6b7280; font-size: 16px; line-height: 1.6; margin-bottom: 24px;"">
                Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Nhấp vào nút bên dưới để tạo mật khẩu mới.
            </p>
        </div>
        
        <!-- Button -->
        <div style=""text-align: center; margin: 40px 0;"">
            <a href=""{resetUrl}"" style=""display: inline-block; background: rgba(0, 0, 0, 0.9); color: white; text-decoration: none; padding: 14px 32px; border-radius: 12px; font-weight: 600; font-size: 16px; transition: all 0.2s ease; box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);"">Đặt lại mật khẩu</a>
        </div>
        
        <!-- Footer -->
        <p style=""color: #9ca3af; font-size: 14px; line-height: 1.6; margin-top: 32px; text-align: center;"">
            Link này sẽ hết hạn sau 1 giờ. Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.
        </p>
    </div>
</body>
</html>";
    }

    private string GetPasswordResetText(string name, string resetUrl)
    {
        return $@"
Đặt lại mật khẩu

Xin chào {name},

Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Truy cập link sau để tạo mật khẩu mới:

{resetUrl}

Link này sẽ hết hạn sau 1 giờ. Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.
";
    }
}

