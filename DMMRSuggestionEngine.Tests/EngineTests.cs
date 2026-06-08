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
            var items = new[]
            {
        new TestItem { Name = "ab", Weight = 0.2f },
        new TestItem { Name = "abc", Weight = 0.5f },
        new TestItem { Name = "abcd", Weight = 1.0f } // só para ter um terceiro
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
            engine.LoadData(new[] { "a", "b" }, x => x);
            engine.LoadData(new[] { "c", "d" }, x => x);
            var result = engine.Suggest("a");
            Assert.DoesNotContain("a", result);
        }
    }
}