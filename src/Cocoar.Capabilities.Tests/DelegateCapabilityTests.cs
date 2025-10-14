using Xunit;

namespace Cocoar.Capabilities.Tests;

public class DelegateCapabilityTests
{
    [Fact]
    public void Add_ActionCapabilities_CanQueryAndInvoke()
    {
        // Arrange
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");
        var messages = new List<string>();

        Action<string> action1 = msg => messages.Add($"Action1: {msg}");
        Action<string> action2 = msg => messages.Add($"Action2: {msg}");
        Action<string> action3 = msg => messages.Add($"Action3: {msg}");

        // Act
        var composition = scope.For(subject)
            .Add(action1)
            .Add(action2)
            .Add(action3)
            .Build();

        var actions = composition.GetAll<Action<string>>();
        
        foreach (var action in actions)
        {
            action("Hello");
        }

        // Assert
        Assert.Equal(3, actions.Count);
        Assert.Equal(3, messages.Count);
        Assert.Contains("Action1: Hello", messages);
        Assert.Contains("Action2: Hello", messages);
        Assert.Contains("Action3: Hello", messages);
    }

    [Fact]
    public void Add_FuncCapabilities_CanQueryAndInvoke()
    {
        // Arrange
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        Func<int, int> double_func = x => x * 2;
        Func<int, int> square = x => x * x;
        Func<int, int> addTen = x => x + 10;

        // Act
        var composition = scope.For(subject)
            .Add(double_func)
            .Add(square)
            .Add(addTen)
            .Build();

        var funcs = composition.GetAll<Func<int, int>>();
        var results = new List<int>();
        
        foreach (var func in funcs)
        {
            results.Add(func(5));
        }

        // Assert
        Assert.Equal(3, funcs.Count);
        Assert.Equal(3, results.Count);
        Assert.Contains(10, results);  // double: 5 * 2
        Assert.Contains(25, results);  // square: 5 * 5
        Assert.Contains(15, results);  // addTen: 5 + 10
    }

    [Fact]
    public void Add_MixedActionTypes_CanQueryBySpecificType()
    {
        // Arrange
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");
        var stringMessages = new List<string>();
        var intMessages = new List<int>();

        Action<string> stringAction = msg => stringMessages.Add(msg);
        Action<int> intAction = num => intMessages.Add(num);

        // Act
        var composition = scope.For(subject)
            .Add(stringAction)
            .Add(intAction)
            .Build();

        var stringActions = composition.GetAll<Action<string>>();
        var intActions = composition.GetAll<Action<int>>();

        foreach (var action in stringActions)
        {
            action("test");
        }

        foreach (var action in intActions)
        {
            action(42);
        }

        // Assert
        Assert.Single(stringActions);
        Assert.Single(intActions);
        Assert.Single(stringMessages);
        Assert.Equal("test", stringMessages[0]);
        Assert.Single(intMessages);
        Assert.Equal(42, intMessages[0]);
    }

    [Fact]
    public void Add_FuncWithDifferentSignatures_CanQueryBySpecificSignature()
    {
        // Arrange
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        Func<string, int> getLength = s => s.Length;
        Func<int, string> toString = i => i.ToString();
        Func<string, string> toUpper = s => s.ToUpper();

        // Act
        var composition = scope.For(subject)
            .Add(getLength)
            .Add(toString)
            .Add(toUpper)
            .Build();

        var stringToInt = composition.GetAll<Func<string, int>>();
        var intToString = composition.GetAll<Func<int, string>>();
        var stringToString = composition.GetAll<Func<string, string>>();

        // Assert
        Assert.Single(stringToInt);
        Assert.Single(intToString);
        Assert.Single(stringToString);

        Assert.Equal(5, stringToInt[0]("Hello"));
        Assert.Equal("42", intToString[0](42));
        Assert.Equal("HELLO", stringToString[0]("Hello"));
    }

    [Fact]
    public void Add_ActionCapabilitiesWithOrdering_InvokesInOrder()
    {
        // Arrange
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");
        var messages = new List<string>();

        Action<string> action1 = msg => messages.Add($"First: {msg}");
        Action<string> action2 = msg => messages.Add($"Second: {msg}");
        Action<string> action3 = msg => messages.Add($"Third: {msg}");

        // Act - Add in non-sequential order but with explicit ordering
        var composition = scope.For(subject)
            .Add(action2, order: 20)
            .Add(action1, order: 10)
            .Add(action3, order: 30)
            .Build();

        var actions = composition.GetAll<Action<string>>();
        
        foreach (var action in actions)
        {
            action("Test");
        }

        // Assert - Should be invoked in order
        Assert.Equal(3, messages.Count);
        Assert.Equal("First: Test", messages[0]);
        Assert.Equal("Second: Test", messages[1]);
        Assert.Equal("Third: Test", messages[2]);
    }

