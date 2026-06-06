DMMR SUGGESTION ENGINE
======================

High-performance hybrid suggestion engine for .NET.

OVERVIEW
--------

DMMR Suggestion Engine combines:

- Fuzzy Search
- BK-Tree Indexing
- Similarity Metrics
- Intelligent Ranking
- Local Suggestions
- OpenSearch Integration
- Result Caching

into a single easy-to-use API.

MAIN FEATURES
-------------

* Levenshtein-based fuzzy matching
* BK-Tree accelerated search
* Cosine Similarity
* Pearson Correlation
* Jaccard Similarity
* Euclidean Distance
* Manhattan Distance
* Weighted ranking
* Hybrid OpenSearch integration
* Built-in cache
* Async support
* CancellationToken support

QUICK EXAMPLE
-------------

var engine = new DMMRLocalSuggester<int>();

engine.LoadData(new[]
{
    (1, "iPhone 16", 100),
    (2, "Samsung Galaxy", 80),
    (3, "Motorola Edge", 50)
});

var results = engine.Suggest("iphon");

ARCHITECTURE
------------

User Query
    |
    v
Normalization
    |
    v
BK-Tree Search
    |
    v
Re-Ranking
    |
    v
Cache Layer
    |
    v
Final Results

PERFORMANCE
-----------

Designed for:

- Low memory usage
- Reduced allocations
- Fast fuzzy matching
- Large datasets
- High query throughput

ROADMAP
-------

Version 1.0 goals:

- Unit Tests
- BenchmarkDotNet
- XML Documentation
- CI/CD Pipeline
- NuGet Publishing
- Additional Ranking Algorithms

AUTHOR
------

Alessandro Silveira

DevOps Tech Leader
Software Architect
Performance-Oriented Systems Engineer

LICENSE
-------

MIT License