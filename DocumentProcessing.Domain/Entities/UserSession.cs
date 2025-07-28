using DocumentProcessing.Domain.Common;

namespace DocumentProcessing.Domain.Entities
{
    public class UserSession : BaseEntity
    {
        public Guid UserId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }

        // Navigation property
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
