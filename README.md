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

BENCHMARK RESULTS
-----------------

Benchmarks executados em Intel Core i7-8550U, .NET 9.0, modo Release.
Cada operação é uma consulta fuzzy com `maxAllowedErrors=2` (exceto ExactMatch).

| Método               | Itens  | Latência (mediana) | Alocação por chamada | Observação                              |
|----------------------|--------|-------------------|----------------------|------------------------------------------|
| **Exact / Typo / NoMatch** | 1.000  | ~0,06 ms          | 200 B                | Mesmo desempenho para todos os tipos de erro |
| **WithReRank**       | 1.000  | 2,19 ms           | 152 KB               | Custo adicional de ordenação e score     |
| **Exact / Typo / NoMatch** | 10.000 | ~0,07 ms          | 200 B                | Escala perfeitamente graças à BK‑Tree    |
| **WithReRank**       | 10.000 | 8,98 ms           | 936 KB               |                                          |
| **Exact / Typo / NoMatch** | 100.000| ~0,07 ms          | 200 B                | Desempenho constante – excelente!        |
| **WithReRank**       | 100.000| 23,77 ms          | 4,5 MB               | Ainda aceitável para autocomplete em API |

### Interpretação

- **Busca fuzzy sem rerank** mantém latência **< 0,1 ms** mesmo com 100.000 itens, graças à BK‑Tree + cache LRU.
- O **rerank** (que combina similaridade fuzzy e peso do item) adiciona um overhead previsível e controlado. Para 100k itens, 24 ms ainda é excelente para cenários web.
- A **alocação de memória** sem rerank é mínima (200 bytes), evitando pressão sobre o GC.
- Os outliers observados no benchmark (primeira execução de cada processo) não representam o desempenho em produção, onde a engine permanece carregada em memória.

### Como reproduzir

```bash
cd DMMRSuggestionEngine.Benchmark
dotnet run -c Release
```

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