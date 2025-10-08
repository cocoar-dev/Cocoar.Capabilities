using System.Reflection;

namespace Cocoar.Capabilities.Tests;

public class SubjectKeyCanonicalizerTests
{
    private sealed record Cap(string Name) ;

    private sealed class UppercaseStringMapper : ISubjectKeyMapper
    {
        public bool CanHandle(Type subjectType) => subjectType == typeof(string);
        public object Map(object subject) => new StringSubjectKey(((string)subject).ToUpperInvariant());
    }

    [Fact]
    public void Canonicalization_StringInstancesShareSameComposition()
    {
        using var scope = new CapabilityScope();
        var s1 = new string("alpha".ToCharArray());
        var s2 = new string("alpha".ToCharArray());
        var comp1 = scope.For(s1).Add(new Cap("A")).Build(useRegistry: true);
        var comp2 = scope.Compositions.FindRequired(s2);
        Assert.Same(comp1, comp2); // value semantics
    }

    [Fact]
    public void OverrideMapper_Applied_BeforeSealing()
    {
        var opts = new CapabilityScopeOptions
        {
            SubjectKeyMappers = new[] { new UppercaseStringMapper() }
        };
        using var scope = new CapabilityScope(opts);
        var lower = "mixedCase";
        scope.For(lower).Build(useRegistry: true); // triggers sealing & canonicalization

        // Reflection: fetch _sharedRegistry._canonicalizer and attempt TryRegisterOverride => false
        var scopeType = typeof(CapabilityScope);
        var registryField = scopeType.GetField("_sharedRegistry", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var registry = registryField.GetValue(scope)!;
        var canonField = registry.GetType().GetField("_canonicalizer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var canonicalizer = canonField.GetValue(registry)!;
        var tryRegister = canonicalizer.GetType().GetMethod("TryRegisterOverride", BindingFlags.Public | BindingFlags.Instance)!;
        var mapper = new UppercaseStringMapper();
        var result = (bool)tryRegister.Invoke(canonicalizer, new object[]{ mapper })!;
        Assert.False(result); // sealed after first Canonicalize
    }
}
