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

## N-Gram Indexing _(v0.1.5)_

The engine now includes a **character-level N-Gram inverted index** that complements BK-Tree search.

While BK-Tree excels at typo correction, N-Gram indexing handles scenarios that edit-distance cannot:

- **Partial phrases**: `"Pro Max"` → finds `"iPhone 16 Pro Max"`
- **Reordered terms**: `"Galaxy Samsung"` → finds `"Samsung Galaxy S25 Ultra"`
- **Incomplete prefixes**: `"Motorola E"` → finds `"Motorola Edge 60"`

Both indexes are queried in parallel and their candidates merged before re-ranking.

Configuration:

```csharp
engine.NgramConfig.Enabled  = true;   // enable/disable (default: true)
engine.NgramConfig.N        = 3;      // n-gram size: 3=trigram (default), 2=bigram
engine.NgramConfig.MinScore = 0.2f;   // minimum intersection score [0,1]
engine.NgramConfig.PadEdges = true;   // add ^ / $ sentinels for prefix/suffix recall
```

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

## Create Engine and Load Data

```csharp
using DMMRSuggestionEngine;

// Instanciar a engine definindo o tipo do item
var engine = new DMMRSuggestionEngine<(int Id, string Name, float Weight)>();

// Carregar os dados fornecendo a coleção e os seletores de texto e peso
engine.LoadData(
    new[]
    {
        (1, "iPhone 16", 100f),
        (2, "Samsung Galaxy", 80f),
        (3, "Motorola Edge", 50f)
    },
    x => x.Name,   // Seletor de texto para busca
    x => x.Weight  // Seletor de peso para relevância
);
```

---

## Search

```csharp
// Realizar a busca de sugestões
var results = engine.Suggest("iphon");
```

---

## Example Output

O retorno será uma lista ordenada por relevância das tuplas originais correspondentes:
1. `(1, "iPhone 16", 100f)`
2. `(2, "Samsung Galaxy", 80f)`
3. `(3, "Motorola Edge", 50f)`

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
              +-----------+-----------+
              |                       |
              v                       v
    +------------------+   +--------------------+
    | BK-Tree Search   |   | N-Gram Index Search|
    | (typo tolerance) |   | (partial / reorder)|
    +--------+---------+   +---------+----------+
              |                       |
              +-----------+-----------+
                          |
                          v
                 +------------------+
                 | Merge & Dedupe   |
                 | candidates       |
                 +--------+---------+
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
using DMMRSuggestionEngine.OpenSearch;
using OpenSearch.Client;

var connectionSettings = new ConnectionSettings(new Uri("http://localhost:9200"));
var openSearchClient = new OpenSearchClient(connectionSettings);

var service = new DMMROHybridSearchService(openSearchClient, "suggestions-index");
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

## Roadmap

### ✅ Version 0.1.5 — Advanced Query Matching _(Released)_

#### N-Gram Indexing

DMMR Suggestion Engine now supports **N-Gram character indexing** as a hybrid complement to BK-Tree.

Previously, searches such as partial phrases, reordered terms, or incomplete expressions would return no results because the edit distance between the query and the full product name was too large.

Example dataset:

```text
iPhone 16 Pro Max
Samsung Galaxy S25 Ultra
Motorola Edge 60
```

Now the following queries all return relevant results:

```text
"Pro Max"         → iPhone 16 Pro Max
"Galaxy Samsung"  → Samsung Galaxy S25 Ultra
"Motorola E"      → Motorola Edge 60
```

The N-Gram index is built automatically during `LoadData` and queried in parallel with the BK-Tree. Results from both are merged and fed into the existing re-ranking pipeline.

---

## BM25 Ranking _(Planned)_

Future versions of DMMR Suggestion Engine will support **BM25 (Best Matching 25)** ranking.

BM25 is the de facto ranking algorithm used by modern search engines such as:

- Elasticsearch
- OpenSearch
- Apache Lucene

Unlike edit-distance algorithms, BM25 evaluates:

