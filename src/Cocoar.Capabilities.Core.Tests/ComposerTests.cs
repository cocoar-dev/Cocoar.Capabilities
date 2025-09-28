namespace Cocoar.Capabilities.Core.Tests;

public class ComposerTests
{
    [Fact]
    public void Build_CalledTwice_ThrowsInvalidOperation()
    {
        
        var subject = new TestSubject();
        var builder = Composer.For(subject).Add(new TestCapability("test"));
        
        
        var composition1 = builder.Build();
        
        
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("Build() can only be called once", ex.Message);
    }

    [Fact]
    public void Builder_AfterBuild_IsUnusable()
    {
        
        var subject = new TestSubject();
        var builder = Composer.For(subject);
        var composition = builder.Build();
        
        
        Assert.Throws<InvalidOperationException>(() => 
            builder.Add(new TestCapability("test")));
            
        Assert.Throws<InvalidOperationException>(() => 
            builder.AddAs<ITestContract>(new ConcreteTestCapability("test")));
    }

    [Fact]
    public void Add_NullCapability_ThrowsArgumentNull()
    {
        
        var subject = new TestSubject();
        var builder = Composer.For(subject);

        
        Assert.Throws<ArgumentNullException>(() => 
            builder.Add(null!));
    }

    [Fact]
    public void AddAs_NullCapability_ThrowsArgumentNull()
    {
        
        var subject = new TestSubject();
        var builder = Composer.For(subject);

        
        Assert.Throws<ArgumentNullException>(() => 
            builder.AddAs<ITestContract>(null!));
    }

    [Fact]
    public void AddAs_ContractRetrieval_WorksCorrectly()
    {
        
        var subject = new TestSubject();
        var concreteCap = new ConcreteTestCapability("contract-test");
        
        var composition = Composer.For(subject)
            .AddAs<ITestContract>(concreteCap)
            .Build();

        
        var success = composition.TryGet<ITestContract>(out var result);

        
        Assert.True(success);
        Assert.Equal(concreteCap, result);
        Assert.Equal("contract-test", result.GetValue());
    }

    [Fact]
    public void AddAs_ExactTypeMatching_OnlyFindsContractType()
    {
        
        var subject = new TestSubject();
        var concreteCap = new ConcreteTestCapability("test");
        
        var composition = Composer.For(subject)
            .AddAs<ITestContract>(concreteCap)
            .Build();

        
        Assert.True(composition.TryGet<ITestContract>(out _));
        Assert.False(composition.TryGet<ConcreteTestCapability>(out _));
    }

    [Fact]
    public void Subject_Property_ReturnsCorrectSubject()
    {
        
        var subject = new TestSubject { Name = "Builder Test" };
        var builder = Composer.For(subject);

        
        Assert.Equal(subject, builder.Subject);
        Assert.Equal("Builder Test", builder.Subject.Name);
    }

    // ===== MISSING COVERAGE TESTS =====

    [Fact]
    public void Add_AfterBuild_ThrowsInvalidOperation()
    {
        
        var subject = new TestSubject();
        var builder = Composer.For(subject);
        builder.Build();

        
        var ex = Assert.Throws<InvalidOperationException>(() => 
            builder.Add(new TestCapability("test")));
        Assert.Contains("Build() has already been called", ex.Message);
    }

    [Fact]
    public void RemoveWhere_AfterBuild_ThrowsInvalidOperation()
    {
        
        var subject = new TestSubject();
        var builder = Composer.For(subject);
        builder.Build();

        
        var ex = Assert.Throws<InvalidOperationException>(() => 
            builder.RemoveWhere(_ => true));
        Assert.Contains("Build() has already been called", ex.Message);
    }

    [Fact]
    public void WithPrimary_MultiplePrimaries_ReplacesExistingPrimary()
    {
        
        var subject = new TestSubject();
        var primary1 = new PrimaryTestCapability("primary1");
        var primary2 = new PrimaryTestCapability("primary2");
        var builder = Composer.For(subject)
            .WithPrimary(primary1);

        
        builder.WithPrimary(primary2);
        var result = builder.Build();

        
        Assert.True(result.HasPrimary());
        var primary = result.GetPrimary();
        Assert.Equal(primary2, primary);
        Assert.Equal("primary2", ((PrimaryTestCapability)primary).Value);
    }

