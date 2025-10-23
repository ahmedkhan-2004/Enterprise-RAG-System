using SemanticKernelDocumentQA.Models;
using SemanticKernelDocumentQA.Services;

namespace SemanticKernelDocumentQA.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IVectorStoreService _vectorStoreService;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(IVectorStoreService vectorStoreService, ILogger<DocumentService> logger)
        {
            _vectorStoreService = vectorStoreService;
            _logger = logger;
        }

        // This method matches IDocumentService.UploadDocumentAsync(IFormFile, string)
        public async Task<UploadDocumentResponse> UploadDocumentAsync(IFormFile file, string userId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return UploadDocumentResponse.CreateError("No file provided");
                }

                // Extract text from file
                string content;
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    content = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return UploadDocumentResponse.CreateError("File appears to be empty or unreadable");
                }

                // Generate document ID
                var documentId = Guid.NewGuid().ToString();

                // Index document in Elasticsearch
                var success = await _vectorStoreService.IndexDocumentAsync(documentId, content, file.FileName);

                if (!success)
                {
                    return UploadDocumentResponse.CreateError("Failed to index document");
                }

                return UploadDocumentResponse.CreateSuccess(documentId, "Document processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {FileName}", file?.FileName);
                return UploadDocumentResponse.CreateError($"Error processing document: {ex.Message}");
            }
        }

        // This method matches IDocumentService.UploadDocumentAsync(string filePath)
        public async Task<UploadDocumentResponse> UploadDocumentAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return UploadDocumentResponse.CreateError("No file path provided");
                }

                if (!File.Exists(filePath))
                {
                    return UploadDocumentResponse.CreateError("File does not exist");
                }

                // Read content from file
                string content = await File.ReadAllTextAsync(filePath);

                if (string.IsNullOrWhiteSpace(content))
                {
                    return UploadDocumentResponse.CreateError("File appears to be empty or unreadable");
                }

                // Generate document ID
                var documentId = Guid.NewGuid().ToString();
                var filename = Path.GetFileName(filePath);

                // Index document in Elasticsearch
                var success = await _vectorStoreService.IndexDocumentAsync(documentId, content, filename);

                if (!success)
                {
                    return UploadDocumentResponse.CreateError("Failed to index document");
                }

                return UploadDocumentResponse.CreateSuccess(documentId, "Document processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document from path {FilePath}", filePath);
                return UploadDocumentResponse.CreateError($"Error processing document: {ex.Message}");
            }
        }

        public async Task<List<DocumentInfo>> GetAllDocumentsAsync()
        {
            return await _vectorStoreService.GetAllDocumentsAsync();
        }

        public async Task<bool> DeleteDocumentAsync(string documentId)
        {
            return await _vectorStoreService.DeleteDocumentAsync(documentId);
        }
    }
}