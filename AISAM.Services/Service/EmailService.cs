using AISAM.Common.Config;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;
using AISAM.Common.Models;

namespace AISAM.Services.Service
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly string _frontendBaseUrl;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            IOptions<FrontendSettings> frontendSettings,
            ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _frontendBaseUrl = frontendSettings.Value.BaseUrl;
        }

        public async Task SendEmailVerificationAsync(string email, string userName, string verificationToken)
        {
            var verificationLink = $"{_frontendBaseUrl}/auth/verify-email?token={verificationToken}";
            
            var subject = "Xác thực email của bạn - AISAM";
            var htmlBody = GetEmailVerificationTemplate(userName, verificationLink);
            var plainTextBody = $"Xin chào {userName},\n\nVui lòng xác thực email của bạn bằng cách truy cập: {verificationLink}\n\nLink có hiệu lực trong 24 giờ.";

            await SendEmailAsync(email, subject, htmlBody, plainTextBody);
            _logger.LogInformation("Email verification sent to {Email}", email);
        }

        public async Task SendPasswordResetAsync(string email, string userName, string resetToken)
        {
            var resetLink = $"{_frontendBaseUrl}/auth/update-password?token={resetToken}";
            
            var subject = "Đặt lại mật khẩu - AISAM";
            var htmlBody = GetPasswordResetTemplate(userName, resetLink);
            var plainTextBody = $"Xin chào {userName},\n\nBạn đã yêu cầu đặt lại mật khẩu. Vui lòng truy cập: {resetLink}\n\nLink có hiệu lực trong 1 giờ.\n\nNếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.";

            await SendEmailAsync(email, subject, htmlBody, plainTextBody);
            _logger.LogInformation("Password reset email sent to {Email}", email);
        }

        public async Task SendWelcomeEmailAsync(string email, string userName)
        {
            var subject = "Chào mừng đến với AISAM!";
            var htmlBody = GetWelcomeEmailTemplate(userName);
            var plainTextBody = $"Xin chào {userName},\n\nChào mừng bạn đến với AISAM - nền tảng quản lý mạng xã hội toàn diện!\n\nChúng tôi rất vui khi có bạn tham gia.";

            await SendEmailAsync(email, subject, htmlBody, plainTextBody);
            _logger.LogInformation("Welcome email sent to {Email}", email);
        }

        public async Task SendTeamInvitationAsync(string email, string teamName, string inviterName, string invitationLink)
        {
            var subject = $"Lời mời tham gia team {teamName}";
            var htmlBody = GetTeamInvitationTemplate(teamName, inviterName, invitationLink);
            var plainTextBody = $"{inviterName} đã mời bạn tham gia team '{teamName}' trên AISAM.\n\nVui lòng truy cập: {invitationLink}";

            await SendEmailAsync(email, subject, htmlBody, plainTextBody);
            _logger.LogInformation("Team invitation sent to {Email} for team {TeamName}", email, teamName);
        }

        public async Task SendNotificationEmailAsync(string email, string subject, string message)
        {
            var htmlBody = GetNotificationTemplate(message);
            var plainTextBody = message;

            await SendEmailAsync(email, subject, htmlBody, plainTextBody);
            _logger.LogInformation("Notification email sent to {Email}", email);
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null)
        {
            try
            {
                if (string.IsNullOrEmpty(_emailSettings.SmtpHost) || string.IsNullOrEmpty(_emailSettings.SmtpUsername))
                {
                    _logger.LogWarning("Email settings not configured. Email not sent to {Email}", toEmail);
                    return false;
                }

                _logger.LogInformation("Sending email to {Email} with subject '{Subject}' via {SmtpHost}:{Port}", 
                    toEmail, subject, _emailSettings.SmtpHost, _emailSettings.SmtpPort);

                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = _emailSettings.EnableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000 // 30 seconds timeout
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8,
                    Priority = MailPriority.Normal
                };

                mailMessage.To.Add(toEmail);

                // Add plain text alternative if provided
                if (!string.IsNullOrEmpty(plainTextBody))
                {
                    var plainView = AlternateView.CreateAlternateViewFromString(plainTextBody, Encoding.UTF8, "text/plain");
                    mailMessage.AlternateViews.Add(plainView);
                }

                _logger.LogInformation("Attempting SMTP connection to {Host}:{Port} with SSL={EnableSsl}", 
                    _emailSettings.SmtpHost, _emailSettings.SmtpPort, _emailSettings.EnableSsl);

                await smtpClient.SendMailAsync(mailMessage);
                
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                return true;
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error sending email to {Email}: StatusCode={StatusCode}, Message={Message}", 
                    toEmail, ex.StatusCode, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}: {Message}", toEmail, ex.Message);
                return false;
            }
        }

        #region Email Templates

        private string GetEmailVerificationTemplate(string userName, string verificationLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>AISAM</h1>
            <p>Xác thực Email</p>
        </div>
        <div class=""content"">
            <h2>Xin chào {userName}!</h2>
            <p>Cảm ơn bạn đã đăng ký tài khoản AISAM. Vui lòng xác thực địa chỉ email của bạn bằng cách nhấp vào nút bên dưới:</p>
            <div style=""text-align: center;"">
                <a href=""{verificationLink}"" class=""button"">Xác thực Email</a>
            </div>
            <p>Hoặc sao chép và dán link sau vào trình duyệt:</p>
            <p style=""word-break: break-all; background: #fff; padding: 10px; border-radius: 5px;"">{verificationLink}</p>
            <p><strong>Lưu ý:</strong> Link này sẽ hết hạn sau 24 giờ.</p>
        </div>
        <div class=""footer"">
            <p>Email này được gửi tự động. Vui lòng không trả lời email này.</p>
            <p>&copy; 2026 AISAM. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetPasswordResetTemplate(string userName, string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #f5576c; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>AISAM</h1>
            <p>Đặt lại mật khẩu</p>
        </div>
        <div class=""content"">
            <h2>Xin chào {userName}!</h2>
            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
            <div style=""text-align: center;"">
                <a href=""{resetLink}"" class=""button"">Đặt lại mật khẩu</a>
            </div>
            <p>Hoặc sao chép và dán link sau vào trình duyệt:</p>
            <p style=""word-break: break-all; background: #fff; padding: 10px; border-radius: 5px;"">{resetLink}</p>
            <div class=""warning"">
                <strong>⚠️ Lưu ý bảo mật:</strong>
                <ul>
                    <li>Link này chỉ có hiệu lực trong 1 giờ</li>
                    <li>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này</li>
                    <li>Không chia sẻ link này với bất kỳ ai</li>
                </ul>
            </div>
        </div>
        <div class=""footer"">
            <p>Email này được gửi tự động. Vui lòng không trả lời email này.</p>
            <p>&copy; 2026 AISAM. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetWelcomeEmailTemplate(string userName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .feature {{ background: white; padding: 15px; margin: 10px 0; border-radius: 5px; border-left: 4px solid #667eea; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎉 Chào mừng đến với AISAM!</h1>
        </div>
        <div class=""content"">
            <h2>Xin chào {userName}!</h2>
            <p>Chúng tôi rất vui mừng chào đón bạn đến với <strong>AISAM</strong> - nền tảng quản lý mạng xã hội toàn diện được hỗ trợ bởi AI.</p>
            
            <h3>Bạn có thể làm gì với AISAM?</h3>
            <div class=""feature"">
                <strong>📱 Quản lý đa nền tảng</strong>
                <p>Kết nối và quản lý Facebook, Instagram, TikTok, Twitter từ một nơi duy nhất.</p>
            </div>
            <div class=""feature"">
                <strong>🤖 Tạo nội dung với AI</strong>
                <p>Sử dụng Gemini AI để tạo nội dung sáng tạo và hấp dẫn chỉ trong vài giây.</p>
            </div>
            <div class=""feature"">
                <strong>📊 Phân tích & Báo cáo</strong>
                <p>Theo dõi hiệu suất và phân tích insights từ tất cả các nền tảng.</p>
            </div>
            <div class=""feature"">
                <strong>👥 Làm việc nhóm</strong>
                <p>Mời thành viên, phân quyền và cộng tác hiệu quả.</p>
            </div>

            <div style=""text-align: center;"">
                <a href=""{_frontendBaseUrl}"" class=""button"">Bắt đầu ngay</a>
            </div>

            <p>Nếu bạn có bất kỳ câu hỏi nào, đừng ngần ngại liên hệ với chúng tôi!</p>
        </div>
        <div class=""footer"">
            <p>Email này được gửi tự động. Vui lòng không trả lời email này.</p>
            <p>&copy; 2026 AISAM. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetTeamInvitationTemplate(string teamName, string inviterName, string invitationLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .invitation-box {{ background: white; padding: 20px; margin: 20px 0; border-radius: 5px; border: 2px solid #4facfe; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #4facfe; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>👥 Lời mời tham gia Team</h1>
        </div>
        <div class=""content"">
            <div class=""invitation-box"">
                <p><strong>{inviterName}</strong> đã mời bạn tham gia team:</p>
                <h2 style=""color: #4facfe; margin: 10px 0;"">{teamName}</h2>
                <p>trên nền tảng AISAM</p>
            </div>
            
            <p>Bằng cách tham gia team, bạn sẽ có thể:</p>
            <ul>
                <li>Cộng tác với các thành viên khác</li>
                <li>Quản lý nội dung và chiến dịch chung</li>
                <li>Truy cập vào các brand và social accounts của team</li>
            </ul>

            <div style=""text-align: center;"">
                <a href=""{invitationLink}"" class=""button"">Chấp nhận lời mời</a>
            </div>
        </div>
        <div class=""footer"">
            <p>Email này được gửi tự động. Vui lòng không trả lời email này.</p>
            <p>&copy; 2026 AISAM. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetNotificationTemplate(string message)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .message {{ background: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>📬 Thông báo từ AISAM</h1>
        </div>
        <div class=""content"">
            <div class=""message"">
                {message}
            </div>
        </div>
        <div class=""footer"">
            <p>Email này được gửi tự động. Vui lòng không trả lời email này.</p>
            <p>&copy; 2026 AISAM. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        #endregion
    }
}
