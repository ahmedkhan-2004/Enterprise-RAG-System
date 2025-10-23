using SemanticKernelDocumentQA.Models;

namespace SemanticKernelDocumentQA.Services
{
    public interface ISemanticKernelService
    {
        Task<ChatResponse> AskQuestionAsync(string userId, string question);
        Task<ChatResponse> SummarizeDocumentAsync(string documentId);
        Task<ChatResponse> ExtractKeyPointsAsync(string documentId);
        void ClearConversation(string userId);
        void ClearAllConversations();
    }
}