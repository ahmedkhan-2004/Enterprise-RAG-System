using SemanticKernelDocumentQA.Models;

namespace SemanticKernelDocumentQA.Services
{
    public interface IDocumentService
    {
        Task<UploadDocumentResponse> UploadDocumentAsync(IFormFile file, string userId);
        Task<UploadDocumentResponse> UploadDocumentAsync(string filePath);
        Task<List<DocumentInfo>> GetAllDocumentsAsync();
        Task<bool> DeleteDocumentAsync(string documentId);
    }
}