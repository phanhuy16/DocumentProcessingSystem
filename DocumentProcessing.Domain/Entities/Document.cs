using DocumentProcessing.Domain.Common;
using DocumentProcessing.Domain.Enums;

namespace DocumentProcessing.Domain.Entities
{
    public class Document : BaseEntity
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? ExtractedText { get; set; }
        public DocumentCategory Category { get; set; }
        public ProcessingStatus Status { get; set; }
        public double? ConfidenceScore { get; set; }
        public Guid UserId { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
    }
}
