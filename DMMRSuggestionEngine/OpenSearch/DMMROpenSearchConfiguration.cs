using System;
using System.Threading.Tasks;
using OpenSearch.Client;
using DMMRSuggestionEngine.Models;


namespace DMMRSuggestionEngine.OpenSearch
{
    public static class OpenSearchConfiguration
    {
        public static async Task CreateIndexIfNotExistsAsync(OpenSearchClient client, string indexName, int embeddingDimension = 384)
        {
            var existsResponse = await client.Indices.ExistsAsync(indexName);
            if (existsResponse.Exists) return;

            var createResponse = await client.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .Setting("index.knn", true)
                    .Setting("index.number_of_shards", 1)
                    .Setting("index.number_of_replicas", 0))
                .Map<SuggestionDocument>(m => m
                    .Properties(p => p
                        .Text(t => t.Name(n => n.Text).Analyzer("portuguese"))
                        .Keyword(k => k.Name(n => n.Category))
                        .Number(n => n.Name(nn => nn.Weight).Type(NumberType.Float))
                        .KnnVector(kv => kv.Name(n => n.Embedding).Dimension(embeddingDimension))
                    )
                )
            );

            if (!createResponse.IsValid)
                throw new Exception($"Error creating index: {createResponse.DebugInformation}");
        }
    }
}
