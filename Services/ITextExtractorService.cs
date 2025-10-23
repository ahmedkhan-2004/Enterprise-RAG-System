using System.Threading.Tasks;

namespace SemanticKernelDocumentQA.Services
{
    public interface ITextExtractorService
    {
        Task<string> ExtractTextAsync(string filePath);
    }
}
