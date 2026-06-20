using System;
using System.Collections.Generic;

namespace DMMRSuggestionEngine.Structures
{
    /// <summary>
    /// Inverted index de n-gramas de caracteres.
    /// Permite busca por frases parciais, termos reordenados e prefixos incompletos,
    /// complementando a BK-Tree que atua sobre a distância de edição inteira.
    ///
    /// Fluxo:
    ///   Build()  — indexa todos os itens (imutável após esta chamada)
    ///   Search() — retorna candidatos com score de interseção ≥ MinScore
    ///   Clear()  — reseta o índice
    ///
    /// Thread-safety: após Build(), Search() pode ser chamado de múltiplas threads
    /// sem lock, pois o índice não é mais modificado.
    /// </summary>
    internal sealed class NgramIndex
    {
        // ngram → conjunto de índices de itens que contêm esse ngram
        private Dictionary<string, List<int>> _index = new();
        // pré-computado: quantos n-gramas únicos cada item possui (denominador do score)
        private int[] _itemNgramCounts = Array.Empty<int>();
        private int _n;
        private bool _padEdges;

        /// <summary>
        /// Constrói o índice a partir de uma lista de textos normalizados.
        /// </summary>
        /// <param name="items">Pares (textoNormalizado, índice) dos itens.</param>
        /// <param name="n">Tamanho do n-grama (padrão: 3).</param>
        /// <param name="padEdges">
        ///   Se true, adiciona sentinelas '^' no início e '$' no final,
        ///   melhorando o recall para prefixos e sufixos.
        /// </param>
        public void Build(IReadOnlyList<(string Text, int Index)> items, int n = 3, bool padEdges = true)
        {
            _n = n;
            _padEdges = padEdges;
            _index = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            _itemNgramCounts = new int[items.Count];

            for (int i = 0; i < items.Count; i++)
            {
                var (text, itemIndex) = items[i];
                var ngrams = ExtractNgrams(text, n, padEdges);
                _itemNgramCounts[i] = ngrams.Count;

                foreach (var ng in ngrams)
                {
                    if (!_index.TryGetValue(ng, out var list))
                    {
                        list = new List<int>();
                        _index[ng] = list;
                    }
                    // Evita duplicatas de índice (o mesmo item não entra duas vezes
                    // pelo mesmo n-grama, embora o texto possa ter repetições)
                    if (list.Count == 0 || list[list.Count - 1] != itemIndex)
                        list.Add(itemIndex);
                }
            }
        }

        /// <summary>
        /// Busca candidatos cujo score de interseção de n-gramas com a query
        /// seja ≥ <paramref name="minScore"/>.
        ///
        /// Score = ngramas_da_query_presentes_no_item / total_ngramas_da_query
        /// (recall-oriented: mede quanto da query está coberta pelo item)
        /// </summary>
        /// <param name="query">Texto normalizado da query.</param>
        /// <param name="minScore">Score mínimo [0,1] para ser candidato.</param>
        /// <returns>Pares (índiceDoItem, score) ordenados por score descendente.</returns>
        public List<(int ItemIndex, float Score)> Search(string query, float minScore = 0.2f)
        {
            if (string.IsNullOrEmpty(query) || _index.Count == 0)
                return new List<(int, float)>();

            var queryNgrams = ExtractNgrams(query, _n, _padEdges);
            if (queryNgrams.Count == 0)
                return new List<(int, float)>();

            // Contagem de hits por índice de item
            var hits = new Dictionary<int, int>();

            foreach (var ng in queryNgrams)
            {
                if (_index.TryGetValue(ng, out var list))
                {
                    foreach (int idx in list)
                    {
                        hits.TryGetValue(idx, out int count);
                        hits[idx] = count + 1;
                    }
                }
            }

            if (hits.Count == 0)
                return new List<(int, float)>();

            float queryNgramCount = queryNgrams.Count;
            var results = new List<(int ItemIndex, float Score)>(hits.Count);

            foreach (var (itemIdx, hitCount) in hits)
            {
                float score = hitCount / queryNgramCount;
                if (score >= minScore)
                    results.Add((itemIdx, score));
            }

            // Ordena por score descendente
            results.Sort((a, b) => b.Score.CompareTo(a.Score));
            return results;
        }

        /// <summary>Reseta o índice.</summary>
        public void Clear()
        {
            _index.Clear();
            _itemNgramCounts = Array.Empty<int>();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Extrai o conjunto único de n-gramas de um texto.
        /// Se padEdges=true, o texto é envolto com sentinelas:
        ///   "abc" com n=3 → "^ab", "abc", "bc$"
        /// </summary>
        private static HashSet<string> ExtractNgrams(string text, int n, bool padEdges)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);

            if (string.IsNullOrEmpty(text))
                return set;

            // Adiciona sentinelas, se necessário
            string padded = padEdges
                ? string.Concat("^", text, "$")
                : text;

            if (padded.Length < n)
            {
                // Texto menor que n: usa o texto inteiro como ngrama único
                set.Add(padded);
                return set;
            }

            int count = padded.Length - n + 1;
            for (int i = 0; i < count; i++)
                set.Add(padded.Substring(i, n));

            return set;
        }
    }
}
