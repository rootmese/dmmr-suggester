using System;

namespace DMMRSuggestionEngine.Metrics
{
    public static class DMMRSimilarityMetrics
    {
        public static float Cosine(float[] a, float[] b) =>
            Cosine((ReadOnlySpan<float>)a, b);

        public static float Cosine(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
        {
            if (a.Length != b.Length) ThrowLengthMismatch();
            float dot = 0, normA = 0, normB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }
            return (normA == 0 || normB == 0) ? 0 : dot / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        public static float Pearson(float[] a, float[] b) =>
            Pearson((ReadOnlySpan<float>)a, b);

        public static float Pearson(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
        {
            if (a.Length != b.Length) ThrowLengthMismatch();
            int n = a.Length;
            float sumA = 0, sumB = 0;
            for (int i = 0; i < n; i++) { sumA += a[i]; sumB += b[i]; }
            float meanA = sumA / n, meanB = sumB / n;
            float num = 0, denA = 0, denB = 0;
            for (int i = 0; i < n; i++)
            {
                float dA = a[i] - meanA;
                float dB = b[i] - meanB;
                num += dA * dB;
                denA += dA * dA;
                denB += dB * dB;
            }
            return (denA == 0 || denB == 0) ? 0 : num / (float)(Math.Sqrt(denA) * Math.Sqrt(denB));
        }

        public static float Jaccard(int[] a, int[] b) =>
            Jaccard((ReadOnlySpan<int>)a, b);

        public static float Jaccard(ReadOnlySpan<int> a, ReadOnlySpan<int> b)
        {
            if (a.Length != b.Length) ThrowLengthMismatch();
            int inter = 0, uni = 0;
            for (int i = 0; i < a.Length; i++)
            {
                bool hasA = a[i] != 0, hasB = b[i] != 0;
                if (hasA && hasB) inter++;
                if (hasA || hasB) uni++;
            }
            return uni == 0 ? 0 : (float)inter / uni;
        }

        public static float EuclideanSimilarity(float[] a, float[] b) =>
            EuclideanSimilarity((ReadOnlySpan<float>)a, b);

        public static float EuclideanSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b) =>
            1f / (1f + EuclideanDistance(a, b));

        private static float EuclideanDistance(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
        {
            if (a.Length != b.Length) ThrowLengthMismatch();
            float sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                float d = a[i] - b[i];
                sum += d * d;
            }
            return (float)Math.Sqrt(sum);
        }

        public static float ManhattanSimilarity(float[] a, float[] b) =>
            ManhattanSimilarity((ReadOnlySpan<float>)a, b);

        public static float ManhattanSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b) =>
            1f / (1f + ManhattanDistance(a, b));

        private static float ManhattanDistance(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
        {
            if (a.Length != b.Length) ThrowLengthMismatch();
            float sum = 0;
            for (int i = 0; i < a.Length; i++)
                sum += Math.Abs(a[i] - b[i]);
            return sum;
        }

        private static void ThrowLengthMismatch() =>
            throw new ArgumentException("Vectors must have the same length.");
    }
}
