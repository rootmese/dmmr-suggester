using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DMMRSuggestionEngine.Local;
using DMMRSuggestionEngine.Structures;

namespace DMMRSuggestionEngine
{
    // Restrição notnull para permitir Dictionary<T, SuggestionItem>
    public class DMMRSuggestionEngine<T> where T : notnull
    {
        private class SuggestionItem
        {
            public T Entity { get; set; } = default!;
            public string NormalizedText { get; set; } = string.Empty;
            public float Weight { get; set; } = 1.0f;
        }

        private readonly List<SuggestionItem> _items = new();
        private Dictionary<T, SuggestionItem> _itemLookup = new();
        private BKTree<SuggestionItem>? _bkTree;
        private bool _useBkTree = false;

        private readonly int _maxCacheSize = 1000;
        private readonly ConcurrentDictionary<string, List<T>> _cache = new();
        private readonly LinkedList<string> _cacheOrder = new();
        private readonly object _cacheLock = new();

        private int _dataVersion = 0;
        private int _configVersion = 0;

        public class ReRankSettings
        {
            private bool _enabled;
            private float _fuzzyWeight;
            private float _weightWeight;

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

            public event Action? ConfigChanged;

            public ReRankSettings()
            {
                _enabled = false;
                _fuzzyWeight = 0.7f;
                _weightWeight = 0.3f;
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

        public DMMRSuggestionEngine()
        {
            _config = new ReRankSettings();
            _config.ConfigChanged += InvalidateCache;
        }

        private void AddToCache(string key, List<T> value)
        {
            lock (_cacheLock)
            {
                if (_cache.ContainsKey(key))
                {
                    _cacheOrder.Remove(key);
                }
                else if (_cache.Count >= _maxCacheSize)
                {
                    // Corrigido: obter o primeiro nó e seu valor
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
            _items.Clear();
            _itemLookup = new Dictionary<T, SuggestionItem>();

            foreach (var item in data)
            {
                string text = textSelector(item);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var sugItem = new SuggestionItem
                    {
                        Entity = item,
                        NormalizedText = text.ToLowerInvariant(),
                        Weight = weightSelector?.Invoke(item) ?? 1.0f
                    };
                    _items.Add(sugItem);
                    _itemLookup[item] = sugItem;
                }
            }

            if (_items.Count > 500)
            {
                BuildBKTree();
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

        private void BuildBKTree()
        {
            _bkTree = new BKTree<SuggestionItem>();
            foreach (var item in _items)
            {
                _bkTree.Add(item.NormalizedText, item);
            }
        }

        public List<T> Suggest(string query, int maxAllowedErrors = 2, int maxResults = 5)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<T>();

            string cacheKey = $"{query}|{maxAllowedErrors}|{maxResults}|{Config.Enabled}|{Config.FuzzyWeight}|{Config.WeightWeight}|{_dataVersion}|{_configVersion}";

            if (TryGetFromCache(cacheKey, out var cachedResult))
                return cachedResult!;

            string search = query.ToLowerInvariant();
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
                    float fuzzyScore = 1 - normalizedDistance;
                    float finalScore = (Config.FuzzyWeight * fuzzyScore) + (Config.WeightWeight * c.Item.Weight);
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
                                   .ThenBy(c => c.Item.Weight)
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