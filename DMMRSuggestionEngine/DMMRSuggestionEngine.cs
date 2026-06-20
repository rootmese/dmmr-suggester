using DMMRSuggestionEngine.Local;
using DMMRSuggestionEngine.Structures;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace DMMRSuggestionEngine
{
    public enum FuzzyScoreMode
    {
        Linear,
        Exponential,
        Inverse
    }

    public class DMMRSuggestionEngine<T> : IDMMRSuggestionEngine<T> where T : notnull
    {
        private class SuggestionItem
        {
            public T Entity { get; set; } = default!;
            public string NormalizedText { get; set; } = string.Empty;
            public float Weight { get; set; } = 1.0f;
        }

        private struct ReRankResult
        {
            public T Entity;
            public float Score;
        }

        private struct ReRankComparer : IComparer<ReRankResult>
        {
            public int Compare(ReRankResult x, ReRankResult y) => y.Score.CompareTo(x.Score);
        }

        private readonly List<SuggestionItem> _items = new();
        private IBKTree<SuggestionItem>? _bkTree;
        private bool _useBkTree;
        private readonly bool _useUnsafeBKTree;

        // ── N-Gram ────────────────────────────────────────────────────────────
        private readonly NgramIndex _ngramIndex = new();

        private readonly int _maxCacheSize = 1000;
        private readonly ConcurrentDictionary<string, List<T>> _cache = new();
        private readonly LinkedList<string> _cacheOrder = new();
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

        private int _dataVersion;
        private int _configVersion;

        // ── ReRankSettings ────────────────────────────────────────────────────

        public class ReRankSettings
        {
            private bool _enabled;
            private float _fuzzyWeight;
            private float _weightWeight;
            private FuzzyScoreMode _fuzzyMode;
            private float _fuzzyExponentialFactor;
            private float _fuzzyInverseFactor;
            private bool _prioritizeExactMatch;
            private float _exactMatchBonus;

            public bool Enabled { get => _enabled; set { _enabled = value; OnConfigChanged(); } }
            public float FuzzyWeight { get => _fuzzyWeight; set { _fuzzyWeight = value; OnConfigChanged(); } }
            public float WeightWeight { get => _weightWeight; set { _weightWeight = value; OnConfigChanged(); } }
            public FuzzyScoreMode FuzzyMode { get => _fuzzyMode; set { _fuzzyMode = value; OnConfigChanged(); } }
            public float FuzzyExponentialFactor { get => _fuzzyExponentialFactor; set { _fuzzyExponentialFactor = value; OnConfigChanged(); } }
            public float FuzzyInverseFactor { get => _fuzzyInverseFactor; set { _fuzzyInverseFactor = value; OnConfigChanged(); } }
            public bool PrioritizeExactMatch { get => _prioritizeExactMatch; set { _prioritizeExactMatch = value; OnConfigChanged(); } }
            public float ExactMatchBonus { get => _exactMatchBonus; set { _exactMatchBonus = value; OnConfigChanged(); } }

            public event Action? ConfigChanged;

            public ReRankSettings()
            {
                _enabled = false;
                _fuzzyWeight = 0.7f;
                _weightWeight = 0.3f;
                _fuzzyMode = FuzzyScoreMode.Linear;
                _fuzzyExponentialFactor = 4f;
                _fuzzyInverseFactor = 5f;
                _prioritizeExactMatch = false;
                _exactMatchBonus = 1.0f;
            }

            private void OnConfigChanged() => ConfigChanged?.Invoke();
        }

        // ── NgramSettings ─────────────────────────────────────────────────────

        /// <summary>
        /// Configurações do índice de N-Gramas.
        /// Permite busca por frases parciais, termos reordenados e prefixos incompletos.
        /// </summary>
        public class NgramSettings
        {
            private bool _enabled;
            private int _n;
            private float _minScore;
            private bool _padEdges;

            /// <summary>
            /// Habilita ou desabilita a busca via N-Gram (padrão: true).
            /// Quando desabilitado, o comportamento é idêntico à versão ≤ 0.1.4.
            /// </summary>
            public bool Enabled { get => _enabled; set { _enabled = value; OnConfigChanged(); } }

            /// <summary>
            /// Tamanho do n-grama (padrão: 3 = trigrams).
            /// Use 2 (bigrams) para datasets com nomes curtos.
            /// </summary>
            public int N { get => _n; set { _n = value < 1 ? 1 : value; OnConfigChanged(); } }

            /// <summary>
            /// Score mínimo de interseção de n-gramas para um candidato ser incluído [0,1].
            /// Score = ngramas_da_query_presentes_no_item / total_ngramas_da_query.
            /// Padrão: 0.2 (20% dos n-gramas da query devem existir no texto candidato).
            /// </summary>
            public float MinScore { get => _minScore; set { _minScore = Math.Clamp(value, 0f, 1f); OnConfigChanged(); } }

            /// <summary>
            /// Adiciona sentinelas '^' e '$' ao redor do texto ao indexar,
            /// melhorando o recall para buscas por prefixos e sufixos (padrão: true).
            /// </summary>
            public bool PadEdges { get => _padEdges; set { _padEdges = value; OnConfigChanged(); } }

            public event Action? ConfigChanged;

            public NgramSettings()
            {
                _enabled = true;
                _n = 3;
                _minScore = 0.2f;
                _padEdges = true;
            }

            private void OnConfigChanged() => ConfigChanged?.Invoke();
        }

        // ── Fields & Properties ───────────────────────────────────────────────

        private ReRankSettings _config;
        public ReRankSettings Config
        {
            get => _config;
            set
            {
                if (_config != null)
                    _config.ConfigChanged -= InvalidateCache;
                _config = value;
                _config.ConfigChanged += InvalidateCache;
                InvalidateCache();
            }
        }

        private NgramSettings _ngramConfig;

        /// <summary>
        /// Configurações do N-Gram Indexing (v0.1.5).
        /// Altere antes de chamar <see cref="LoadData"/> para que o índice
        /// seja construído com os parâmetros corretos.
        /// Alterações em <see cref="NgramSettings.MinScore"/> e
        /// <see cref="NgramSettings.Enabled"/> invalidam apenas o cache.
        /// Alterações em <see cref="NgramSettings.N"/> ou
        /// <see cref="NgramSettings.PadEdges"/> exigem nova chamada a
        /// <see cref="LoadData"/> para rebuildar o índice.
        /// </summary>
        public NgramSettings NgramConfig
        {
            get => _ngramConfig;
            set
            {
                if (_ngramConfig != null)
                    _ngramConfig.ConfigChanged -= InvalidateCache;
                _ngramConfig = value;
                _ngramConfig.ConfigChanged += InvalidateCache;
                InvalidateCache();
            }
        }

        // ── Constructor ───────────────────────────────────────────────────────

        public DMMRSuggestionEngine(bool useUnsafeBKTree = false)
        {
            _useUnsafeBKTree = useUnsafeBKTree;
            _config = new ReRankSettings();
            _config.ConfigChanged += InvalidateCache;
            _ngramConfig = new NgramSettings();
            _ngramConfig.ConfigChanged += InvalidateCache;
        }

        // ── Normalization ─────────────────────────────────────────────────────

        private static string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            // Otimização: strings ASCII não precisam de normalização Unicode
            bool needsNormalization = false;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] > 127)
                {
                    needsNormalization = true;
                    break;
                }
            }
            if (!needsNormalization)
                return input.ToLowerInvariant();

            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(input.Length);
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        // ── Cache ─────────────────────────────────────────────────────────────

        private void AddToCache(string key, List<T> value)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                if (_cache.ContainsKey(key))
                    _cacheOrder.Remove(key);
                else if (_cache.Count >= _maxCacheSize)
                {
                    if (_cacheOrder.First is { } first)
                    {
                        string oldest = first.Value;
                        _cacheOrder.RemoveFirst();
                        _cache.TryRemove(oldest, out _);
                    }
                }
                _cache[key] = value;
                _cacheOrder.AddLast(key);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        private bool TryGetFromCache(string key, out List<T>? value)
        {
            _cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (_cache.TryGetValue(key, out value))
                {
                    _cacheLock.EnterWriteLock();
                    try
                    {
                        _cacheOrder.Remove(key);
                        _cacheOrder.AddLast(key);
                    }
                    finally
                    {
                        _cacheLock.ExitWriteLock();
                    }
                    return true;
                }
                return false;
            }
            finally
            {
                _cacheLock.ExitUpgradeableReadLock();
            }
        }

        private void InvalidateCache()
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _cache.Clear();
                _cacheOrder.Clear();
                _configVersion++;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        // ── LoadData ──────────────────────────────────────────────────────────

        public void LoadData(IEnumerable<T> data, Func<T, string> textSelector, Func<T, float>? weightSelector = null)
        {
            var tempItems = new List<(T Entity, string RawText, float RawWeight)>();
            foreach (var item in data)
            {
                string text = textSelector(item);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    float rawWeight = weightSelector?.Invoke(item) ?? 1.0f;
                    tempItems.Add((item, text, rawWeight));
                }
            }

            float maxWeight = tempItems.Count > 0 ? tempItems.Max(x => x.RawWeight) : 1f;
            _items.Clear();

            foreach (var (entity, rawText, rawWeight) in tempItems)
            {
                float normalizedWeight = maxWeight > 0 ? rawWeight / maxWeight : 1f;
                _items.Add(new SuggestionItem
                {
                    Entity = entity,
                    NormalizedText = NormalizeString(rawText),
                    Weight = normalizedWeight
                });
            }

            // ── BK-Tree ────────────────────────────────────────────────────
            _useBkTree = _items.Count > 500;
            if (_useBkTree)
            {
                if (_bkTree == null)
                {
                    _bkTree = _useUnsafeBKTree
                        ? new UnsafeBKTree<SuggestionItem>()
                        : new SafeBKTree<SuggestionItem>();
                }
                else
                {
                    _bkTree.Clear();
                }
                foreach (var item in _items)
                    _bkTree.Add(item.NormalizedText, item);
            }
            else
            {
                _bkTree = null;
            }

            // ── N-Gram Index ───────────────────────────────────────────────
            // Sempre constrói o índice (útil mesmo para datasets pequenos,
            // pois cobre cenários que a BK-Tree não alcança).
            var ngramItems = new List<(string Text, int Index)>(_items.Count);
            for (int i = 0; i < _items.Count; i++)
                ngramItems.Add((_items[i].NormalizedText, i));

            _ngramIndex.Build(ngramItems, _ngramConfig.N, _ngramConfig.PadEdges);

            InvalidateCache();
            _dataVersion++;
        }

        // ── Suggest ───────────────────────────────────────────────────────────

        public List<T> Suggest(string query, int maxAllowedErrors = 2, int maxResults = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<T>();

            string cacheKey = $"{query}|{maxAllowedErrors}|{maxResults}|{Config.Enabled}|{Config.FuzzyWeight}|{Config.WeightWeight}|{Config.FuzzyMode}|{Config.FuzzyExponentialFactor}|{Config.FuzzyInverseFactor}|{Config.PrioritizeExactMatch}|{Config.ExactMatchBonus}|{NgramConfig.Enabled}|{NgramConfig.MinScore}|{_dataVersion}|{_configVersion}";

            if (TryGetFromCache(cacheKey, out var cachedResult))
                return cachedResult!;

            string normalizedQuery = NormalizeString(query);

            // ── 1. Candidatos BK-Tree / Levenshtein (typos) ───────────────
            List<(SuggestionItem Item, int Distance)> candidates;
            if (_useBkTree && _bkTree != null)
            {
                candidates = _bkTree.Search(normalizedQuery, maxAllowedErrors);
            }
            else
            {
                candidates = new List<(SuggestionItem, int)>();
                int queryLen = normalizedQuery.Length;
                foreach (var item in _items)
                {
                    if (Math.Abs(item.NormalizedText.Length - queryLen) <= maxAllowedErrors)
                    {
                        int dist = LevenshteinDistance(normalizedQuery, item.NormalizedText);
                        if (dist <= maxAllowedErrors)
                            candidates.Add((item, dist));
                    }
                }
            }

            // ── 2. Candidatos N-Gram (parcial / reordenado / prefixo) ─────
            if (_ngramConfig.Enabled)
            {
                var ngramHits = _ngramIndex.Search(normalizedQuery, _ngramConfig.MinScore);

                if (ngramHits.Count > 0)
                {
                    // Monta um HashSet com os itens já presentes nos candidatos
                    // para evitar duplicatas (compara por referência de objeto)
                    var seen = new HashSet<SuggestionItem>(ReferenceEqualityComparer.Instance);
                    foreach (var (item, _) in candidates)
                        seen.Add(item);

                    foreach (var (itemIdx, _) in ngramHits)
                    {
                        if (itemIdx < 0 || itemIdx >= _items.Count) continue;
                        var candidate = _items[itemIdx];
                        if (seen.Add(candidate))
                        {
                            // Para candidatos vindos apenas do N-Gram, a "distância"
                            // usada no rerank é o comprimento do texto (penalidade suave),
                            // pois não há distância de edição disponível sem calcular Levenshtein.
                            // Isso garante que candidatos BK-Tree com distância real sempre
                            // sejam preferidos quando o rerank estiver desabilitado.
                            int pseudoDist = Math.Max(candidate.NormalizedText.Length, normalizedQuery.Length);
                            candidates.Add((candidate, pseudoDist));
                        }
                    }
                }
            }

            if (candidates.Count == 0)
                return new List<T>();

            // ── 3. ReRank / Ordenação ─────────────────────────────────────
            List<T> result;
            if (Config.Enabled)
            {
                int k = Math.Min(maxResults, candidates.Count);
                if (k <= 0)
                    return new List<T>();

                // Renta um buffer minúsculo do tamanho de k para guardar os melhores resultados
                ReRankResult[] topK = ArrayPool<ReRankResult>.Shared.Rent(k);
                int count = 0;

                try
                {
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        var (item, dist) = candidates[i];
                        float maxPossibleDist = Math.Max(normalizedQuery.Length, item.NormalizedText.Length);
                        float normalizedDistance = maxPossibleDist > 0 ? dist / maxPossibleDist : 0f;

                        float fuzzyScore;
                        switch (Config.FuzzyMode)
                        {
                            case FuzzyScoreMode.Exponential:
                                fuzzyScore = MathF.Exp(-normalizedDistance * Config.FuzzyExponentialFactor);
                                break;
                            case FuzzyScoreMode.Inverse:
                                fuzzyScore = 1f / (1f + normalizedDistance * Config.FuzzyInverseFactor);
                                break;
                            default:
                                fuzzyScore = 1 - normalizedDistance;
                                break;
                        }

                        float finalScore = (Config.FuzzyWeight * fuzzyScore) + (Config.WeightWeight * item.Weight);
                        if (Config.PrioritizeExactMatch && dist == 0)
                            finalScore += Config.ExactMatchBonus;

                        if (count < k)
                        {
                            topK[count++] = new ReRankResult { Entity = item.Entity, Score = finalScore };
                            if (count == k)
                            {
                                // Ordena o buffer inicial em ordem decrescente de score
                                Array.Sort(topK, 0, k, new ReRankComparer());
                            }
                        }
                        else if (finalScore > topK[k - 1].Score)
                        {
                            // Substitui o menor elemento e faz a ordenação de inserção
                            topK[k - 1] = new ReRankResult { Entity = item.Entity, Score = finalScore };

                            // Bubble up para manter a ordenação
                            for (int j = k - 2; j >= 0; j--)
                            {
                                if (topK[j + 1].Score > topK[j].Score)
                                {
                                    var temp = topK[j];
                                    topK[j] = topK[j + 1];
                                    topK[j + 1] = temp;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }

                    result = new List<T>(count);
                    for (int i = 0; i < count; i++)
                        result.Add(topK[i].Entity);
                }
                finally
                {
                    ArrayPool<ReRankResult>.Shared.Return(topK, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<ReRankResult>());
                }
            }
            else
            {
                // Sem rerank: candidatos BK-Tree ordenados por distância + peso;
                // candidatos N-Gram (pseudo-dist elevada) ficam ao final naturalmente.
                candidates.Sort((a, b) =>
                {
                    int cmp = a.Distance.CompareTo(b.Distance);
                    if (cmp != 0) return cmp;
                    return b.Item.Weight.CompareTo(a.Item.Weight);
                });
                int take = Math.Min(maxResults, candidates.Count);
                result = new List<T>(take);
                for (int i = 0; i < take; i++)
                    result.Add(candidates[i].Item.Entity);
            }

            AddToCache(cacheKey, result);
            return result;
        }

        public void ClearCache() => InvalidateCache();

        // ── Levenshtein ───────────────────────────────────────────────────────

        // Implementação da distância de Levenshtein com Span e sem alocações
        private static int LevenshteinDistance(string s, string t)
        {
            if (s == t) return 0;
            if (s.Length == 0) return t.Length;
            if (t.Length == 0) return s.Length;

            int sLen = s.Length;
            int tLen = t.Length;

            // Aloca vetores na stack se forem pequenos (< 256), senão no heap
            Span<int> v0 = tLen < 256 ? stackalloc int[tLen + 1] : new int[tLen + 1];
            Span<int> v1 = tLen < 256 ? stackalloc int[tLen + 1] : new int[tLen + 1];

            for (int i = 0; i <= tLen; i++)
                v0[i] = i;

            for (int i = 0; i < sLen; i++)
            {
                v1[0] = i + 1;
                for (int j = 0; j < tLen; j++)
                {
                    int cost = (s[i] == t[j]) ? 0 : 1;
                    v1[j + 1] = Math.Min(Math.Min(v1[j] + 1, v0[j + 1] + 1), v0[j] + cost);
                }
                v1.CopyTo(v0);
            }
            return v0[tLen];
        }
    }
}