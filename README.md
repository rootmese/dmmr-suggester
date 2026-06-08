# DMMR Suggestion Engine

[![.NET](https://img.shields.io/badge/.NET-9.0-blue)]()
[![License](https://img.shields.io/badge/license-MIT-green)]()

A high-performance hybrid suggestion engine for .NET applications.

DMMR Suggestion Engine combines fuzzy matching, similarity metrics, BK-Tree indexing, local ranking, caching, and OpenSearch integration into a single, easy-to-use API.

Designed for:

- Product search
- Autocomplete systems
- Search suggestions
- Typo correction
- Recommendation engines
- Offline and online search scenarios

---

# Features

## Local Fuzzy Search

Supports typo-tolerant search using:

- Levenshtein Distance
- BK-Tree indexing
- Weighted ranking
- Query normalization

Example:

```text
iphone
iphon
ifone
iphne
```

All can produce relevant results.

---

## Similarity Metrics

Built-in similarity algorithms:

- Cosine Similarity
- Pearson Correlation
- Jaccard Similarity
- Euclidean Distance
- Manhattan Distance

Optimized using:

```csharp
ReadOnlySpan<float>
```

to minimize allocations.

---

## Hybrid Search

Combine local search with OpenSearch.

Benefits:

- Fast local suggestions
- Large-scale distributed search
- Re-ranking support
- Better relevance

---

## Intelligent Ranking

Results are ranked using:

- Edit distance
- Item weight
- Text length
- Similarity score

This helps prioritize the most relevant suggestions.

---

## BK-Tree Optimization

For large datasets the engine automatically enables BK-Tree indexing.

Benefits:

- Faster fuzzy lookups
- Reduced search space
- Better scalability

---

## Built-in Cache

Includes result caching with automatic size control.

Benefits:

- Reduced CPU usage
- Faster repeated queries
- Lower latency

---

# Installation

```bash
dotnet add package DMMRSuggestionEngine
```

---

# Quick Start

## Create Engine

```csharp
var engine = new DMMRLocalSuggester<int>();
```

---

## Load Data

```csharp
engine.LoadData(new[]
{
    (1, "iPhone 16", 100),
    (2, "Samsung Galaxy", 80),
    (3, "Motorola Edge", 50)
});
```

---

## Search

```csharp
var results = engine.Suggest("iphon");
```

---

## Example Output

```text
iPhone 16
Samsung Galaxy
Motorola Edge
```

ordered by relevance.

---

# Architecture

```text
                 +----------------+
                 | User Query     |
                 +--------+-------+
                          |
                          v
                 +----------------+
                 | Normalization  |
                 +--------+-------+
                          |
                          v
                 +----------------+
                 | BK-Tree Search |
                 +--------+-------+
                          |
                          v
                 +----------------+
                 | Re-Ranking     |
                 +--------+-------+
                          |
                          v
                 +----------------+
                 | Cache Layer    |
                 +--------+-------+
                          |
                          v
                 +----------------+
                 | Final Results  |
                 +----------------+
```

---

# OpenSearch Integration

```csharp
var service = new DMMRHybridSearchService(configuration);
```

Supports:

- OpenSearch
- Hybrid search
- Async queries
- CancellationToken

---

# Performance

Designed with:

- Low allocations
- ReadOnlySpan<T>
- BK-Tree indexing
- Controlled caching
- O(1) entity lookups

Suitable for:

- APIs
- E-commerce
- Recommendation systems
- Search-heavy applications

---

# Roadmap

## Version 1.0

- [ ] Unit Tests
- [ ] BenchmarkDotNet Suite
- [ ] XML Documentation
- [ ] GitHub Actions CI/CD
- [ ] NuGet Publishing Pipeline
- [ ] Additional Ranking Strategies

---

# BENCHMARK RESULTS
-----------------

Benchmarks executed on Intel Core i7-8550U, .NET 9.0, Release mode.  
Each operation is a fuzzy query with `maxAllowedErrors=2` (except ExactMatch).  
*Note: results reflect the improved scoring logic (weight normalization, configurable fuzzy curves, optional exact-match bonus) and optimized reranking (struct arrays, manual sort).*

| Method                       | Items   | Latency (median) | Allocation per call | Observation                                      |
|------------------------------|---------|------------------|---------------------|--------------------------------------------------|
| **Exact / Typo / NoMatch**   | 1,000   | ~0.09 ms         | 672 B               | Minimal allocation, same for all error types    |
| **WithReRank**               | 1,000   | 4.31 ms          | 146 KB              | Reliable and predictable overhead               |
| **Exact / Typo / NoMatch**   | 10,000  | ~0.12 ms         | 672 B               | Scales perfectly thanks to BK‑Tree              |
| **WithReRank**               | 10,000  | 26.11 ms         | 906 KB              | Slightly higher but still fast                  |
| **Exact / Typo / NoMatch**   | 100,000 | ~0.10 ms         | 672 B               | Excellent constant‑time behaviour               |
| **WithReRank**               | 100,000 | 27.88 ms         | 4.33 MB             | Fully acceptable for API / web autocomplete     |

## Interpretation

- **Fuzzy search without rerank** keeps median latency **below 0.15 ms** for all dataset sizes, thanks to the BK‑Tree index and LRU cache.  
- **Rerank** (combining fuzzy similarity and item weight) now includes **weight normalization** (weights scaled to [0,1]), optional **non‑linear fuzzy curves** (exponential, inverse), and an **exact‑match bonus**. The optimized implementation uses `struct` arrays and `Array.Sort`, reducing GC pressure and improving consistency. Observed latencies: ~4.3 ms (1k), ~26 ms (10k), ~28 ms (100k) – still excellent for interactive use.  
- **Memory allocation** for searches without rerank is minimal (≈672 bytes per call) – no GC pressure. For rerank, allocations are proportional to the number of candidates (≈150 KB for 1k, up to 4.3 MB for 100k), which is acceptable for typical workloads.  
- Outliers (first cold execution) are not representative of production performance; the engine stays warm in memory.

## Key Improvements in This Version

- Weight normalization during `LoadData` (0 ≤ weight ≤ 1)  
- Three fuzzy score modes: `Linear` (default), `Exponential`, `Inverse`  
- Optional exact‑match bonus (`PrioritizeExactMatch` + `ExactMatchBonus`)  
- High‑performance reranking using `struct` arrays and manual sorting  
- Better concurrency with `ReaderWriterLockSlim` on the cache  
- Reduced LINQ usage and stack‑allocated buffers for Levenshtein distance  
- Fully backward‑compatible defaults  

## Features

- Edit‑distance‑based fuzzy search  
- BK‑Tree for large datasets  
- Built‑in LRU cache  
- Configurable item weight reranking  
- .NET 9 compatible  
- No external dependencies  
- Generic type‑safe API (`DMMRSuggestionEngine<T>`)  

## How to reproduce

```bash
cd DMMRSuggestionEngine.Benchmark
dotnet run -c Release```

---

# Contributing

Contributions are welcome.

Feel free to submit:

- Pull Requests
- Issues
- Feature Requests

---

# License

MIT License.

---

# Author

Alessandro Silveira

DevOps Tech Leader, Software Architect and Performance-Oriented Systems Engineer.