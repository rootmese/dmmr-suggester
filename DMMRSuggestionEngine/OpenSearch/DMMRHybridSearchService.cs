using DMMRSuggestionEngine.Models;
using OpenSearch.Client;
using OpenSearch.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DMMRSuggestionEngine.OpenSearch
{
    public class DMMROHybridSearchService : IHybridSearchService
    {
        private readonly OpenSearchClient _client;
        private readonly IOpenSearchLowLevelClient _lowLevelClient;
        private readonly string _indexName;

        public DMMROHybridSearchService(OpenSearchClient client, string indexName)
        {
            _client = client;
            _lowLevelClient = client.LowLevel;
            _indexName = indexName;
        }

        public async Task<List<SuggestionDocument>> HybridSearchAsync(
            string userQuery,
            float[] queryEmbedding,
            string? categoryFilter = null,
            int maxResults = 10,
            CancellationToken cancellationToken = default)
        {
            var queryObj = new
            {
                size = maxResults,
                query = new
                {
                    hybrid = new
                    {
                        queries = new object[]
                        {
                            new
                            {
                                match = new
                                {
                                    text = new
                                    {
                                        query = userQuery,
                                        fuzziness = "AUTO",
                                        @operator = "or"
                                    }
                                }
                            },
                            new
                            {
                                knn = new
                                {
                                    embedding = new
                                    {
                                        vector = queryEmbedding,
                                        k = maxResults
                                    }
                                }
                            }
                        }
                    }
                },
                post_filter = categoryFilter != null ? new
                {
                    term = new
                    {
                        category = new
                        {
                            value = categoryFilter
                        }
                    }
                } : null
            };

            var options = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
            string jsonQuery = JsonSerializer.Serialize(queryObj, options);

            // Corrigido: passar null como SearchRequestParameters (ou um objeto vazio)
            var response = await _lowLevelClient.SearchAsync<StringResponse>(
                _indexName,
                jsonQuery,
                null,   // SearchRequestParameters (pode ser null)
                cancellationToken
            );

            if (!response.Success)
                throw new Exception($"Erro na busca híbrida: {response.Body}");

            using JsonDocument doc = JsonDocument.Parse(response.Body);
            JsonElement hits = doc.RootElement.GetProperty("hits").GetProperty("hits");
            var documents = new List<SuggestionDocument>();
            foreach (JsonElement hit in hits.EnumerateArray())
            {
                JsonElement source = hit.GetProperty("_source");
                var document = JsonSerializer.Deserialize<SuggestionDocument>(source.GetRawText());
                if (document != null) documents.Add(document);
            }
            return documents;
        }
    }
}