    [Fact]
    public void AddAs_InvalidContractType_ThrowsArgumentException()
    {
        
        var subject = new TestSubject();
        var builder = Composer.For(subject);
        var capability = new TestCapability("test");

        
        var ex = Assert.Throws<ArgumentException>(() => 
            builder.AddAs<string>(capability));
        Assert.Contains("must implement ICapability", ex.Message);
    }

    [Fact]
    public void WithPrimary_ReplaceExistingPrimary_UpdatesPrimary()
    {
        
        var subject = new TestSubject();
        var primaryCap = new PrimaryTestCapability("existing-primary");
        var builder = Composer.For(subject)
            .WithPrimary(primaryCap);

        var anotherPrimary = new PrimaryTestCapability("new-primary");

        
        builder.WithPrimary(anotherPrimary);
        var result = builder.Build();

        
        Assert.True(result.HasPrimary());
        var primary = result.GetPrimary();
        Assert.Equal(anotherPrimary, primary);
        Assert.Equal("new-primary", ((PrimaryTestCapability)primary).Value);
    }

    [Fact]
    public void WithPrimary_InvalidBranchCoverage_SeedFromComposition()
    {
        
        var subject = new TestSubject();
        var existingComposition = Composer.For(subject)
            .Add(new TestCapability("existing"))
            .Build();

        
        var builder = Composer.Recompose(existingComposition)
            .WithPrimary(new PrimaryTestCapability("primary"));

        var result = builder.Build();

        
        Assert.True(result.HasPrimary());
        Assert.True(result.Has<TestCapability>());
    }

    [Fact]
    public void RemoveWhere_ComplexScenarios_ImprovesCoverage()
    {
        
        var subject = new TestSubject();
        var builder = Composer.For(subject)
            .Add(new TestCapability("keep"))
            .Add(new TestCapability("remove"))
            .Add(new AnotherTestCapability(1))
            .Add(new AnotherTestCapability(2));

        
        builder.RemoveWhere(cap => cap is TestCapability tc && tc.Value == "remove");
        builder.RemoveWhere(cap => cap is AnotherTestCapability ac && ac.Number == 2);

        var result = builder.Build();

        
        var testCaps = result.GetAll<TestCapability>();
        var numberCaps = result.GetAll<AnotherTestCapability>();
        
        Assert.Single(testCaps);
        Assert.Equal("keep", testCaps[0].Value);
        Assert.Single(numberCaps);
        Assert.Equal(1, numberCaps[0].Number);
    }

    [Fact]
    public void Build_EdgeCases_ImprovesBranchCoverage()
    {
        
        var subject = new TestSubject();
        
        var emptyResult = Composer.For(subject).Build();
        Assert.Equal(0, emptyResult.TotalCapabilityCount);

        var primaryOnlyResult = Composer.For(subject)
            .WithPrimary(new PrimaryTestCapability("primary"))
            .Build();
        Assert.Equal(1, primaryOnlyResult.TotalCapabilityCount);
        Assert.True(primaryOnlyResult.HasPrimary());
        var mixedResult = Composer.For(subject)
            .Add(new TestCapability("regular"))
            .AddAs<ITestContract>(new ConcreteTestCapability("contract"))
            .WithPrimary(new PrimaryTestCapability("primary"))
            .Build();
        Assert.Equal(3, mixedResult.TotalCapabilityCount);
        Assert.True(mixedResult.HasPrimary());
    }
    [Fact]
    public void WithPrimary_ReplaceExistingPrimary_CoverRemoveExistingPrimaryPath()
    {
        // This test specifically targets the RemoveExistingPrimary() method
        // which is currently showing as uncovered in coverage reports
        var subject = new TestSubject();
        var originalPrimary = new PrimaryTestCapability("original");
        var newPrimary = new PrimaryTestCapability("replacement");

        var builder = Composer.For(subject)
            .Add(new TestCapability("test"))
            .WithPrimary(originalPrimary);

        // This should trigger RemoveExistingPrimary() internally
        builder.WithPrimary(newPrimary);
        var result = builder.Build();

        Assert.True(result.HasPrimary());
        var primary = result.GetPrimary();
        Assert.Equal(newPrimary, primary);
        Assert.Equal("replacement", ((PrimaryTestCapability)primary).Value);
        
        // Ensure we have both the regular capability and the primary
        Assert.Equal(2, result.TotalCapabilityCount);
    }

