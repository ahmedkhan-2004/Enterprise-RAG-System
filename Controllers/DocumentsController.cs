using Microsoft.AspNetCore.Mvc;
using SemanticKernelDocumentQA.Models;
using SemanticKernelDocumentQA.Services;

namespace SemanticKernelDocumentQA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        /// <summary>
        /// Upload a document for indexing
        /// </summary>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(UploadDocumentResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<UploadDocumentResponse>> UploadDocument(
            [FromForm] IFormFile file,
            [FromForm] string? userId = null)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(UploadDocumentResponse.CreateError("No file uploaded."));
            }

            var user = userId ?? "default-user";
            var response = await _documentService.UploadDocumentAsync(file, user);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Get all indexed documents
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<DocumentInfo>), 200)]
        public async Task<ActionResult<List<DocumentInfo>>> GetAllDocuments()
        {
            try
            {
                var documents = await _documentService.GetAllDocumentsAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents");
                return StatusCode(500, new { Error = "Failed to retrieve documents" });
            }
        }

        /// <summary>
        /// Delete a document by ID
        /// </summary>
        [HttpDelete("{documentId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteDocument(string documentId)
        {
            try
            {
                var success = await _documentService.DeleteDocumentAsync(documentId);
                
                if (!success)
                {
                    return NotFound(new { Error = "Document not found or could not be deleted" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                return StatusCode(500, new { Error = "Failed to delete document" });
            }
        }
    }
}