using Cocoar.Capabilities.Core;

namespace Cocoar.Capabilities.Tests;

/// <summary>
/// Tests for the composition registry functionality.
/// These tests focus on registry-specific behavior: registration, lookup, removal, and GC.
/// </summary>
public class RegistryTests : IDisposable
{
    private sealed class Subject { }

    private sealed class DemoCapability : ICapability<Subject> { }

    private sealed class TestSubject(string name)
    {
        public string Name { get; } = name;
    }

    private sealed class TestCapability(string value) : ICapability<TestSubject>
    {
        public string Value { get; } = value;
    }

    private sealed class ValueCapability(int value) : ICapability<int>
    {
        public int Value { get; } = value;
    }

    private sealed class TestProvider : ICompositionRegistryProvider
    {
        public readonly Dictionary<object, IComposition> Items = new();

        public void Register(object subject, IComposition composition) => Items[subject] = composition;
        public bool TryGet(object subject, out IComposition composition) => Items.TryGetValue(subject, out composition!);
        public bool Remove(object subject) => Items.Remove(subject);
    }

    public void Dispose()
    {
        CompositionRegistryConfiguration.ClearValueTypes();
        GC.SuppressFinalize(this);
    }

    #region BuildAndRegister Tests

    [Fact]
    public void BuildAndRegister_WithReferenceType_RegistersInWeakTable()
    {
        
        var subject = new TestSubject("test");
        var composer = Composer.For(subject).Add(new TestCapability("data"));

        
        var composition = composer.BuildAndRegister();

        
        Assert.True(Composition.TryFind(subject, out var found));
        Assert.Same(composition, found);
        Assert.Equal("data", found.GetAll<TestCapability>()[0].Value);
    }

    [Fact]
    public void BuildAndRegister_WithValueType_RegistersInStrongStorage()
    {
        
        var subject = 42;
        var composer = Composer.For(subject).Add(new ValueCapability(100));

        
        var composition = composer.BuildAndRegister();

        
        Assert.True(Composition.TryFind(subject, out var found));
        Assert.Same(composition, found);
        Assert.Equal(100, found.GetAll<ValueCapability>()[0].Value);
        
        Assert.Equal(1, CompositionRegistryConfiguration.ValueTypeCount);
    }

    #endregion

    #region FindOrDefault Tests

    [Fact]
    public void FindOrDefault_WithRegisteredSubject_ReturnsComposition()
    {
        
        var subject = new TestSubject("test");
        var expected = Composer.For(subject).Add(new TestCapability("data")).BuildAndRegister();

        
        var found = Composition.FindOrDefault(subject);

        
        Assert.Same(expected, found);
    }

    [Fact]
    public void FindOrDefault_WithUnregisteredSubject_ReturnsNull()
    {
        
        var subject = new TestSubject("test");

        
        var found = Composition.FindOrDefault(subject);

        
        Assert.Null(found);
    }

    [Fact]
    public void FindOrDefault_NonGeneric_WithRegisteredSubject_ReturnsComposition()
    {
        
        var subject = new TestSubject("test");
        var expected = Composer.For(subject).Add(new TestCapability("data")).BuildAndRegister();

        
        var found = Composition.FindOrDefault((object)subject);

        
        Assert.Same(expected, found);
    }

    [Fact]
    public void FindOrDefault_NonGeneric_WithUnregisteredSubject_ReturnsNull()
    {
        
        var subject = new TestSubject("test");

        
        var found = Composition.FindOrDefault((object)subject);

        
        Assert.Null(found);
    }

    #endregion

    #region FindRequired Tests

    [Fact]
    public void FindRequired_WithRegisteredSubject_ReturnsComposition()
    {
        
        var subject = new TestSubject("test");
        var expected = Composer.For(subject).Add(new TestCapability("data")).BuildAndRegister();

        
        var found = Composition.FindRequired(subject);

        
        Assert.Same(expected, found);
    }

    [Fact]
    public void FindRequired_WithUnregisteredSubject_ThrowsException()
    {
        
        var subject = new TestSubject("test");

        
        var ex = Assert.Throws<InvalidOperationException>(() => Composition.FindRequired(subject));
        Assert.Contains("TestSubject", ex.Message);
    }

    [Fact]
    public void FindRequired_NonGeneric_WithRegisteredSubject_ReturnsComposition()
    {
        
        var subject = new TestSubject("test");
        var expected = Composer.For(subject).Add(new TestCapability("data")).BuildAndRegister();

        
        var found = Composition.FindRequired((object)subject);

        
        Assert.Same(expected, found);
    }