    [Fact]
    public void WithPrimary_AfterBuild_ThrowsInvalidOperation()
    {
        // This test specifically covers the _built check in WithPrimary()
        // which is showing strange coverage results
        var subject = new TestSubject();
        var builder = Composer.For(subject);
        builder.Build();

        var primary = new PrimaryTestCapability("test-primary");

        
        var ex = Assert.Throws<InvalidOperationException>(() => 
            builder.WithPrimary(primary));
        Assert.Contains("Build() has already been called", ex.Message);
    }

    // ==== MISSING COVERAGE TESTS ====
    
    [Fact]
    public void Composition_GetPrimaryOrDefault_WhenNoPrimary_ReturnsNull()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .Add(new TestCapability("regular"))
            .Build();

        
        var primary = composition.GetPrimaryOrDefault();

        
        Assert.Null(primary);
    }

    [Fact]
    public void Composition_TryGetPrimary_WhenNoPrimary_ReturnsFalse()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .Add(new TestCapability("regular"))
            .Build();

        
        var result = composition.TryGetPrimary(out var primary);

        
        Assert.False(result);
        Assert.Null(primary);
    }

    [Fact]
    public void Composition_GetPrimary_WhenNoPrimary_ThrowsInvalidOperation()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .Add(new TestCapability("regular"))
            .Build();

        
        var ex = Assert.Throws<InvalidOperationException>(() => composition.GetPrimary());
        Assert.Contains("Primary capability not found", ex.Message);
    }

    [Fact]
    public void Composition_TryGetPrimaryAs_WhenNoPrimary_ReturnsFalse()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .Add(new TestCapability("regular"))
            .Build();

        
        var result = composition.TryGetPrimaryAs<PrimaryTestCapability>(out var primary);

        
        Assert.False(result);
        Assert.Null(primary);
    }

    [Fact]
    public void Composition_TryGetPrimaryAs_WhenPrimaryIsWrongType_ReturnsFalse()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .WithPrimary(new PrimaryTestCapability("primary"))
            .Build();

        
        var result = composition.TryGetPrimaryAs<SecondPrimaryTestCapability>(out var primary);

        
        Assert.False(result);
        Assert.Null(primary);
    }

    [Fact]
    public void Composition_TryGetPrimaryAs_WhenPrimaryIsCorrectType_ReturnsTrue()
    {
        
        var subject = new TestSubject();
        var primaryCap = new PrimaryTestCapability("primary");
        var composition = Composer.For(subject)
            .WithPrimary(primaryCap)
            .Build();

        
        var result = composition.TryGetPrimaryAs<PrimaryTestCapability>(out var primary);

        
        Assert.True(result);
        Assert.Equal(primaryCap, primary);
    }

    [Fact]
    public void Composition_GetPrimaryOrDefaultAs_WhenNoPrimary_ReturnsNull()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .Add(new TestCapability("regular"))
            .Build();

        
        var primary = composition.GetPrimaryOrDefaultAs<PrimaryTestCapability>();

        
        Assert.Null(primary);
    }

    [Fact]
    public void Composition_GetPrimaryOrDefaultAs_WhenPrimaryIsWrongType_ReturnsNull()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .WithPrimary(new PrimaryTestCapability("primary"))
            .Build();

        
        var primary = composition.GetPrimaryOrDefaultAs<SecondPrimaryTestCapability>();

        
        Assert.Null(primary);
    }

    [Fact]
    public void Composition_GetPrimaryOrDefaultAs_WhenPrimaryIsCorrectType_ReturnsPrimary()
    {
        
        var subject = new TestSubject();
        var primaryCap = new PrimaryTestCapability("primary");
        var composition = Composer.For(subject)
            .WithPrimary(primaryCap)
            .Build();

        
        var primary = composition.GetPrimaryOrDefaultAs<PrimaryTestCapability>();

        
        Assert.Equal(primaryCap, primary);
    }

    [Fact]
    public void Composition_GetRequiredPrimaryAs_WhenNoPrimary_ThrowsInvalidOperation()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .Add(new TestCapability("regular"))
            .Build();

        
        var ex = Assert.Throws<InvalidOperationException>(() => 
            composition.GetRequiredPrimaryAs<PrimaryTestCapability>());
        Assert.Contains("Primary capability of type 'PrimaryTestCapability' not found", ex.Message);
    }

    [Fact]
    public void Composition_GetRequiredPrimaryAs_WhenPrimaryIsWrongType_ThrowsInvalidOperation()
    {
        
        var subject = new TestSubject();
        var composition = Composer.For(subject)
            .WithPrimary(new PrimaryTestCapability("primary"))
            .Build();

        
        var ex = Assert.Throws<InvalidOperationException>(() => 
            composition.GetRequiredPrimaryAs<SecondPrimaryTestCapability>());
        Assert.Contains("Primary capability of type 'SecondPrimaryTestCapability' not found", ex.Message);
    }

    [Fact]
    public void Composition_GetRequiredPrimaryAs_WhenPrimaryIsCorrectType_ReturnsPrimary()
    {
        
        var subject = new TestSubject();
        var primaryCap = new PrimaryTestCapability("primary");
        var composition = Composer.For(subject)
            .WithPrimary(primaryCap)
            .Build();

        
        var primary = composition.GetRequiredPrimaryAs<PrimaryTestCapability>();

        
        Assert.Equal(primaryCap, primary);
    }

    // ==== PRIMARY CAPABILITY VALIDATION TESTS (TESTING ACTUAL BEHAVIOR) ====
    
    [Fact]
    public void Add_MultiplePrimaryCapabilities_ThrowsOnBuild()
    {
        
        var subject = new TestSubject();
        var firstPrimary = new PrimaryTestCapability("first");
        var secondPrimary = new SecondPrimaryTestCapability("second");
        
        var builder = Composer.For(subject)
            .Add(firstPrimary) 
            .Add(secondPrimary); // Add allows multiple, but Build() will catch it

        
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("Multiple primary capabilities registered for 'TestSubject'", ex.Message);
        Assert.Contains("Only one primary capability is allowed", ex.Message);
    }

    [Fact]
    public void AddAs_DuplicatePrimaryCapabilityViaMultipleContracts_ThrowsInvalidOperation()
    {
        
        var subject = new TestSubject();
        var firstPrimary = new PrimaryTestCapability("first");
        var secondPrimary = new SecondPrimaryTestCapability("second");
        
        var builder = Composer.For(subject)
            .WithPrimary(firstPrimary); // Add first primary via WithPrimary

        
        var ex = Assert.Throws<InvalidOperationException>(() => 
            builder.AddAs<(IPrimaryCapability<TestSubject>, ICapability<TestSubject>)>(secondPrimary));
        Assert.Contains("A primary capability is already set for 'TestSubject'", ex.Message);
        Assert.Contains("Only one primary capability is allowed", ex.Message);
    }

    [Fact]
    public void WithPrimary_AfterAddingPrimaryViaAdd_ReplacesExisting()
    {
        
        var subject = new TestSubject();
        var firstPrimary = new PrimaryTestCapability("first");
        var secondPrimary = new SecondPrimaryTestCapability("second");
        
        var builder = Composer.For(subject)
            .Add(firstPrimary); // Add primary via Add

        
        var composition = builder
            .WithPrimary(secondPrimary)
            .Build();

        
        Assert.True(composition.HasPrimary());
        var primary = composition.GetPrimary();
        Assert.Equal(secondPrimary, primary);
        
        var allPrimaries = composition.GetAll<IPrimaryCapability<TestSubject>>();
        Assert.Single(allPrimaries);
        Assert.Equal(secondPrimary, allPrimaries[0]);
    }

    [Fact]
    public void WithPrimary_WhenNoPrimaryExists_AddsNewPrimary()
    {
        
        var subject = new TestSubject();
        var primary = new PrimaryTestCapability("primary");
        
        
        var composition = Composer.For(subject)
            .WithPrimary(primary)
            .Build();

        
        Assert.True(composition.HasPrimary());
        Assert.Equal(primary, composition.GetPrimary());
    }

    [Fact]
    public void Recompose_WithIncompatibleComposition_ThrowsArgumentException()
    {
        
        var subject = new TestSubject();
        var mockComposition = new MockComposition(subject);

        
        var ex = Assert.Throws<ArgumentException>(() => 
            Composer.Recompose(mockComposition));
            
        Assert.Contains("Recompose only supports compositions created by this system", ex.Message);
        Assert.Equal("existingComposition", ex.ParamName);
    }
}

