using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Engines;
using DMMRSuggestionEngine;

[SimpleJob(RunStrategy.ColdStart, iterationCount: 5)]
[MemoryDiagnoser]
[RankColumn]
public class SuggestionBenchmark
{
    private DMMRSuggestionEngine<string> _engine = null!;
    private List<string> _dataSmall = null!;
    private List<string> _dataMedium = null!;
    private List<string> _dataLarge = null!;

    [Params(1000, 10000, 100000)]
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _engine = new DMMRSuggestionEngine<string>();
        _dataSmall = Enumerable.Range(0, 1000).Select(i => $"item_{i}").ToList();
        _dataMedium = Enumerable.Range(0, 10000).Select(i => $"item_{i}").ToList();
        _dataLarge = Enumerable.Range(0, 100000).Select(i => $"item_{i}").ToList();

        // Carrega dados de acordo com o tamanho parametrizado
        var data = DataSize switch
        {
            1000 => _dataSmall,
            10000 => _dataMedium,
            _ => _dataLarge
        };
        _engine.LoadData(data, x => x);
    }

    [Benchmark(Baseline = true)]
    public void Search_ExactMatch() => _engine.Suggest("item_123");

    [Benchmark]
    public void Search_Typo_OneError() => _engine.Suggest("item_124", maxAllowedErrors: 1);

    [Benchmark]
    public void Search_Typo_TwoErrors() => _engine.Suggest("ittem_125", maxAllowedErrors: 2);

    [Benchmark]
    public void Search_WithReRank()
    {
        _engine.Config.Enabled = true;
        _engine.Config.FuzzyWeight = 0.6f;
        _engine.Config.WeightWeight = 0.4f;
        _engine.Suggest("item_126", maxAllowedErrors: 2);
        _engine.Config.Enabled = false; // reset
    }

    [Benchmark]
    public void Search_NoMatch() => _engine.Suggest("xyzabc", maxAllowedErrors: 2);
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<SuggestionBenchmark>();
    }
}