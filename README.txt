DMMR SUGGESTION ENGINE
======================

High-performance hybrid suggestion engine for .NET.

VERSION
-------

0.1.5

OVERVIEW
--------

DMMR Suggestion Engine combines:

- Fuzzy Search
- BK-Tree Indexing
- N-Gram Indexing (v0.1.5)
- Similarity Metrics
- Intelligent Ranking
- Local Suggestions
- OpenSearch Integration
- Result Caching

into a single easy-to-use API.

MAIN FEATURES
-------------

* Levenshtein-based fuzzy matching
* BK-Tree accelerated search (auto-enabled for datasets > 500 items)
* N-Gram character indexing — partial phrases, reordered terms, incomplete prefixes
* Cosine Similarity
* Pearson Correlation
* Jaccard Similarity
* Euclidean Distance
* Manhattan Distance
* Weighted ranking
* Configurable ReRank pipeline (Linear / Exponential / Inverse)
* Hybrid OpenSearch integration
* Built-in LRU cache (auto-invalidation on data/config change)
* Async support
* CancellationToken support

QUICK EXAMPLE
-------------

var engine = new DMMRSuggestionEngine<(int Id, string Name, float Weight)>();

engine.LoadData(new[]
{
    (1, "iPhone 16 Pro Max", 100f),
    (2, "Samsung Galaxy S25 Ultra", 80f),
    (3, "Motorola Edge 60", 50f)
},
x => x.Name,
x => x.Weight
);

// Typo tolerance (BK-Tree)
var r1 = engine.Suggest("iphon");

// Partial phrase (N-Gram)
var r2 = engine.Suggest("Pro Max");

// Reordered terms (N-Gram)
var r3 = engine.Suggest("Galaxy Samsung");

// Incomplete prefix (N-Gram)
var r4 = engine.Suggest("Motorola E");

N-GRAM CONFIGURATION
--------------------

engine.NgramConfig.Enabled  = true;   // enable/disable (default: true)
engine.NgramConfig.N        = 3;      // n-gram size: 3=trigram, 2=bigram
engine.NgramConfig.MinScore = 0.2f;   // minimum intersection score [0,1]
engine.NgramConfig.PadEdges = true;   // ^ / $ sentinels for prefix/suffix recall

ARCHITECTURE
------------

User Query
    |
    v
Normalization
    |
    +------------------+
    |                  |
    v                  v
BK-Tree Search    N-Gram Index Search
(typo tolerance)  (partial / reorder)
    |                  |
    +------------------+
    |
    v
Merge & Deduplicate candidates
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
- Reduced allocations (stackalloc Levenshtein, ArrayPool ReRank)
- Fast fuzzy matching
- Large datasets
- High query throughput

ROADMAP
-------

Version 1.0 goals:

- [x] Unit Tests
- [x] BenchmarkDotNet
- [x] N-Gram Indexing (v0.1.5)
- [ ] XML Documentation
- [ ] CI/CD Pipeline
- [ ] NuGet Publishing
- [ ] Additional Ranking Algorithms

AUTHOR
------

Alessandro Silveira

DevOps Tech Leader
Software Architect
Performance-Oriented Systems Engineer

LICENSE
-------

MIT License

VERSIONING POLICY
---------------------

This project follows a maintenance-oriented versioning model.

Version numbers do not primarily represent technical stability.
Instead, they represent the maintainer's support commitment.

0.x Releases
------------

Versions in the 0.x series are considered functional and tested.

A 0.x release may be used in production environments at the user's
discretion, but:

  - APIs may change without notice
  - Features may be redesigned
  - Long-term maintenance is not guaranteed
  - Backward compatibility is not a goal

The purpose of the 0.x series is to validate architecture,
collect feedback, and evaluate community adoption.

1.x Releases
------------

A project reaches version 1.x only when there is an explicit
commitment to ongoing maintenance.

This includes:

  - Active updates
  - Dependency modernization
  - Security fixes when required
  - Backward compatibility considerations
  - Long-term roadmap commitment

In this model, version 1.x represents a maintenance commitment
rather than a statement about technical maturity.

Notes
-----

A 0.x release should not be interpreted as unstable software.

Many projects in the 0.x series may already be suitable for
production workloads, depending on the user's requirements.

The distinction between 0.x and 1.x is support commitment,
not implementation quality.
