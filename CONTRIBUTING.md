# Contributing

Thanks for your interest in contributing to Cocoar.Capabilities!

## Getting started
- Fork the repository and create a feature branch.
- Use .NET 9 SDK (9.x). You can verify with `dotnet --info`.
- Run the test suite locally before opening a PR.

## Coding guidelines
- Prefer small, focused PRs.
- Keep public APIs stable; consider extension methods for additive APIs.
- Performance matters: avoid allocations in hot paths, avoid LINQ in core loops, prefer arrays/spans where it helps, and keep `CapabilityBag` immutable/thread-safe.
- Add or update tests for all behavior changes.

## Testing
- Run unit tests under `src/Cocoar.Capabilities.Tests`.
- Optional: run benchmarks in `src/Cocoar.Capabilities.Benchmarks` to validate perf changes.

## Commit/PR
- Reference related issues in the PR description.
- Describe user-facing changes and migration notes if any.

## License
By contributing, you agree that your contributions will be licensed under the Apache-2.0 License.