    [Fact]
    public void FindRequired_NonGeneric_WithUnregisteredSubject_ThrowsException()
    {
        
        var subject = new TestSubject("test");

        
        var ex = Assert.Throws<InvalidOperationException>(() => Composition.FindRequired((object)subject));
        Assert.Contains("TestSubject", ex.Message);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void Remove_WithRegisteredReferenceType_RemovesFromRegistry()
    {
        
        var subject = new TestSubject("test");
        var composition = Composer.For(subject).Add(new TestCapability("data")).BuildAndRegister();
        
        Assert.True(Composition.TryFind(subject, out var found));
        Assert.Same(composition, found);

        
        var removed = Composition.Remove(subject);

        
        Assert.True(removed);
        Assert.False(Composition.TryFind(subject, out _));
    }

    [Fact]
    public void Remove_WithRegisteredValueType_RemovesFromRegistry()
    {
        
        var subject = 42;
        var composition = Composer.For(subject).Add(new ValueCapability(100)).BuildAndRegister();
        
        Assert.True(Composition.TryFind(subject, out var found));
        Assert.Same(composition, found);
        Assert.Equal(1, CompositionRegistryConfiguration.ValueTypeCount);

        
        var removed = Composition.Remove(subject);

        
        Assert.True(removed);
        Assert.False(Composition.TryFind(subject, out _));
        Assert.Equal(0, CompositionRegistryConfiguration.ValueTypeCount);
    }

    [Fact]
    public void Remove_WithUnregisteredSubject_ReturnsFalse()
    {
        
        var subject = new TestSubject("test");

        
        var removed = Composition.Remove(subject);

        
        Assert.False(removed);
    }

    [Fact]
    public void Remove_NonGeneric_WithRegisteredSubject_RemovesFromRegistry()
    {
        
        var subject = new TestSubject("test");
        var composition = Composer.For(subject).Add(new TestCapability("data")).BuildAndRegister();
        
        Assert.True(Composition.TryFind(subject, out var found));
        Assert.Same(composition, found);

        
        var removed = Composition.Remove((object)subject);

        
        Assert.True(removed);
        Assert.False(Composition.TryFind(subject, out _));
    }

    #endregion

    #region Custom Provider Tests

    [Fact]
    public void CustomProvider_IsUsedForRegistrationAndLookup()
    {
        
        var provider = new TestProvider();
        var original = CompositionRegistryConfiguration.Provider;
        var subject = new TestSubject("test");

        try
        {
            CompositionRegistryConfiguration.Provider = provider;
            
            
            var composition = Composer.For(subject).Add(new TestCapability("data")).BuildAndRegister();

            
            Assert.True(provider.Items.ContainsKey(subject));
            Assert.Same(composition, provider.Items[subject]);
            
            Assert.True(Composition.TryFind(subject, out var found));
            Assert.Same(composition, found);
        }
        finally
        {
            CompositionRegistryConfiguration.Provider = original;
        }
    }

    [Fact]
    public void CustomProvider_RemovalWorks()
    {
        
        var provider = new TestProvider();
        var original = CompositionRegistryConfiguration.Provider;
        var subject = new TestSubject("test");

        try
        {
            CompositionRegistryConfiguration.Provider = provider;
            var composition = Composer.For(subject).Add(new TestCapability("data")).BuildAndRegister();
            
            Assert.True(Composition.TryFind(subject, out _));

            
            var removed = Composition.Remove(subject);

            
            Assert.True(removed);
            Assert.False(provider.Items.ContainsKey(subject));
            Assert.False(Composition.TryFind(subject, out _));
        }
        finally
        {
            CompositionRegistryConfiguration.Provider = original;
        }
    }

    #endregion

    #region Value Type Storage Tests

    [Fact]
    public void ValueTypeStorage_ClearValueTypes_RemovesAllValueTypes()
    {
        
        var subject1 = 42;
        var subject2 = 84;
        Composer.For(subject1).Add(new ValueCapability(100)).BuildAndRegister();
        Composer.For(subject2).Add(new ValueCapability(200)).BuildAndRegister();
        
        Assert.Equal(2, CompositionRegistryConfiguration.ValueTypeCount);
        Assert.True(Composition.TryFind(subject1, out _));
        Assert.True(Composition.TryFind(subject2, out _));

        
        CompositionRegistryConfiguration.ClearValueTypes();

        
        Assert.Equal(0, CompositionRegistryConfiguration.ValueTypeCount);
        Assert.False(Composition.TryFind(subject1, out _));
        Assert.False(Composition.TryFind(subject2, out _));
    }

    [Fact]
    public void ValueTypeStorage_CountReflectsCurrentState()
    {
        Assert.Equal(0, CompositionRegistryConfiguration.ValueTypeCount);

        var subject1 = 42;
        var subject2 = 84;
        Composer.For(subject1).Add(new ValueCapability(100)).BuildAndRegister();
        Assert.Equal(1, CompositionRegistryConfiguration.ValueTypeCount);
        
        Composer.For(subject2).Add(new ValueCapability(200)).BuildAndRegister();
        Assert.Equal(2, CompositionRegistryConfiguration.ValueTypeCount);

        Composition.Remove(subject1);
        Assert.Equal(1, CompositionRegistryConfiguration.ValueTypeCount);

        Composition.Remove(subject2);
        Assert.Equal(0, CompositionRegistryConfiguration.ValueTypeCount);
    }

    #endregion

    #region TryFind Tests

    [Fact]
    public void TryFind_WithRegisteredSubject_ReturnsTrue()
    {
        
        var subject = new TestSubject("test");
        var expected = Composer.For(subject).Add(new TestCapability("data")).BuildAndRegister();

        
        var result = Composition.TryFind(subject, out var found);

        
        Assert.True(result);
        Assert.Same(expected, found);
    }

    [Fact]
    public void TryFind_WithUnregisteredSubject_ReturnsFalse()
    {
        
        var subject = new TestSubject("test");

        
        var result = Composition.TryFind(subject, out var found);

        
        Assert.False(result);
        Assert.Null(found);
    }

    [Fact]
    public void TryFind_NonGeneric_WithRegisteredSubject_ReturnsTrue()
    {
        
        var subject = new TestSubject("test");
        var expected = Composer.For(subject).Add(new TestCapability("data")).BuildAndRegister();

        
        var result = Composition.TryFind((object)subject, out IComposition found);

        
        Assert.True(result);
        Assert.Same(expected, found);
    }

    [Fact]
    public void TryFind_NonGeneric_WithUnregisteredSubject_ReturnsFalse()
    {
        
        var subject = new TestSubject("test");

        
        var result = Composition.TryFind((object)subject, out IComposition found);

        
        Assert.False(result);
        Assert.Null(found);
    }

    #endregion
}