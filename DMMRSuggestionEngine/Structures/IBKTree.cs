using System;
using System.Collections.Generic;

namespace DMMRSuggestionEngine.Structures
{
    public interface IBKTree<T>
    {
        void Add(string key, T value);
        List<(T Value, int Distance)> Search(string query, int maxDistance);
        void Clear();
    }
}