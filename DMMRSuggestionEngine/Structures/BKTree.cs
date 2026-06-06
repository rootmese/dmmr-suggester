using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DMMRSuggestionEngine.Local;

namespace DMMRSuggestionEngine.Structures
{
    /// <summary>
    /// Implementação de uma BK-Tree para busca fuzzy eficiente.
    /// </summary>
    /// <typeparam name="T">Tipo do item armazenado.</typeparam>
    public class BKTree<T>
    {
        private class Node
        {
            public string Key { get; set; }
            public T Value { get; set; }
            public Dictionary<int, Node> Children { get; } = new();

            public Node(string key, T value) => (Key, Value) = (key, value);
        }

        private Node? _root;
        private readonly Func<string, string, int> _distanceFunc;

        public BKTree(Func<string, string, int>? distanceFunc = null)
        {
            _distanceFunc = distanceFunc ?? DMMRFuzzyAlgorithm.CalculateDistance;
        }

        public void Add(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty");
            key = key.ToLowerInvariant();

            if (_root == null)
            {
                _root = new Node(key, value);
                return;
            }

            Node current = _root;
            while (true)
            {
                int dist = _distanceFunc(key, current.Key);
                if (dist == 0)
                {
                    current.Value = value;
                    return;
                }

                if (current.Children.TryGetValue(dist, out Node? next) && next != null)
                    current = next;
                else
                {
                    current.Children[dist] = new Node(key, value);
                    return;
                }
            }
        }

        /// <summary>
        /// Busca todos os itens com distância <= maxDistance.
        /// </summary>
        public List<(T Value, int Distance)> Search(string query, int maxDistance)
        {
            if (_root == null || string.IsNullOrEmpty(query)) return new List<(T, int)>();
            query = query.ToLowerInvariant();

            var results = new List<(T, int)>();
            var stack = new Stack<Node>();
            stack.Push(_root);

            while (stack.Count > 0)
            {
                Node current = stack.Pop();
                int dist = _distanceFunc(query, current.Key);

                if (dist <= maxDistance)
                    results.Add((current.Value, dist));

                int lower = dist - maxDistance;
                int upper = dist + maxDistance;

                foreach (var (childDist, child) in current.Children)
                {
                    if (childDist >= lower && childDist <= upper)
                        stack.Push(child);
                }
            }
            return results;
        }

        /// <summary>
        /// Versão assíncrona – executa a busca em outra thread.
        /// </summary>
        public async Task<List<(T Value, int Distance)>> SearchAsync(string query, int maxDistance)
        {
            return await Task.Run(() => Search(query, maxDistance));
        }
    }
}