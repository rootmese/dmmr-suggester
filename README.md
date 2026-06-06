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