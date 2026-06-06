namespace DMMRSuggestionEngine.Models
{
    public class SuggestionDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public float Weight { get; set; }

        /// <summary>
        /// Vetor de embeddings (ex: 384 dimensões do modelo 'all-MiniLM-L6-v2')
        /// </summary>
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}