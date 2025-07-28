using DocumentProcessing.Application.Common;
using DocumentProcessing.Application.DTOs;

namespace DocumentProcessing.Application.Interfaces
{
    public interface IDocumentService
    {
        Task<Result<DocumentDto>> UploadDocumentAsync(UploadDocumentDto uploadDto, Guid userId);
        Task<Result<IEnumerable<DocumentDto>>> GetUserDocumentsAsync(Guid userId);
        Task<Result<DocumentDto?>> GetDocumentByIdAsync(Guid id, Guid userId);
        Task ProcessDocumentAsync(Guid documentId);
        Task<Result<IEnumerable<DocumentDto>>> SearchDocumentsAsync(string query, Guid userId);
        Task DeleteDocumentAsync(Guid id, Guid userId);
    }
}
