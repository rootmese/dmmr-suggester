using System;
using System.Collections.Generic;
using System.Linq;

namespace DMMRSuggestionEngine.Local
{
    public class DMMRLocalSuggester
    {
        private readonly HashSet<string> _dictionary = new();

        public void AddWord(string word) =>
            _dictionary.Add(word.ToLowerInvariant());

        public void AddWords(IEnumerable<string> words)
        {
            foreach (var w in words) AddWord(w);
        }

        public List<string> Suggest(string query, int maxAllowedErrors = 2, int maxResults = 5)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<string>();

            string search = query.ToLowerInvariant();
            int searchLen = search.Length;

            return _dictionary
                .Where(word => Math.Abs(word.Length - searchLen) <= maxAllowedErrors)
                .Select(word => new { Word = word, Distance = DMMRFuzzyAlgorithm.CalculateDistance(search, word) })
                .Where(x => x.Distance <= maxAllowedErrors)
                .OrderBy(x => x.Distance)
                .ThenBy(x => x.Word)
                .Take(maxResults)
                .Select(x => x.Word)
                .ToList();
        }
    }
}
