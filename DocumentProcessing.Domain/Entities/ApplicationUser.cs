using DocumentProcessing.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace DocumentProcessing.Domain.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Navigation properties
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();

        // Full name property
        public string FullName => $"{FirstName} {LastName}";
    }
}