// ==== READONLYLISTEXTENSIONS TESTS ====

public class ReadOnlyListExtensionsTests
{
    [Fact]
    public void ForEach_WithValidListAndAction_CallsActionForEachItem()
    {
        
        var list = new List<string> { "item1", "item2", "item3" }.AsReadOnly();
        var calledItems = new List<string>();

        
        list.ForEach(item => calledItems.Add(item));

        
        Assert.Equal(3, calledItems.Count);
        Assert.Equal("item1", calledItems[0]);
        Assert.Equal("item2", calledItems[1]);
        Assert.Equal("item3", calledItems[2]);
    }

    [Fact]
    public void ForEach_WithEmptyList_DoesNotCallAction()
    {
        
        var list = new List<string>().AsReadOnly();
        var calledCount = 0;

        
        list.ForEach(_ => calledCount++);

        
        Assert.Equal(0, calledCount);
    }

    [Fact]
    public void ForEach_WithNullList_ThrowsArgumentNull()
    {
        
        IReadOnlyList<string> list = null!;

        
        Assert.Throws<ArgumentNullException>(() => 
            list.ForEach(item => { }));
    }

    [Fact]
    public void ForEach_WithNullAction_ThrowsArgumentNull()
    {
        
        var list = new List<string> { "item1" }.AsReadOnly();

        
        Assert.Throws<ArgumentNullException>(() => 
            list.ForEach(null!));
    }

