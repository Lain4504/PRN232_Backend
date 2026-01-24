namespace AISAM.Services.IServices
{
    public interface IEmailService
    {
        Task SendEmailVerificationAsync(string email, string userName, string verificationToken);
        Task SendPasswordResetAsync(string email, string userName, string resetToken);
        Task SendWelcomeEmailAsync(string email, string userName);
        Task SendTeamInvitationAsync(string email, string teamName, string inviterName, string invitationLink);
        Task SendNotificationEmailAsync(string email, string subject, string message);
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null);
    }
}
