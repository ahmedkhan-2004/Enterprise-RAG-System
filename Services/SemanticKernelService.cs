using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticKernelDocumentQA.Models;

namespace SemanticKernelDocumentQA.Services
{
    public class SemanticKernelService : ISemanticKernelService
    {
        private readonly Kernel _kernel;
        private readonly IVectorStoreService _vectorStoreService;
        private readonly ILogger<SemanticKernelService> _logger;
        private readonly Dictionary<string, ChatHistory> _conversations;

        public SemanticKernelService(
            Kernel kernel,
            IVectorStoreService vectorStoreService,
            ILogger<SemanticKernelService> logger)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _vectorStoreService = vectorStoreService ?? throw new ArgumentNullException(nameof(vectorStoreService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _conversations = new Dictionary<string, ChatHistory>();
        }

        /// <summary>
        /// Handles conversational Q&A with context from vector store.
        /// </summary>
        public async Task<ChatResponse> AskQuestionAsync(string userId, string question)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ChatResponse.CreateError("UserId cannot be empty.");

                if (string.IsNullOrWhiteSpace(question))
                    return ChatResponse.CreateError("Question cannot be empty.");

                // Retrieve relevant context
                var relevantChunks = await _vectorStoreService.SearchSimilarChunksAsync(question);
                var context = string.Join("\n\n", relevantChunks ?? Enumerable.Empty<string>());

                // Initialize conversation with system message if new user
                if (!_conversations.ContainsKey(userId))
                {
                    var newHistory = new ChatHistory();
                    newHistory.AddSystemMessage(
                        "You are a helpful AI assistant specialized in answering questions about documents and general knowledge. " +
                        "When context is provided, use it. If no context is available, still answer based on your knowledge."
                    );
                    _conversations[userId] = newHistory;
                }

                var chatHistory = _conversations[userId];

                // Build prompt with graceful fallback
                var prompt = string.IsNullOrWhiteSpace(context)
                    ? $"Question: {question}\n\nAnswer the question clearly and concisely."
                    : $"Context:\n{context}\n\nQuestion: {question}\n\nIf the context is relevant, answer based on it. " +
                      "If the context is insufficient, also use your own knowledge to provide the best possible answer.";

                _logger.LogInformation("User {UserId} | Prompt sent to model: {Prompt}", userId, prompt);

                chatHistory.AddUserMessage(prompt);

                // Call Semantic Kernel
                var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
                var response = await chatCompletion.GetChatMessageContentAsync(chatHistory);

                var answer = response?.Content?.Trim();
                if (string.IsNullOrWhiteSpace(answer))
                {
                    answer = "I'm sorry, I couldn't generate a response.";
                }

                chatHistory.AddAssistantMessage(answer);

                return ChatResponse.CreateSuccess(answer, relevantChunks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asking question for user {UserId}", userId);
                return ChatResponse.CreateError("An unexpected error occurred. Please try again later.");
            }
        }

        /// <summary>
        /// Summarizes a document from the vector store.
        /// </summary>
        public async Task<ChatResponse> SummarizeDocumentAsync(string documentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(documentId))
                    return ChatResponse.CreateError("DocumentId cannot be empty.");

                var documents = await _vectorStoreService.GetAllDocumentsAsync();
                var document = documents.FirstOrDefault(d => d.Id == documentId);

                if (document == null)
                    return ChatResponse.CreateError("Document not found.");

                var prompt = $"Please provide a comprehensive but concise summary of the following document:\n\n{document.Content}";

                _logger.LogInformation("SummarizeDocument | DocumentId: {DocumentId} | Prompt: {Prompt}", documentId, prompt);

                var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("You are a document summarization assistant. Provide accurate and clear summaries.");
                chatHistory.AddUserMessage(prompt);

                var response = await chatCompletion.GetChatMessageContentAsync(chatHistory);
                var answer = response?.Content?.Trim() ?? "No summary generated.";

                return ChatResponse.CreateSuccess(answer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error summarizing document {DocumentId}", documentId);
                return ChatResponse.CreateError("An unexpected error occurred while summarizing the document.");
            }
        }

        /// <summary>
        /// Extracts key points from a document in bullet format.
        /// </summary>
        public async Task<ChatResponse> ExtractKeyPointsAsync(string documentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(documentId))
                    return ChatResponse.CreateError("DocumentId cannot be empty.");

                var documents = await _vectorStoreService.GetAllDocumentsAsync();
                var document = documents.FirstOrDefault(d => d.Id == documentId);

                if (document == null)
                    return ChatResponse.CreateError("Document not found.");

                var prompt = $"Please extract the key points from the following document in clear bullet point format:\n\n{document.Content}";

                _logger.LogInformation("ExtractKeyPoints | DocumentId: {DocumentId} | Prompt: {Prompt}", documentId, prompt);

                var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("You are a key point extraction assistant. Respond only in concise bullet points.");
                chatHistory.AddUserMessage(prompt);

                var response = await chatCompletion.GetChatMessageContentAsync(chatHistory);
                var answer = response?.Content?.Trim() ?? "No key points extracted.";

                return ChatResponse.CreateSuccess(answer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting key points for document {DocumentId}", documentId);
                return ChatResponse.CreateError("An unexpected error occurred while extracting key points.");
            }
        }

        /// <summary>
        /// Clears a single user's conversation history.
        /// </summary>
        public void ClearConversation(string userId)
        {
            if (!string.IsNullOrWhiteSpace(userId) && _conversations.ContainsKey(userId))
            {
                _conversations.Remove(userId);
                _logger.LogInformation("Conversation cleared for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Clears all conversation histories.
        /// </summary>
        public void ClearAllConversations()
        {
            _conversations.Clear();
            _logger.LogInformation("All conversations cleared.");
        }
    }
}
