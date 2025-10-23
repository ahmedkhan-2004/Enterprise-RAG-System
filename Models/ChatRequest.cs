namespace SemanticKernelDocumentQA.Models
{
    public class ChatRequest
    {
        public string Question { get; set; } = string.Empty;
        public string? UserId { get; set; }
    }
}