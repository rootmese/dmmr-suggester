using System;
using DMMRSuggestionEngine.Tests;

namespace DMMRSuggestionEngine.Tests
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Executando testes da DMMRSuggestionEngine...\n");

            var tests = new EngineTests();

            try { tests.Suggest_WithExactMatch_ReturnsItem(); Console.WriteLine("✅ Suggest_WithExactMatch_ReturnsItem"); }
            catch (Exception ex) { Console.WriteLine($"❌ Suggest_WithExactMatch_ReturnsItem: {ex.Message}"); return 1; }

            try { tests.Suggest_WithTypo_ReturnsClosestMatch(); Console.WriteLine("✅ Suggest_WithTypo_ReturnsClosestMatch"); }
            catch (Exception ex) { Console.WriteLine($"❌ Suggest_WithTypo_ReturnsClosestMatch: {ex.Message}"); return 1; }

            try { tests.Suggest_WhenNoMatch_ReturnsEmpty(); Console.WriteLine("✅ Suggest_WhenNoMatch_ReturnsEmpty"); }
            catch (Exception ex) { Console.WriteLine($"❌ Suggest_WhenNoMatch_ReturnsEmpty: {ex.Message}"); return 1; }

            try { tests.Suggest_RespectsMaxResults(); Console.WriteLine("✅ Suggest_RespectsMaxResults"); }
            catch (Exception ex) { Console.WriteLine($"❌ Suggest_RespectsMaxResults: {ex.Message}"); return 1; }

            try { tests.Suggest_WithReRank_ChangesOrder(); Console.WriteLine("✅ Suggest_WithReRank_ChangesOrder"); }
            catch (Exception ex) { Console.WriteLine($"❌ Suggest_WithReRank_ChangesOrder: {ex.Message}"); return 1; }

            try { tests.Cache_ReturnsSameResult_ForIdenticalQuery(); Console.WriteLine("✅ Cache_ReturnsSameResult_ForIdenticalQuery"); }
            catch (Exception ex) { Console.WriteLine($"❌ Cache_ReturnsSameResult_ForIdenticalQuery: {ex.Message}"); return 1; }

            try { tests.LoadData_ClearsPreviousData(); Console.WriteLine("✅ LoadData_ClearsPreviousData"); }
            catch (Exception ex) { Console.WriteLine($"❌ LoadData_ClearsPreviousData: {ex.Message}"); return 1; }

            Console.WriteLine("\n🎉 Todos os testes passaram!");
            return 0;
        }
    }
}