using DocumentProcessing.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentProcessing.Application.DTOs
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? ExtractedText { get; set; }
        public DocumentCategory Category { get; set; }
        public ProcessingStatus Status { get; set; }
        public double? ConfidenceScore { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CategoryName => Category.ToString();
        public string StatusName => Status.ToString();
    }
}
