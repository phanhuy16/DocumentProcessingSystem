using DocumentProcessing.Application.DTOs;
using DocumentProcessing.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocumentProcessing.API.Controllers
{
    [ApiController]
    [Route("api/client/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Authorize(Policy = "Manager")]
        public async Task<ActionResult<DocumentDto>> UploadDocument([FromForm] UploadDocumentDto uploadDto, Guid userId)
        {
            try
            {
                var result = await _documentService.UploadDocumentAsync(uploadDto, userId);

                _ = Task.Run(async () => await _documentService.ProcessDocumentAsync(result.Data.Id));

                return CreatedAtAction(nameof(GetDocument), new { id = result.Data.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        [Authorize(Policy = "User")]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetDocuments(Guid userId)
        {
            try
            {
                var documents = await _documentService.GetUserDocumentsAsync(userId);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user documents");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "User")]
        public async Task<ActionResult<DocumentDto>> GetDocument(Guid id, Guid userId)
        {
            try
            {
                var document = await _documentService.GetDocumentByIdAsync(id, userId);
                if (document == null)
                {
                    return NotFound();
                }
                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("search")]
        [Authorize(Policy = "User")]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> SearchDocuments([FromQuery] string query, Guid userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query cannot be empty");
                }

                var documents = await _documentService.SearchDocumentsAsync(query, userId);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "Manager")]
        public async Task<ActionResult> DeleteDocument(Guid userId, Guid id)
        {
            try
            {
                await _documentService.DeleteDocumentAsync(id, userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
