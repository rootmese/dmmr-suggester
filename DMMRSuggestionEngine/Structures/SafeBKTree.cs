using System;
using System.Collections.Generic;

namespace DMMRSuggestionEngine.Structures
{
    public class SafeBKTree<T> : IBKTree<T>
    {
        private readonly object _lock = new();
        private readonly UnsafeBKTree<T> _inner;

        public SafeBKTree(Func<string, string, int>? distanceFunc = null)
        {
            _inner = new UnsafeBKTree<T>(distanceFunc);
        }

        public void Add(string key, T value)
        {
            lock (_lock)
                _inner.Add(key, value);
        }

        public List<(T Value, int Distance)> Search(string query, int maxDistance)
        {
            lock (_lock)
                return _inner.Search(query, maxDistance);
        }

        public void Clear()
        {
            lock (_lock)
                _inner.Clear();
        }
    }
}