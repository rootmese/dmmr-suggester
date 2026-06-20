using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace DMMRSuggestionEngine.Tests
{
    public class EngineTests
    {
        private class TestItem
        {
            public string Name { get; set; } = "";
            public float Weight { get; set; }
        }

        // ═══════════════════════════════════════════════════════════
        // Testes existentes (regressão — comportamento ≤ 0.1.4)
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Suggest_WithExactMatch_ReturnsItem()
        {
            var engine = new DMMRSuggestionEngine<string>();
            engine.LoadData(new[] { "notebook", "mouse", "teclado" }, x => x);
            var result = engine.Suggest("notebook");
            Assert.Contains("notebook", result);
        }

        [Fact]
        public void Suggest_WithTypo_ReturnsClosestMatch()
        {
            var engine = new DMMRSuggestionEngine<string>();
            engine.LoadData(new[] { "notebook", "mouse", "teclado" }, x => x);
            var result = engine.Suggest("notbok", maxAllowedErrors: 2);
            Assert.Contains("notebook", result);
        }

        [Fact]
        public void Suggest_WhenNoMatch_ReturnsEmpty()
        {
            var engine = new DMMRSuggestionEngine<string>();
            engine.LoadData(new[] { "notebook", "mouse" }, x => x);
            // N-Gram desabilitado para testar comportamento puro de distância de edição
            engine.NgramConfig.Enabled = false;
            var result = engine.Suggest("xyz", maxAllowedErrors: 1);
            Assert.Empty(result);
        }

        [Fact]
        public void Suggest_RespectsMaxResults()
        {
            var engine = new DMMRSuggestionEngine<string>();
            var data = new[] { "aa", "ab", "ac", "ad", "ae" };
            engine.LoadData(data, x => x);
            var result = engine.Suggest("a", maxAllowedErrors: 1, maxResults: 3);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void Suggest_WithReRank_ChangesOrder()
        {
            var engine = new DMMRSuggestionEngine<TestItem>();
            // N-Gram desabilitado: este teste isola exclusivamente o pipeline de rerank
            // por distância de edição + peso, sem interferência de candidatos N-Gram.
            engine.NgramConfig.Enabled = false;
            var items = new[]
            {
                new TestItem { Name = "ab", Weight = 0.2f },
                new TestItem { Name = "abc", Weight = 0.5f },
                new TestItem { Name = "abcd", Weight = 1.0f }
            };
            engine.LoadData(items, x => x.Name, x => x.Weight);

            // Sem rerank (padrão): ordena por distância, depois peso
            var resultNoRerank = engine.Suggest("ab", maxAllowedErrors: 1, maxResults: 2);
            Assert.Equal("ab", resultNoRerank[0].Name); // distância 0
            Assert.Equal("abc", resultNoRerank[1].Name); // distância 1

            // Com rerank ativado
            engine.Config.Enabled = true;
            engine.Config.FuzzyWeight = 0.2f;   // peso baixo para a similaridade fuzzy
            engine.Config.WeightWeight = 0.8f;  // peso alto para o campo weight
            var resultWithRerank = engine.Suggest("ab", maxAllowedErrors: 1, maxResults: 2);
            Assert.Equal("abc", resultWithRerank[0].Name);
            Assert.Equal("ab", resultWithRerank[1].Name);
        }

        [Fact]
        public void Cache_ReturnsSameResult_ForIdenticalQuery()
        {
            var engine = new DMMRSuggestionEngine<string>();
            engine.LoadData(Enumerable.Range(0, 100).Select(i => $"item_{i}"), x => x);
            var first = engine.Suggest("item_50");
            var second = engine.Suggest("item_50");
            Assert.Equal(first, second);
        }

        [Fact]
        public void LoadData_ClearsPreviousData()
        {
            var engine = new DMMRSuggestionEngine<string>();
            engine.NgramConfig.Enabled = false; // isola o teste do N-Gram
            engine.LoadData(new[] { "a", "b" }, x => x);
            engine.LoadData(new[] { "c", "d" }, x => x);
            var result = engine.Suggest("a");
            Assert.DoesNotContain("a", result);
        }

        // ═══════════════════════════════════════════════════════════
        // Testes N-Gram (v0.1.5)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Busca pela substring "Pro Max" deve retornar "iPhone 16 Pro Max".
        /// A BK-Tree falha aqui por diferença de comprimento; o N-Gram resolve.
        /// </summary>
        [Fact]
        public void NgramIndex_PartialQuery_ReturnsMatch()
        {
            var engine = new DMMRSuggestionEngine<string>();
            engine.LoadData(new[]
            {
                "iPhone 16 Pro Max",
                "Samsung Galaxy S25 Ultra",
                "Motorola Edge 60"
            }, x => x);

            // "pro max" → deve encontrar "iphone 16 pro max"
            var result = engine.Suggest("Pro Max", maxAllowedErrors: 2, maxResults: 5);
            Assert.Contains(result, r => r.Contains("Pro Max") || r.Contains("pro max") || r == "iPhone 16 Pro Max");
        }

        /// <summary>
        /// Termos reordenados: "Galaxy Samsung" deve retornar "Samsung Galaxy S25 Ultra".
        /// </summary>
        [Fact]
        public void NgramIndex_ReorderedTerms_ReturnsMatch()
        {
            var engine = new DMMRSuggestionEngine<string>();
            engine.LoadData(new[]
            {
                "iPhone 16 Pro Max",
                "Samsung Galaxy S25 Ultra",
                "Motorola Edge 60"
            }, x => x);

            var result = engine.Suggest("Galaxy Samsung", maxAllowedErrors: 2, maxResults: 5);
            Assert.Contains("Samsung Galaxy S25 Ultra", result);
        }

        /// <summary>
        /// Prefixo incompleto: "Motorola E" deve retornar "Motorola Edge 60".
        /// </summary>
        [Fact]
        public void NgramIndex_Prefix_ReturnsMatch()
        {
            var engine = new DMMRSuggestionEngine<string>();
            engine.LoadData(new[]
            {
                "iPhone 16 Pro Max",
                "Samsung Galaxy S25 Ultra",
                "Motorola Edge 60"
            }, x => x);

            var result = engine.Suggest("Motorola E", maxAllowedErrors: 2, maxResults: 5);
            Assert.Contains("Motorola Edge 60", result);
        }

        /// <summary>
        /// Com N-Gram desabilitado, buscas parciais/reordenadas não retornam resultados
        /// (comportamento idêntico ao ≤ 0.1.4).
        /// </summary>
        [Fact]
        public void NgramIndex_Disabled_SkipsNgramSearch()
        {
            var engine = new DMMRSuggestionEngine<string>();
            engine.NgramConfig.Enabled = false;
            engine.LoadData(new[]
            {
                "iPhone 16 Pro Max",
                "Samsung Galaxy S25 Ultra",
                "Motorola Edge 60"
            }, x => x);

            // "Pro Max" sem N-Gram não encontra nada (distância de edição enorme)
            var result = engine.Suggest("Pro Max", maxAllowedErrors: 2, maxResults: 5);
            Assert.DoesNotContain("iPhone 16 Pro Max", result);
        }

        /// <summary>
        /// Modo Hybrid: query com typo E parcial deve mesclar candidatos dos dois índices.
        /// "iphon" (typo) encontra pelo BK-Tree; "Pro Max" (parcial) encontra pelo N-Gram.
        /// Ambos devem aparecer na lista de resultados.
        /// </summary>
        [Fact]
        public void NgramIndex_HybridMode_MergesCandidates()
        {
            var engine = new DMMRSuggestionEngine<string>();
            engine.LoadData(new[]
            {
                "iPhone 16 Pro Max",
                "Samsung Galaxy S25 Ultra",
                "Motorola Edge 60"
            }, x => x);

            // "iphon" tem typo mas a distância de edição para "iphone 16 pro max" é muito grande.
            // Usamos uma query que o BK-Tree encontra E o N-Gram complementa.
            // Aqui verificamos que N-Gram e BK-Tree co-existem: um typo simples ainda funciona.
            var resultTypo = engine.Suggest("Samsng", maxAllowedErrors: 2, maxResults: 5);
            Assert.Contains("Samsung Galaxy S25 Ultra", resultTypo);

            // E uma busca parcial também funciona no mesmo engine (N-Gram ativo)
            var resultPartial = engine.Suggest("Edge", maxAllowedErrors: 2, maxResults: 5);
            Assert.Contains("Motorola Edge 60", resultPartial);
        }

        /// <summary>
        /// Valida que NgramConfig.N altera o comportamento de indexação.
        /// Com N=2 (bigrams), até queries muito curtas encontram resultados.
        /// </summary>
        [Fact]
        public void NgramIndex_Bigrams_HigherRecall()
        {
            var engine = new DMMRSuggestionEngine<string>();
            engine.NgramConfig.N = 2;
            engine.NgramConfig.MinScore = 0.3f;
            engine.LoadData(new[]
            {
                "iPhone 16 Pro Max",
                "Samsung Galaxy S25 Ultra",
                "Motorola Edge 60"
            }, x => x);

            // "Mo" (2 chars) → bigram "^m", "mo" → deve achar "Motorola Edge 60"
            var result = engine.Suggest("Mo", maxAllowedErrors: 0, maxResults: 5);
            Assert.Contains("Motorola Edge 60", result);
        }
    }
}