using Elasticsearch.Net;
using Nest;
using SemanticKernelDocumentQA.Services;
using SemanticKernelDocumentQA.Models;

namespace SemanticKernelDocumentQA.Services
{
    public class VectorStoreService : IVectorStoreService
    {
        private readonly IElasticClient _elasticClient;
        private readonly ILogger<VectorStoreService> _logger;
        private const string DocumentIndex = "documents";
        private const string ChunkIndex = "document_chunks";

        public VectorStoreService(IElasticClient elasticClient, ILogger<VectorStoreService> logger)
        {
            _elasticClient = elasticClient;
            _logger = logger;
            InitializeIndices();
        }

        private async void InitializeIndices()
        {
            try
            {
                var documentsIndexExists = await _elasticClient.Indices.ExistsAsync(DocumentIndex);
                if (!documentsIndexExists.Exists)
                {
                    await _elasticClient.Indices.CreateAsync(DocumentIndex, c => c
                        .Map<DocumentInfo>(m => m
                            .Properties(p => p
                                .Text(t => t.Name(n => n.Content).Analyzer("standard"))
                                .Keyword(k => k.Name(n => n.Id))
                                .Keyword(k => k.Name(n => n.Filename))
                                .Date(d => d.Name(n => n.UploadedAt))
                            )
                        )
                    );
                }

                var chunksIndexExists = await _elasticClient.Indices.ExistsAsync(ChunkIndex);
                if (!chunksIndexExists.Exists)
                {
                    await _elasticClient.Indices.CreateAsync(ChunkIndex, c => c
                        .Map<DocumentChunk>(m => m
                            .Properties(p => p
                                .Text(t => t.Name(n => n.Content).Analyzer("standard"))
                                .Keyword(k => k.Name(n => n.DocumentId))
                                .Number(n => n.Name(n => n.ChunkNumber))
                            )
                        )
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Elasticsearch indices");
            }
        }

        public async Task<bool> IndexDocumentAsync(string documentId, string content, string filename)
        {
            try
            {
                // Index the full document
                var document = new DocumentInfo
                {
                    Id = documentId,
                    Content = content,
                    Filename = filename,
                    UploadedAt = DateTime.UtcNow
                };

                var indexResponse = await _elasticClient.IndexDocumentAsync(document);
                if (!indexResponse.IsValid)
                {
                    _logger.LogError("Failed to index document: {Error}", indexResponse.DebugInformation);
                    return false;
                }

                // Create chunks for better search
                var chunks = CreateChunks(content, documentId);
                var bulkResponse = await _elasticClient.BulkAsync(b => b
                    .Index(ChunkIndex)
                    .IndexMany(chunks)
                );

                if (!bulkResponse.IsValid)
                {
                    _logger.LogError("Failed to index chunks: {Error}", bulkResponse.DebugInformation);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing document {DocumentId}", documentId);
                return false;
            }
        }

        public async Task<List<string>> SearchSimilarChunksAsync(string query, int maxResults = 5)
        {
            try
            {
                var searchResponse = await _elasticClient.SearchAsync<DocumentChunk>(s => s
                    .Index(ChunkIndex)
                    .Query(q => q
                        .Match(m => m
                            .Field(f => f.Content)
                            .Query(query)
                            .Fuzziness(Fuzziness.Auto)
                        )
                    )
                    .Size(maxResults)
                    .Highlight(h => h
                        .Fields(f => f
                            .Field(ff => ff.Content)
                            .PreTags("<mark>")
                            .PostTags("</mark>")
                        )
                    )
                );

                if (!searchResponse.IsValid)
                {
                    _logger.LogError("Search failed: {Error}", searchResponse.DebugInformation);
                    return new List<string>();
                }

                return searchResponse.Documents.Select(d => d.Content).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching chunks for query: {Query}", query);
                return new List<string>();
            }
        }

        public async Task<bool> DeleteDocumentAsync(string documentId)
        {
            try
            {
                // Delete document
                var deleteDocResponse = await _elasticClient.DeleteAsync<DocumentInfo>(documentId, d => d.Index(DocumentIndex));
                
                // Delete associated chunks
                var deleteChunksResponse = await _elasticClient.DeleteByQueryAsync<DocumentChunk>(d => d
                    .Index(ChunkIndex)
                    .Query(q => q
                        .Term(t => t
                            .Field(f => f.DocumentId)
                            .Value(documentId)
                        )
                    )
                );

                return deleteDocResponse.IsValid && deleteChunksResponse.IsValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                return false;
            }
        }

        public async Task<List<DocumentInfo>> GetAllDocumentsAsync()
        {
            try
            {
                var searchResponse = await _elasticClient.SearchAsync<DocumentInfo>(s => s
                    .Index(DocumentIndex)
                    .Query(q => q.MatchAll())
                    .Size(1000)
                );

                if (!searchResponse.IsValid)
                {
                    _logger.LogError("Failed to get documents: {Error}", searchResponse.DebugInformation);
                    return new List<DocumentInfo>();
                }

                return searchResponse.Documents.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all documents");
                return new List<DocumentInfo>();
            }
        }

        private List<DocumentChunk> CreateChunks(string content, string documentId)
        {
            var chunks = new List<DocumentChunk>();
            const int chunkSize = 1000;
            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < words.Length; i += chunkSize)
            {
                var chunkWords = words.Skip(i).Take(chunkSize);
                var chunkContent = string.Join(" ", chunkWords);
                
                chunks.Add(new DocumentChunk
                {
                    DocumentId = documentId,
                    Content = chunkContent,
                    ChunkNumber = i / chunkSize
                });
            }

            return chunks;
        }
    }

    public class DocumentChunk
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int ChunkNumber { get; set; }
    }
}