    [Fact]
    public void GetFirstOrDefault_WithMultipleActions_ReturnsFirst()
    {
        // Arrange
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");
        var invoked = false;

        Action<string> action1 = msg => invoked = true;
        Action<string> action2 = msg => { };

        // Act
        var composition = scope.For(subject)
            .Add(action1)
            .Add(action2)
            .Build();

        var firstAction = composition.GetFirstOrDefault<Action<string>>();
        firstAction?.Invoke("test");

        // Assert
        Assert.NotNull(firstAction);
        Assert.True(invoked);
    }

    [Fact]
    public void TryGetFirst_WithFuncCapabilities_ReturnsFirstFunc()
    {
        // Arrange
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");

        Func<int, int> first = x => x * 2;
        Func<int, int> second = x => x * 3;

        // Act
        var composition = scope.For(subject)
            .Add(first)
            .Add(second)
            .Build();

        var found = composition.TryGetFirst<Func<int, int>>(out var func);
        var result = func?.Invoke(5);

        // Assert
        Assert.True(found);
        Assert.NotNull(func);
        Assert.Equal(10, result); // First func: 5 * 2
    }

    [Fact]
    public void Add_ActionAsContract_CanQueryByBaseDelegate()
    {
        // Arrange
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");
        var invoked = false;

        Action<string> action = msg => invoked = true;

        // Act
        var composition = scope.For(subject)
            .AddAs<Action<string>>(action)
            .Build();

        var actions = composition.GetAll<Action<string>>();
        actions[0]("test");

        // Assert
        Assert.Single(actions);
        Assert.True(invoked);
    }

    [Fact]
    public void Add_ComplexDelegateScenario_ProcessingPipeline()
    {
        // Arrange
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("processor");

        // Build a processing pipeline using Func capabilities
        Func<string, string> trim = s => s.Trim();
        Func<string, string> toUpper = s => s.ToUpper();
        Func<string, string> addPrefix = s => $"PROCESSED: {s}";

        // Act
        var composition = scope.For(subject)
            .Add(trim, order: 10)
            .Add(toUpper, order: 20)
            .Add(addPrefix, order: 30)
            .Build();

        var pipeline = composition.GetAll<Func<string, string>>();
        
        // Execute pipeline
        var input = "  hello world  ";
        var result = input;
        foreach (var step in pipeline)
        {
            result = step(result);
        }

        // Assert
        Assert.Equal(3, pipeline.Count);
        Assert.Equal("PROCESSED: HELLO WORLD", result);
    }

    [Fact]
    public void Add_ValidatorFuncs_CanQueryAndValidate()
    {
        // Arrange
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("validation");

        Func<string, bool> notEmpty = s => !string.IsNullOrEmpty(s);
        Func<string, bool> hasMinLength = s => s.Length >= 3;
        Func<string, bool> hasMaxLength = s => s.Length <= 10;

        // Act
        var composition = scope.For(subject)
            .Add(notEmpty)
            .Add(hasMinLength)
            .Add(hasMaxLength)
            .Build();

        var validators = composition.GetAll<Func<string, bool>>();
        
        var testValue = "test";
        var allValid = validators.All(validator => validator(testValue));

        var emptyValue = "";
        var allValidForEmpty = validators.All(validator => validator(emptyValue));

        // Assert
        Assert.Equal(3, validators.Count);
        Assert.True(allValid);
        Assert.False(allValidForEmpty);
    }

    [Fact]
    public void TryAdd_DuplicateAction_OnlyAddsOnce()
    {
        // Arrange
        using var scope = new CapabilityScope(TestOptions.Disabled);
        var subject = new StringSubject("test");
        var counter = 0;

        Action<string> action = msg => counter++;

        // Act
        var composition = scope.For(subject)
            .Add(action)
            .TryAdd(action) // Should not add duplicate
            .Build();

        var actions = composition.GetAll<Action<string>>();
        
        foreach (var a in actions)
        {
            a("test");
        }

        // Assert
        Assert.Single(actions); // Only one action should be added
        Assert.Equal(1, counter); // Should only be invoked once
    }
}
