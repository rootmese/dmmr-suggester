using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace DMMRSuggestionEngine.Tests
{
    public class EngineTests
    {
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
            var result = engine.Suggest("a", maxAllowedErrors: 0, maxResults: 3);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void Suggest_WithReRank_ChangesOrder()
        {
            var engine = new DMMRSuggestionEngine<string>();
            var items = new[]
            {
                new { Name = "abc", Weight = 0.5f },
                new { Name = "abcd", Weight = 1.0f },
                new { Name = "ab", Weight = 0.2f }
            };
            engine.LoadData(items, x => x.Name, x => x.Weight);
            engine.Config.Enabled = true;
            engine.Config.FuzzyWeight = 0.3f;
            engine.Config.WeightWeight = 0.7f;

            var result = engine.Suggest("ab", maxAllowedErrors: 1, maxResults: 3);
            Assert.Equal("abcd", result.First());
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