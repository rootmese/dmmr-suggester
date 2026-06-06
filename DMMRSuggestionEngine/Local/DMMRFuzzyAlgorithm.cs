

namespace DMMRSuggestionEngine.Local
{
    public static class DMMRFuzzyAlgorithm
    {
        public static int CalculateDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
            if (string.IsNullOrEmpty(target)) return source.Length;

            ReadOnlySpan<char> s = source.AsSpan();
            ReadOnlySpan<char> t = target.AsSpan();

            if (s.Length > t.Length)
            {
                var temp = s;
                s = t;
                t = temp;
            }

            int n = s.Length;
            int m = t.Length;

            int[] prev = new int[n + 1];
            int[] curr = new int[n + 1];

            for (int i = 0; i <= n; i++)
                prev[i] = i;

            for (int j = 1; j <= m; j++)
            {
                curr[0] = j;
                for (int i = 1; i <= n; i++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    curr[i] = Math.Min(
                        Math.Min(curr[i - 1] + 1, prev[i] + 1),
                        prev[i - 1] + cost
                    );
                }
                (prev, curr) = (curr, prev);
            }
            return prev[n];
        }
    }
}
