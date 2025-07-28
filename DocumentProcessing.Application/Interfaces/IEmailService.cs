using DocumentProcessing.Domain.Entities;
using DocumentProcessing.Domain.Models;

namespace DocumentProcessing.Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(EmailRequest emailRequest);
        Task<bool> SendEmailWithTemplateAsync(string templateName, string to, string toName, Dictionary<string, object> variables);
        Task<bool> SendEmailConfirmationAsync(string email, string firstName, string confirmationLink);
        Task<bool> SendPasswordResetAsync(string email, string firstName, string resetLink);
        Task<bool> SendWelcomeEmailAsync(string email, string firstName, string lastName);
        Task<bool> SendPasswordChangedNotificationAsync(string email, string firstName);
        Task<bool> SendAccountLockedNotificationAsync(string email, string firstName, DateTime lockoutEnd);
        Task<List<EmailLog>> GetEmailLogsAsync(Guid? userId = null, int page = 1, int pageSize = 10);
        Task RetryFailedEmailsAsync();
    }
}
