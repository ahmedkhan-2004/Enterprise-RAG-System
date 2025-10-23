namespace SemanticKernelDocumentQA.Models
{
    public class ChatResponse
    {
        public string Response { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> Sources { get; set; } = new List<string>();

        public static ChatResponse CreateSuccess(string response, List<string>? sources = null)
        {
            return new ChatResponse
            {
                Response = response,
                Success = true,
                Sources = sources ?? new List<string>()
            };
        }

        public static ChatResponse CreateError(string errorMessage)
        {
            return new ChatResponse
            {
                Response = string.Empty,
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
