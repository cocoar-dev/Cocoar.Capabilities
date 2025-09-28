using Xunit;

// Disables parallel test execution because tests mutate shared global registries
// (CompositionRegistryCore & ComposerRegistryCore) for value type subjects.
// Parallel execution was causing nondeterministic failures in value type tests
// due to interleaved ClearValueTypes() calls.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