    [Fact]
    public void AddAs_DuplicatePrimaryCapabilityViaSingleContract_ThrowsInvalidOperation()
    {
        
        var subject = new TestSubject();
        var firstPrimary = new PrimaryTestCapability("first");
        var secondPrimary = new SecondPrimaryTestCapability("second");
        
        var builder = Composer.For(subject)
            .WithPrimary(firstPrimary); // Add first primary via WithPrimary

        
        var ex = Assert.Throws<InvalidOperationException>(() => 
            builder.AddAs<IPrimaryCapability<TestSubject>>(secondPrimary)); // Single contract path
        Assert.Contains("A primary capability is already set for 'TestSubject'", ex.Message);
        Assert.Contains("Only one primary capability is allowed", ex.Message);
    }
}

public class TupleTypeExtractorTests
{
    [Fact]
    public void GetTupleTypes_WithNonGenericType_ReturnsSingleTypeArray()
    {
        
        var result = TupleTypeExtractor.GetTupleTypes<string>();
        
        
        Assert.Single(result);
        Assert.Equal(typeof(string), result[0]);
    }
    
    [Fact]
    public void GetTupleTypes_WithValueTuple_ReturnsGenericArguments()
    {
        
        var result = TupleTypeExtractor.GetTupleTypes<(string, int)>();
        
        
        Assert.Equal(2, result.Length);
        Assert.Equal(typeof(string), result[0]);
        Assert.Equal(typeof(int), result[1]);
    }
    
    [Fact]
    public void GetTupleTypes_WithValueTupleOfThreeTypes_ReturnsAllGenericArguments()
    {
        
        var result = TupleTypeExtractor.GetTupleTypes<(string, int, bool)>();
        
        
        Assert.Equal(3, result.Length);
        Assert.Equal(typeof(string), result[0]);
        Assert.Equal(typeof(int), result[1]);
        Assert.Equal(typeof(bool), result[2]);
    }
    
    [Fact]
    public void GetTupleTypes_WithNonTupleGenericType_ReturnsSingleTypeArray()
    {
        
        var result = TupleTypeExtractor.GetTupleTypes<List<string>>();
        
        
        Assert.Single(result);
        Assert.Equal(typeof(List<string>), result[0]);
    }
    
    [Fact]
    public void ValidateCapabilityTypes_WithValidCapabilityTypes_DoesNotThrow()
    {
        
        var types = new[] { typeof(TestCapability), typeof(PrimaryTestCapability) };
        
        
        var exception = Record.Exception(() => 
            TupleTypeExtractor.ValidateCapabilityTypes<TestSubject>(types));
        Assert.Null(exception);
    }
    
