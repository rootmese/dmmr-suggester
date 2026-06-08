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
*Note: results reflect the improved scoring logic (weight normalization, configurable fuzzy curves, optional exact-match bonus)*

| Method                       | Items   | Latency (median) | Allocation per call | Observation                                      |
|------------------------------|---------|------------------|---------------------|--------------------------------------------------|
| **Exact / Typo / NoMatch**   | 1,000   | ~0.08 ms         | 672 B               | Slightly higher allocation due to normalization |
| **WithReRank**               | 1,000   | 1.48 ms          | 152 KB              | Faster than before – improved scoring path      |
| **Exact / Typo / NoMatch**   | 10,000  | ~0.08 ms         | 672 B               | Still scales perfectly with BK‑Tree             |
| **WithReRank**               | 10,000  | 9.96 ms          | 936 KB              | Consistent overhead                            |
| **Exact / Typo / NoMatch**   | 100,000 | ~0.11 ms         | 672 B               | Excellent constant‑time behaviour               |
| **WithReRank**               | 100,000 | 25.24 ms         | 4.56 MB             | Very usable for API / web autocomplete          |

## Interpretation

- **Fuzzy search without rerank** keeps median latency **below 0.12 ms** even with 100,000 items, thanks to the BK‑Tree index and LRU cache.  
- **Rerank** (combining fuzzy similarity and item weight) now includes **weight normalization** (weights scaled to [0,1]), optional **non‑linear fuzzy curves** (exponential, inverse), and an **exact‑match bonus**. Overhead is controlled: ~1.5 ms for 1k items, ~10 ms for 10k, ~25 ms for 100k – still excellent for interactive use.  
- **Memory allocation** for searches without rerank is only **672 bytes** per call – minimal GC pressure. The small increase from previous 200 bytes is due to richer cache keys and weight normalization, which also improves ranking quality.  
- Outliers (first cold execution) are not representative of production behaviour; the engine stays warm in memory.

## Key Improvements in This Version

- Weight normalization during `LoadData` (0 ≤ weight ≤ 1)  
- Three fuzzy score modes: `Linear` (default), `Exponential`, `Inverse`  
- Optional exact‑match bonus (`PrioritizeExactMatch` + `ExactMatchBonus`)  
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