using DocumentProcessing.Application.Common;
using DocumentProcessing.Application.DTOs;
using DocumentProcessing.Application.DTOs.Auth;
using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Domain.Entities;
using DocumentProcessing.Domain.Enums;
using DocumentProcessing.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DocumentProcessing.Application.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IRepository<Document> _documentRepository;
        private readonly IRepository<UserSession> _userRepository;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(
           IRepository<Document> documentRepository,
           IRepository<UserSession> userRepository,
           ILogger<DocumentService> logger)
        {
            _documentRepository = documentRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Result<DocumentDto>> UploadDocumentAsync(UploadDocumentDto uploadDto, Guid userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", userId);
                    return Result<DocumentDto>.Failure($"User with ID {userId} does not exist.");
                }

                var document = new Document
                {
                    FileName = uploadDto.File.FileName,
                    ContentType = uploadDto.File.ContentType,
                    FileSize = uploadDto.File.Length,
                    FilePath = $"documents/{userId}/{Guid.NewGuid()}-{uploadDto.File.FileName}",
                    Status = ProcessingStatus.Uploaded,
                    Category = DocumentCategory.Other,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var savedDocument = await _documentRepository.AddAsync(document);

                _logger.LogInformation("Document uploaded successfully: {DocumentId}", savedDocument.Id);

                return Result<DocumentDto>.Success(MapToDto(savedDocument));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for user {UserId}", userId);
                return Result<DocumentDto>.Failure("Error uploading document for user {UserId}");
                throw;
            }
        }

        public async Task<Result<IEnumerable<DocumentDto>>> GetUserDocumentsAsync(Guid userId)
        {
            var documents = await _documentRepository.FindAsync(d => d.UserId == userId && !d.IsDeleted);
            return Result<IEnumerable<DocumentDto>>.Success(documents.Select(MapToDto));
        }

        public async Task<Result<DocumentDto?>> GetDocumentByIdAsync(Guid id, Guid userId)
        {
            var document = await _documentRepository.GetByIdAsync(id);
            if (document == null || document.UserId != userId || document.IsDeleted)
            {
                return Result<DocumentDto?>.Success(null);
            }
            return Result<DocumentDto?>.Success(MapToDto(document));
        }

        public async Task ProcessDocumentAsync(Guid documentId)
        {
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null) return;

            try
            {
                document.Status = ProcessingStatus.Processing;
                document.UpdatedAt = DateTime.UtcNow;
                await _documentRepository.UpdateAsync(document);

                await Task.Delay(2000); // Simulate processing delay

                document.ExtractedText = "Sample extracted text from document";
                document.Category = DocumentCategory.Report;
                document.ConfidenceScore = 0.95;
                document.Status = ProcessingStatus.Completed;
                document.UpdatedAt = DateTime.UtcNow;

                await _documentRepository.UpdateAsync(document);

                _logger.LogInformation("Document processed successfully: {DocumentId}", documentId);

            }
            catch (Exception ex)
            {
                document.Status = ProcessingStatus.Failed;
                document.UpdatedAt = DateTime.UtcNow;
                await _documentRepository.UpdateAsync(document);

                _logger.LogError(ex, "Error processing document {DocumentId}", documentId);
            }
        }

        public async Task<Result<IEnumerable<DocumentDto>>> SearchDocumentsAsync(string query, Guid userId)
        {
            var documents = await _documentRepository.FindAsync(d =>
                d.UserId == userId && !d.IsDeleted &&
                (d.FileName.Contains(query) || (d.ExtractedText != null && d.ExtractedText.Contains(query))));

            return Result<IEnumerable<DocumentDto>>.Success(documents.Select(MapToDto));
        }

        public async Task DeleteDocumentAsync(Guid id, Guid userId)
        {
            var document = await _documentRepository.GetByIdAsync(id);
            if (document != null && document.UserId == userId)
            {
                document.IsDeleted = true;
                document.UpdatedAt = DateTime.UtcNow;
                await _documentRepository.UpdateAsync(document);
                _logger.LogInformation("Document deleted successfully: {DocumentId}", id);
            }
        }

        private static DocumentDto MapToDto(Document document)
        {
            return new DocumentDto
            {
                Id = document.Id,
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileSize = document.FileSize,
                ExtractedText = document.ExtractedText,
                Category = document.Category,
                Status = document.Status,
                ConfidenceScore = document.ConfidenceScore,
                CreatedAt = document.CreatedAt
            };
        }
    }
}
