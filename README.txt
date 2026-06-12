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

var engine = new DMMRSuggestionEngine<(int Id, string Name, float Weight)>();

engine.LoadData(new[]
{
    (1, "iPhone 16", 100f),
    (2, "Samsung Galaxy", 80f),
    (3, "Motorola Edge", 50f)
},
x => x.Name,
x => x.Weight
);

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

- [x] Unit Tests
- [x] BenchmarkDotNet
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
