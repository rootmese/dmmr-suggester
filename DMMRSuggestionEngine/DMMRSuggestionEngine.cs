using DMMRSuggestionEngine.Local;
using DMMRSuggestionEngine.Structures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DMMRSuggestionEngine
{
    public enum FuzzyScoreMode
    {
        /// <summary>Score = 1 - normalizedDistance (comportamento original)</summary>
        Linear,
        /// <summary>Score = exp(-k * normalizedDistance) - penaliza erros pequenos suavemente</summary>
        Exponential,
        /// <summary>Score = 1 / (1 + k * normalizedDistance) - curva similar a exponencial mas mais simples</summary>
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

        private readonly List<SuggestionItem> _items = new();
        private IBKTree<SuggestionItem>? _bkTree;
        private bool _useBkTree;
        private readonly bool _useUnsafeBKTree;

        private readonly int _maxCacheSize = 1000;
        private readonly ConcurrentDictionary<string, List<T>> _cache = new();
        private readonly LinkedList<string> _cacheOrder = new();
        private readonly object _cacheLock = new();

        private int _dataVersion;
        private int _configVersion;

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

            public bool Enabled
            {
                get => _enabled;
                set { _enabled = value; OnConfigChanged(); }
            }

            public float FuzzyWeight
            {
                get => _fuzzyWeight;
                set { _fuzzyWeight = value; OnConfigChanged(); }
            }

            public float WeightWeight
            {
                get => _weightWeight;
                set { _weightWeight = value; OnConfigChanged(); }
            }

            public FuzzyScoreMode FuzzyMode
            {
                get => _fuzzyMode;
                set { _fuzzyMode = value; OnConfigChanged(); }
            }

            /// <summary>Fator k para o modo Exponential (score = exp(-k * normalizedDistance)). Default = 4f.</summary>
            public float FuzzyExponentialFactor
            {
                get => _fuzzyExponentialFactor;
                set { _fuzzyExponentialFactor = value; OnConfigChanged(); }
            }

            /// <summary>Fator k para o modo Inverse (score = 1 / (1 + k * normalizedDistance)). Default = 5f.</summary>
            public float FuzzyInverseFactor
            {
                get => _fuzzyInverseFactor;
                set { _fuzzyInverseFactor = value; OnConfigChanged(); }
            }

            /// <summary>Se true, adiciona um bônus ao score de itens com distância exata 0.</summary>
            public bool PrioritizeExactMatch
            {
                get => _prioritizeExactMatch;
                set { _prioritizeExactMatch = value; OnConfigChanged(); }
            }

            /// <summary>Bônus somado ao score quando PrioritizeExactMatch = true e Distance == 0. Default = 1.0f.</summary>
            public float ExactMatchBonus
            {
                get => _exactMatchBonus;
                set { _exactMatchBonus = value; OnConfigChanged(); }
            }

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

        public DMMRSuggestionEngine(bool useUnsafeBKTree = false)
        {
            _useUnsafeBKTree = useUnsafeBKTree;
            _config = new ReRankSettings();
            _config.ConfigChanged += InvalidateCache;
        }

        private static string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        private void AddToCache(string key, List<T> value)
        {
            lock (_cacheLock)
            {
                if (_cache.ContainsKey(key))
                    _cacheOrder.Remove(key);
                else if (_cache.Count >= _maxCacheSize)
                {
                    var firstNode = _cacheOrder.First;
                    if (firstNode != null)
                    {
                        string oldest = firstNode.Value;
                        _cacheOrder.RemoveFirst();
                        _cache.TryRemove(oldest, out _);
                    }
                }

                _cache[key] = value;
                _cacheOrder.AddLast(key);
            }
        }

        private bool TryGetFromCache(string key, out List<T>? value)
        {
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(key, out value))
                {
                    _cacheOrder.Remove(key);
                    _cacheOrder.AddLast(key);
                    return true;
                }
                return false;
            }
        }

        private void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _cache.Clear();
                _cacheOrder.Clear();
                _configVersion++;
            }
        }

        public void LoadData(IEnumerable<T> data, Func<T, string> textSelector, Func<T, float>? weightSelector = null)
        {
            // Coleta temporária para calcular peso máximo
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

            // Decide se usa BK-Tree baseado no número de itens
            if (_items.Count > 500)
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

                _useBkTree = true;
            }
            else
            {
                _bkTree = null;
                _useBkTree = false;
            }

            InvalidateCache();
            _dataVersion++;
        }

        public List<T> Suggest(string query, int maxAllowedErrors = 2, int maxResults = 5)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<T>();

            string cacheKey = $"{query}|{maxAllowedErrors}|{maxResults}|{Config.Enabled}|{Config.FuzzyWeight}|{Config.WeightWeight}|{Config.FuzzyMode}|{Config.FuzzyExponentialFactor}|{Config.FuzzyInverseFactor}|{Config.PrioritizeExactMatch}|{Config.ExactMatchBonus}|{_dataVersion}|{_configVersion}";

            if (TryGetFromCache(cacheKey, out var cachedResult))
                return cachedResult!;

            string search = NormalizeString(query);
            int searchLen = search.Length;

            IEnumerable<(SuggestionItem Item, int Distance)> candidates;

            if (_useBkTree && _bkTree != null)
            {
                var bkResults = _bkTree.Search(search, maxAllowedErrors);
                candidates = bkResults.Select(r => (r.Value, r.Distance));
            }
            else
            {
                var filtered = _items
                    .Where(x => Math.Abs(x.NormalizedText.Length - searchLen) <= maxAllowedErrors)
                    .ToList();

                if (_items.Count > 10000)
                {
                    candidates = filtered.AsParallel()
                        .Select(item => (item, Distance: DMMRFuzzyAlgorithm.CalculateDistance(search, item.NormalizedText)))
                        .Where(x => x.Distance <= maxAllowedErrors)
                        .AsEnumerable();
                }
                else
                {
                    candidates = filtered
                        .Select(item => (item, Distance: DMMRFuzzyAlgorithm.CalculateDistance(search, item.NormalizedText)))
                        .Where(x => x.Distance <= maxAllowedErrors);
                }
            }

            List<T> result;
            if (Config.Enabled)
            {
                result = candidates.Select(c =>
                {
                    float maxPossibleDist = Math.Max(search.Length, c.Item.NormalizedText.Length);
                    float normalizedDistance = maxPossibleDist > 0 ? c.Distance / maxPossibleDist : 0;
                    float fuzzyScore;

                    switch (Config.FuzzyMode)
                    {
                        case FuzzyScoreMode.Exponential:
                            fuzzyScore = MathF.Exp(-normalizedDistance * Config.FuzzyExponentialFactor);
                            break;
                        case FuzzyScoreMode.Inverse:
                            fuzzyScore = 1f / (1f + normalizedDistance * Config.FuzzyInverseFactor);
                            break;
                        default: // Linear
                            fuzzyScore = 1 - normalizedDistance;
                            break;
                    }

                    float finalScore = (Config.FuzzyWeight * fuzzyScore) + (Config.WeightWeight * c.Item.Weight);
                    if (Config.PrioritizeExactMatch && c.Distance == 0)
                        finalScore += Config.ExactMatchBonus;

                    return (Entity: c.Item.Entity, Score: finalScore);
                })
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .Select(x => x.Entity)
                .ToList();
            }
            else
            {
                result = candidates.OrderBy(c => c.Distance)
                                   .ThenByDescending(c => c.Item.Weight)
                                   .Take(maxResults)
                                   .Select(c => c.Item.Entity)
                                   .ToList();
            }

            AddToCache(cacheKey, result);
            return result;
        }

        public void ClearCache() => InvalidateCache();
    }
}