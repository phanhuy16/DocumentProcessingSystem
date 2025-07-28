
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DocumentProcessing.Application.DTOs
{
    public class UploadDocumentDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        public string? Description { get; set; }
    }
}
