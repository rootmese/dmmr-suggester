
using DMMRSuggestionEngine.Models;

namespace DMMRSuggestionEngine.OpenSearch
{
    public interface IHybridSearchService
    {
        Task<List<SuggestionDocument>> HybridSearchAsync(
            string userQuery,
            float[] queryEmbedding,
            string? categoryFilter = null,
            int maxResults = 10,
            CancellationToken cancellationToken = default);
    }
}