- Term frequency
- Document frequency
- Query relevance
- Document length normalization

This allows more accurate ranking when multiple candidates match the same query.

Example:

Query:

```text
iphone pro
```

Results:

```text
iPhone 16 Pro Max
iPhone 15 Pro
iPhone Case Pro Edition
```

BM25 helps prioritize results based on textual relevance rather than only fuzzy distance.

Planned usage:

```csharp
engine.RankingAlgorithm = RankingAlgorithm.BM25;
```

Or as part of a hybrid ranking strategy:

```csharp
engine.RankingAlgorithm = RankingAlgorithm.Hybrid;
```

Where the final score may combine:

- Levenshtein Distance
- N-Gram Similarity
- BM25 Score
- Item Weight

Benefits:

- Better relevance
- Improved result ordering
- Industry-standard ranking model
- Better performance on large datasets

Status:

```text
Planned for future releases
```

---

# BENCHMARK RESULTS
-----------------

Benchmarks executed on Intel Core i7-8550U, .NET 9.0, Release mode.  
Each operation is a fuzzy query with `maxAllowedErrors=2` (except ExactMatch)
*Note: results reflect the optimized Top-K reranking (using ArrayPool, struct comparers, and early-exit top-K insertion) and stackalloc-based Levenshtein distance calculations.*

| Method                       | Items   | Latency (median) | Allocation per call | Observation                                      |
|------------------------------|---------|------------------|---------------------|--------------------------------------------------|
| **Exact / Typo / NoMatch**   | 1,000   | ~0.05 ms         | ~640 B              | Zero heap allocations during search calculations |
| **WithReRank**               | 1,000   | 0.82 ms          | 66 KB               | Optimized Top-K selection with ArrayPool         |
| **Exact / Typo / NoMatch**   | 10,000  | ~0.08 ms         | ~640 B              | Scales perfectly thanks to BK‑Tree & stackalloc  |
| **WithReRank**               | 10,000  | 9.42 ms          | 66 KB               | Over 10x memory allocation reduction             |
| **Exact / Typo / NoMatch**   | 100,000 | ~0.10 ms         | ~640 B              | Excellent constant-time behavior                 |
| **Partial (N-Gram)**         | 100,000 | ~0.12 ms         | ~670 B              | Pure N-Gram search is virtually allocation-free  |
| **WithReRank**               | 100,000 | 28.67 ms         | 66 KB               | 98.5% reduction in heap allocation (from 4.3MB)  |

## Interpretation

- **Fuzzy search without rerank** keeps median latency **below 0.10 ms** for all dataset sizes, thanks to the BK‑Tree index, stackalloc Levenshtein distance, and LRU cache.  
- **Partial search (N-Gram)** resolves missing substrings using the inverted index with minimal overhead (~670 Bytes allocation), running nearly as fast as the BK-Tree, even at 100,000 items.
- **Rerank** (combining fuzzy similarity and item weight) uses an optimized **Top-K Selection** algorithm with **ArrayPool** and **struct-based comparers**. Instead of allocating large arrays of size $N$ and sorting them, the engine rents a tiny buffer of size `maxResults` and performs in-place bubble insertions. This keeps memory allocation constant at **66 KB** regardless of the dataset size (previously up to **4.33 MB**), drastically reducing GC overhead.
- Outliers (first cold execution) are not representative of production performance; the engine stays warm in memory.

## Key Improvements in This Version

- Weight normalization during `LoadData` (0 ≤ weight ≤ 1)  
- Three fuzzy score modes: `Linear` (default), `Exponential`, `Inverse`  
- Optional exact‑match bonus (`PrioritizeExactMatch` + `ExactMatchBonus`)  
- High‑performance reranking using struct comparers, ArrayPool, and Top-K selection  
- Better concurrency with `ReaderWriterLockSlim` on the cache  
- Reduced LINQ usage and stack‑allocated buffers for Levenshtein distance (with 0 heap allocations during tree traversal)  
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
