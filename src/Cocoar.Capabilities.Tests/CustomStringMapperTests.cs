using Xunit;

namespace Cocoar.Capabilities.Tests;

public sealed class CustomStringMapperTests
{
    private sealed class CaseInsensitiveStringMapper : ISubjectKeyMapper
    {
        public bool CanHandle(Type subjectType) => subjectType == typeof(string);
        public object Map(object subject)
        {
            var s = (string)subject;
            return new StringSubjectKey(s.Trim().ToUpperInvariant());
        }
    }

    [Fact]
    public void CustomStringMapper_AppliedPerScope_CanonicalizesConsistently()
    {
        using var scope = new CapabilityScope(new CapabilityScopeOptions
        {
            SubjectKeyMappers = new[] { new CaseInsensitiveStringMapper() }
        });

        var composer = scope.For(" hello ")
            .Add(new SimpleCapability())
            .Build();

        // Lookup using differently cased + spaced variant
        var found = scope.Compositions.FindOrDefault("HeLLo");
        Assert.NotNull(found);
        Assert.Same(composer, found);
    }

    [Fact]
    public void CustomStringMapper_SecondRegistrationIgnored()
    {
        var first = new CaseInsensitiveStringMapper();
        var second = new CaseInsensitiveStringMapper();
        using var scope = new CapabilityScope(new CapabilityScopeOptions
        {
            SubjectKeyMappers = new ISubjectKeyMapper[] { first, second }
        });

        var composer = scope.For("a")
            .Add(new SimpleCapability())
            .Build();

        var found = scope.Compositions.FindOrDefault("A");
        Assert.NotNull(found);
        Assert.Same(composer, found);
    }
    private sealed class SimpleCapability  { }
}
