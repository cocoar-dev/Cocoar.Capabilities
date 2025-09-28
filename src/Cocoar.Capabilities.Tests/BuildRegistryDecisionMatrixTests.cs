using Xunit;

namespace Cocoar.Capabilities.Tests;

public class BuildRegistryDecisionMatrixTests
{
    // Encode override tri-state: -1 => null, 0 => false, 1 => true
    public static IEnumerable<object[]> AllCombinations()
    {
        bool[] bools = [false, true];
        int[] tri = [-1, 0, 1];
        foreach (var scopeComposer in bools)
        foreach (var scopeComposition in bools)
        foreach (var compOv in tri)
        foreach (var compoOv in tri)
        {
            yield return new object[] { scopeComposer, scopeComposition, compOv, compoOv };
        }
    }

    [Theory]
    [MemberData(nameof(AllCombinations))]
    public void Matrix_Verify_All_Registry_Decisions(
        bool scopeComposerDefault,
        bool scopeCompositionDefault,
        int composerOverrideState,
        int compositionOverrideState)
    {
        // Arrange scope
        using var scope = new CapabilityScope(new CapabilityScopeOptions
        {
            UseComposerRegistry = scopeComposerDefault,
            UseCompositionRegistry = scopeCompositionDefault
        });

        var subject = new StringSubject($"sub-{scopeComposerDefault}-{scopeCompositionDefault}-{composerOverrideState}-{compositionOverrideState}");

        bool? composerOverride = composerOverrideState switch { -1 => null, 0 => false, 1 => true, _ => null };
        bool? compositionOverride = compositionOverrideState switch { -1 => null, 0 => false, 1 => true, _ => null };

        // Effective decisions
        bool effectiveComposer = composerOverride ?? scopeComposerDefault;
        bool effectiveComposition = compositionOverride ?? scopeCompositionDefault;

        // Create composer (registration may happen now)
        var composer = scope.For(subject, composerOverride);
        composer.Add(new TestCapability("T"));

        var preComposer = scope.Composers.GetOrDefault(subject);
        var preComposition = scope.Compositions.GetOrDefault(subject);

        Assert.Equal(effectiveComposer, preComposer != null);
        Assert.Null(preComposition); // Never registered before Build

        // Act: Build
        var composition = composer.Build(compositionOverride);

        var postComposer = scope.Composers.GetOrDefault(subject);
        var postComposition = scope.Compositions.GetOrDefault(subject);

        // Assert post-build according to matrix
        if (!effectiveComposer && !effectiveComposition)
        {
            Assert.Null(postComposer);
            Assert.Null(postComposition);
        }
        else if (!effectiveComposer && effectiveComposition)
        {
            Assert.Null(postComposer);
            Assert.NotNull(postComposition); // direct registration
            Assert.Same(composition, postComposition);
        }
        else if (effectiveComposer && !effectiveComposition)
        {
            // Composer should have been removed; no composition registered
            Assert.Null(postComposer);
            Assert.Null(postComposition);
        }
        else // effectiveComposer && effectiveComposition
        {
            // Transition path
            Assert.Null(postComposer); // removed
            Assert.NotNull(postComposition);
            Assert.Same(composition, postComposition);
        }

        // Composition object should always reflect subject
        Assert.Same(subject, composition.Subject);
    }
}
