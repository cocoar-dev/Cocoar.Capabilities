# Cocoar.Capabilities Benchmarks

This folder contains a small, decision-focused BenchmarkDotNet suite that measures:

- Build cost: Create a bag at tiny (~20) and larger (1000) sizes
- Lookups: Single-tag and multi-tag intersection on tiny and large bags
- Indexing strategies: TagIndexing None vs Auto vs Eager

## TL;DR guidance

- Tiny bags (~20 capabilities): Prefer `TagIndexing=None` (or `CapabilityBagBuildOptions.SmallBag`).
  - Build: ~9–10 µs; Lookup (single tag): ~120 ns
  - Eager is ~3× faster at lookup but costs more to build; only worth it if you’ll do ~80–100+ lookups per bag lifetime.
- Large bags (≥1000 capabilities): Prefer `TagIndexing=Eager` (Auto picks this by default).
  - Lookups: ~10× faster and near zero allocations; intersections benefit too.
- Default `Auto` (threshold 64) is safe and usually optimal. It picks None for tiny, Eager for large.

## How to run

From this `benchmarks` folder:

```pwsh
# Quick, lower-iteration run
dotnet run -c Release -- --job short

# List available benchmarks and their full names
dotnet run -c Release -- --list flat

# Run only lookups
dotnet run -c Release -- --job short --anyCategories Lookup

# Run a specific method (wildcards allowed)
dotnet run -c Release -- --job short --filter "*Lookup_Tiny_SingleTag_CountOnly*"
```

BenchmarkDotNet emits Markdown and HTML reports under `BenchmarkDotNet.Artifacts/results`.

## Reading results

- Focus on relative differences between `IndexMode` values; absolute times vary per machine.
- Build vs Lookup trade-off matters: Eager indexes speed up lookups but make builds slower and allocate more.
- Intersections: With current heuristics, Eager is not penalized and tends to help on larger sets.

## About hardware

Running on a consumer ARM Windows laptop (e.g., Snapdragon X Elite) is fine for comparative analysis. BenchmarkDotNet captures the exact environment in the report header (OS, runtime, CPU), so results are reproducible on the same machine. If you need cross-architecture validation, consider running the same suite on an x64 desktop/server and compare ratios.

## Configuration knobs

- `AutoIndexThreshold` (default 64): When `TagIndexing=Auto`, switch to Eager at or above this total capability count.
- `IndexMinFrequency` (default 2): Only build a tag index entry if at least this many capabilities share the tag (avoids indexing singletons).

## Caveats

- Short runs are great for quick checks; use the default job for more statistically robust results.
- Avoid running the full suite unnecessarily—it can take several minutes.

## Last updated

- Curated suite and intersection heuristics updated to reduce allocations and improve large-set intersections.
