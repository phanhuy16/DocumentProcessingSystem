

using DocumentProcessing.Domain.Common;
using DocumentProcessing.Domain.Enums;

namespace DocumentProcessing.Domain.Entities
{
    public class EmailLog : BaseEntity
    {
        public string To { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public EmailStatus Status { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime? SentAt { get; set; }
        public int RetryCount { get; set; } = 0;
        public Guid? UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
        public string TemplateName { get; set; } = string.Empty;
    }

}