    [Fact]
    public void ValidateCapabilityTypes_WithInvalidCapabilityType_ThrowsArgumentException()
    {
        
        var types = new[] { typeof(string) }; // string doesn't implement ICapability<TestSubject>
        
        
        var ex = Assert.Throws<ArgumentException>(() => 
            TupleTypeExtractor.ValidateCapabilityTypes<TestSubject>(types));
        Assert.Contains("Type 'String' must implement ICapability<TestSubject>", ex.Message);
        Assert.Contains("to be registered as a capability contract", ex.Message);
    }
    
    [Fact]
    public void ValidateCapabilityTypes_WithMixedValidAndInvalidTypes_ThrowsArgumentException()
    {
        
        var types = new[] { typeof(TestCapability), typeof(int) }; // int doesn't implement ICapability<TestSubject>
        
        
        var ex = Assert.Throws<ArgumentException>(() => 
            TupleTypeExtractor.ValidateCapabilityTypes<TestSubject>(types));
        Assert.Contains("Type 'Int32' must implement ICapability<TestSubject>", ex.Message);
    }
    
    [Fact]
    public void GetTupleTypes_WithGenericTypeDefinition_ReturnsSingleTypeArray()
    {
        // This tests the case where IsGenericType is true but IsValueTupleType returns false
        
        var result = TupleTypeExtractor.GetTupleTypes<Dictionary<string, int>>();
        
        
        Assert.Single(result);
        Assert.Equal(typeof(Dictionary<string, int>), result[0]);
    }
    
    [Fact] 
    public void GetTupleTypes_WithNullableValueType_ReturnsSingleTypeArray()
    {
        // This tests another non-tuple generic type to ensure IsValueTupleType edge cases
        
        var result = TupleTypeExtractor.GetTupleTypes<int?>();
        
        
        Assert.Single(result);
        Assert.Equal(typeof(int?), result[0]);
    }
    
    [Fact]
    public void GetTupleTypes_WithArrayType_ReturnsSingleTypeArray()
    {
        // Array types are not generic type definitions, this should hit the IsGenericTypeDefinition == false branch
        
        var result = TupleTypeExtractor.GetTupleTypes<string[]>();
        
        
        Assert.Single(result);
        Assert.Equal(typeof(string[]), result[0]);
    }
}

// Test helper class for testing edge cases in TupleTypeExtractor
public class TupleTypeEdgeCaseTests
{
    [Fact]
    public void IsValueTupleType_WithGenericTypeDefinitionThatIsNotValueTuple_ReturnsFalse()
    {
        // This will test a generic type definition that is NOT a ValueTuple
        // to hit the branch where IsGenericTypeDefinition=true but StartsWith=false
        
        // Get the generic type definition of List<T>
        var listGenericDef = typeof(List<>);
        
        // Use reflection to call the private IsValueTupleType method
        var method = typeof(TupleTypeExtractor).GetMethod("IsValueTupleType", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(method);
        
        
        var result = (bool)method.Invoke(null, new object[] { listGenericDef })!;
        
        
        Assert.False(result);
    }
    
    [Fact]
    public void IsValueTupleType_WithTypeHavingNullFullName_ReturnsFalse()
    {
        // Some types can have null FullName (like generic type parameters in certain contexts)
        // Let's create a mock type with null FullName to test the ?. operator branch
        
        // Get a generic method's type parameter, which might have null FullName
        var method = typeof(TupleTypeEdgeCaseTests).GetMethod(nameof(GenericMethodForTesting), 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var genericParam = method.GetGenericArguments()[0]; // This is T
        
        // Use reflection to call the private IsValueTupleType method
        var isValueTupleMethod = typeof(TupleTypeExtractor).GetMethod("IsValueTupleType", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(isValueTupleMethod);
        
        
        var result = (bool)isValueTupleMethod.Invoke(null, new object[] { genericParam })!;
        
        
        Assert.False(result);
    }
    
    // Helper method to get a generic type parameter for testing (private to avoid xUnit warning)
    private void GenericMethodForTesting<T>() { }
}
