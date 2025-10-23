namespace SemanticKernelDocumentQA.Models
{
    public class UploadDocumentResponse
    {
        public bool Success { get; set; } = true;
        public string DocumentId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        
        // For backwards compatibility with error checking
        public static bool IsSuccessful => true; // Static property for backwards compatibility
        public static string ErrorMessage => string.Empty; // Static property for backwards compatibility
        
        public static UploadDocumentResponse CreateSuccess(string documentId, string message = "Upload successful")
        {
            return new UploadDocumentResponse
            {
                Success = true,
                DocumentId = documentId,
                Message = message
            };
        }
        
        public static UploadDocumentResponse CreateError(string errorMessage)
        {
            return new UploadDocumentResponse
            {
                Success = false,
                DocumentId = string.Empty,
                Message = errorMessage
            };
        }
    }
}