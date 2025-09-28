Test Catalog
============

Purpose: High-level overview of existing test coverage. Each entry: Method Name -> Scenario Description.

BasicCompositionTests
---------------------
- Build_WithRegistryDisabled_CompositionNotRegistered: Building a composition when both registries are disabled should not register the composition.
- Build_WithOverrideEnablesRegistry_CompositionRegistered: Explicit override enables registration even if scope defaults disable registries; composition is retrievable and identical.
- AddAs_WithTupleContracts_RegistersUnderBothContracts: A capability registered via tuple contracts appears under each contract and refers to the same instance.
- Build_WithMultiplePrimaryCapabilities_Throws: Attempting to add two primary capabilities results in an exception enforcing uniqueness.

Planned (Not Yet Implemented)
-----------------------------
- PrimaryCapability_Accessors_Work: Verify TryGetPrimary / GetPrimary / GetPrimaryOrDefault semantics.
- PrimaryCapability_GenericCasting_Works: Casting primary to requested subtype succeeds and throws when absent.
- OrderedCapabilities_AreSortedByOrderThenInsertion: Ensure ordering logic stable and deterministic.
- Registry_ComposerToComposition_Transition_Succeeds: Composer registered then replaced by composition on Build.
- ValueTypeSubject_StrongStorage_TracksCount: Value type compositions persist and count updates on removal.
- Recompose_PreservesCompositionIdentity: Rebuilding from existing composition updates in-place.
- ContractQuery_ExcludesContractOnlyInstances_WhenNotContractSearch: Ensure contract-only instances not leaked incorrectly.

Legacy Scenario Categories (Reference Only)
-----------------------------------------
These derive from the old static-registry design (no CapabilityScope). They guide parity goals without copying legacy test code.

1. Registration
	- Build + register (ref vs value types) => Present in registry.
2. Lookup APIs
	- FindOrDefault / FindRequired / TryFind (generic & non-generic).
3. Removal
	- Remove existing (ref & value) / removing absent returns false.
4. Value Type Storage Bookkeeping
	- Count increments/decrements; clear removes all; memory safety.
5. Custom Provider Behavior
	- Provider interception of register/remove (now potentially mapped to pluggable registry implementation or options).
6. Primary Capability Uniqueness
	- Multiple primary => exception; retrieval helpers.
7. Contract Queries
	- Capabilities registered under contract vs concrete; exclude contract-only where appropriate.
8. Tuple / Multi-Contract Registration
	- Single instance appears under each contract.
9. Ordered Capabilities
	- Order + stable tie-breaking by original insertion order.
10. Recomposition
	- Updating an existing composition (identity preservation) (applies to internal recompose path).
11. Transition Semantics
	- Composer -> composition replacement in registry.
12. Disposal & Resource Cleanup
	- Disposing scope / registry clears value type tracking & prevents further usage.
13. Error Conditions
	- Null subject, double Build(), Build after WithPrimary etc.
14. Idempotency / Safety
	- Repeated lookups do not mutate state.

New Architecture Mapping
------------------------
Old Static API | New Scoped API / Behavior
-------------- | -------------------------
Composer.For(subject).BuildAndRegister() | scope.For(subject).Add(...).Build(useRegistry: true)
Composition.FindOrDefault(subject)       | scope.Compositions.FindOrDefault(subject)
Composition.FindRequired(subject)        | scope.Compositions.FindRequired(subject) (to implement if needed)
Composition.TryFind(subject, out comp)   | scope.Compositions.TryFind(subject, out comp)
Composition.Remove(subject)              | scope.Compositions.Remove(subject)
CompositionRegistryConfiguration.*       | Provided via CapabilityScopeOptions & internal registries

Prioritization Proposal (Incremental Batches)
---------------------------------------------
Batch 1 (done): Basic build, registry override, tuple contracts, primary uniqueness.
Batch 2: Primary capability accessor semantics + ordered capabilities.
Batch 3: Registry transition (composer->composition) + value type bookkeeping.
Batch 4: Recomposition identity + contract-only filtering.
Batch 5: Negative/error cases (double build, multiple adds after build, null args) + disposal behavior.
Batch 6: (Optional) Custom registry/provider plug-in tests if abstraction stabilized.

Open Questions / TBD
--------------------
- Do we expose a public 'FindRequired' equivalent? (If yes, add tests.)
- Is recomposition an intended public scenario or internal only? (Affects test surface.)
- Should contract-only capabilities be tracked distinctly to exclude from non-contract queries? (Clarify semantics.)

Action Needed
-------------
Select next batch (e.g., "Batch 2") to proceed with concrete test implementation.

Keep this list updated as you add new tests to maintain clarity on coverage and gaps.
