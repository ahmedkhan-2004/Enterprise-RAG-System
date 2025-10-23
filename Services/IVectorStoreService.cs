using SemanticKernelDocumentQA.Models;

namespace SemanticKernelDocumentQA.Services
{
    public interface IVectorStoreService
    {
        Task<bool> IndexDocumentAsync(string documentId, string content, string filename);
        Task<List<string>> SearchSimilarChunksAsync(string query, int maxResults = 5);
        Task<bool> DeleteDocumentAsync(string documentId);
        Task<List<DocumentInfo>> GetAllDocumentsAsync();
    }

    public class DocumentInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}