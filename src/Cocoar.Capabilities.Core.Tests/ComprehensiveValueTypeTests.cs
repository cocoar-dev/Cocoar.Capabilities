namespace Cocoar.Capabilities.Core.Tests;

public class ComprehensiveValueTypeTests
{
    // Test capabilities for different value types
    private class IntCapability(int subject) : ICapability<int>
    {
        public int Subject { get; } = subject;
        public string Description => $"Integer capability for {subject}";
    }
    
    private class DoubleCapability(double subject) : ICapability<double>
    {
        public double Subject { get; } = subject;
        public string Description => $"Double capability for {subject}";
    }
    
    private class BoolCapability(bool subject) : ICapability<bool>
    {
        public bool Subject { get; } = subject;
        public string Description => $"Boolean capability for {subject}";
    }
    
    private class CharCapability(char subject) : ICapability<char>
    {
        public char Subject { get; } = subject;
        public string Description => $"Character capability for '{subject}'";
    }
    
    private class DecimalCapability(decimal subject) : ICapability<decimal>
    {
        public decimal Subject { get; } = subject;
        public string Description => $"Decimal capability for {subject}";
    }
    
    private class DateTimeCapability(DateTime subject) : ICapability<DateTime>
    {
        public DateTime Subject { get; } = subject;
        public string Description => $"DateTime capability for {subject:yyyy-MM-dd HH:mm:ss}";
    }
    
    private class GuidCapability(Guid subject) : ICapability<Guid>
    {
        public Guid Subject { get; } = subject;
        public string Description => $"Guid capability for {subject}";
    }
    
    private enum TestEnum { First, Second, Third }
    
    private class EnumCapability(TestEnum subject) : ICapability<TestEnum>
    {
        public TestEnum Subject { get; } = subject;
        public string Description => $"Enum capability for {subject}";
    }
    
    private struct Point
    {
        public int X { get; init; }
        public int Y { get; init; }
        
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
        
        public override string ToString() => $"({X}, {Y})";
    }
    
    private class PointCapability(Point subject) : ICapability<Point>
    {
        public Point Subject { get; } = subject;
        public string Description => $"Point capability for {subject}";
    }
    
    private record struct PersonRecord(string Name, int Age);
    
    private class PersonRecordCapability(PersonRecord subject) : ICapability<PersonRecord>
    {
        public PersonRecord Subject { get; } = subject;
        public string Description => $"PersonRecord capability for {subject.Name}, {subject.Age}";
    }

    [Fact]
    public void IntegerValueTypes_CanComposeAndFind()
    {
        // Test different integer values
        var values = new[] { 0, 1, -1, 42, int.MaxValue, int.MinValue };
        
        foreach (var value in values)
        {
            
            var composer = Composer.For(value);
            composer.Add(new IntCapability(value));
            var composition = composer.Build();
            
            
            Assert.Equal(value, composition.Subject);
            Assert.Equal(1, composition.TotalCapabilityCount);
            
            var typedComposition = (IComposition<int>)composition;
            var capabilities = typedComposition.GetAll<IntCapability>();
            Assert.Single(capabilities);
            Assert.Equal(value, capabilities[0].Subject);
        }
    }

    [Fact]
    public void DoubleValueTypes_CanComposeAndFind()
    {
        var simpleValues = new[] { 0.0, 1.0, -1.0, 3.14159, double.MaxValue, double.MinValue };
        var specialValues = new[] { double.PositiveInfinity, double.NegativeInfinity };
        
        // Test simple values first
        foreach (var value in simpleValues)
        {
            var composer = Composer.For(value);
            composer.Add(new DoubleCapability(value));
            var composition = composer.Build();
            
            Assert.Equal(value, composition.Subject);
            
            
            var typedComposition = (IComposition<double>)composition;
            var capabilities = typedComposition.GetAll<DoubleCapability>();
            Assert.Single(capabilities);
            Assert.Equal(value, capabilities[0].Subject);
        }
        
        // Test special infinity values
        foreach (var value in specialValues)
        {
            var composer = Composer.For(value);
            composer.Add(new DoubleCapability(value));
            var composition = composer.Build();
            
            Assert.Equal(value, composition.Subject);
            
            
            var typedComposition = (IComposition<double>)composition;
            var capabilities = typedComposition.GetAll<DoubleCapability>();
            Assert.Single(capabilities);
            Assert.Equal(value, capabilities[0].Subject);
        }
        
        // Special case for NaN - it doesn't equal itself
        var nanValue = double.NaN;
        var nanComposer = Composer.For(nanValue);
        nanComposer.Add(new DoubleCapability(nanValue));
        var nanComposition = nanComposer.Build();
        
        Assert.True(double.IsNaN((double)nanComposition.Subject));
        
        var nanTypedComposition = (IComposition<double>)nanComposition;
        var nanCapabilities = nanTypedComposition.GetAll<DoubleCapability>();
        Assert.Single(nanCapabilities);
        Assert.True(double.IsNaN(nanCapabilities[0].Subject));
    }

    [Fact]
    public void BooleanValueTypes_CanComposeAndFind()
    {
        var values = new[] { true, false };
        
        foreach (var value in values)
        {
            var composer = Composer.For(value);
            composer.Add(new BoolCapability(value));
            var composition = composer.Build();
            
            Assert.Equal(value, composition.Subject);
            
            
            var typedComposition = (IComposition<bool>)composition;
            var capabilities = typedComposition.GetAll<BoolCapability>();
            Assert.Single(capabilities);
            Assert.Equal(value, capabilities[0].Subject);
        }
    }

    [Fact]
    public void CharacterValueTypes_CanComposeAndFind()
    {
        var values = new[] { 'a', 'Z', '0', '!', ' ', '\n', '\t', char.MinValue, char.MaxValue };
        
        foreach (var value in values)
        {
            var composer = Composer.For(value);
            composer.Add(new CharCapability(value));
            var composition = composer.Build();
            
            Assert.Equal(value, composition.Subject);
            
            
            var typedComposition = (IComposition<char>)composition;
            var capabilities = typedComposition.GetAll<CharCapability>();
            Assert.Single(capabilities);
            Assert.Equal(value, capabilities[0].Subject);
        }
    }

    [Fact]
    public void DecimalValueTypes_CanComposeAndFind()
    {
        var values = new[] { 0m, 1m, -1m, 3.14159m, decimal.MaxValue, decimal.MinValue };
        
        foreach (var value in values)
        {
            var composer = Composer.For(value);
            composer.Add(new DecimalCapability(value));
            var composition = composer.Build();
            
            Assert.Equal(value, composition.Subject);
            
            
            var typedComposition = (IComposition<decimal>)composition;
            var capabilities = typedComposition.GetAll<DecimalCapability>();
            Assert.Single(capabilities);
            Assert.Equal(value, capabilities[0].Subject);
        }
    }

    [Fact]
    public void DateTimeValueTypes_CanComposeAndFind()
    {
        var values = new[]
        {
            DateTime.MinValue,
            DateTime.MaxValue,
            new DateTime(2025, 10, 1),
            new DateTime(2025, 10, 1, 14, 30, 0),
            DateTime.Now.Date, // Remove time component for consistency
            new DateTime(1970, 1, 1) // Unix epoch
        };
        
        foreach (var value in values)
        {
            var composer = Composer.For(value);
            composer.Add(new DateTimeCapability(value));
            var composition = composer.Build();
            
            Assert.Equal(value, composition.Subject);
            
            
            var typedComposition = (IComposition<DateTime>)composition;
            var capabilities = typedComposition.GetAll<DateTimeCapability>();
            Assert.Single(capabilities);
            Assert.Equal(value, capabilities[0].Subject);
        }
    }

    [Fact]
    public void GuidValueTypes_CanComposeAndFind()
    {
        var values = new[]
        {
            Guid.Empty,
            Guid.NewGuid(),
            new Guid("12345678-1234-1234-1234-123456789abc"),
            new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff")
        };
        
        foreach (var value in values)
        {
            var composer = Composer.For(value);
            composer.Add(new GuidCapability(value));
            var composition = composer.Build();
            
            Assert.Equal(value, composition.Subject);
            
            
            var typedComposition = (IComposition<Guid>)composition;
            var capabilities = typedComposition.GetAll<GuidCapability>();
            Assert.Single(capabilities);
            Assert.Equal(value, capabilities[0].Subject);
        }
    }

    [Fact]
    public void EnumValueTypes_CanComposeAndFind()
    {
        var values = new[] { TestEnum.First, TestEnum.Second, TestEnum.Third };
        
        foreach (var value in values)
        {
            var composer = Composer.For(value);
            composer.Add(new EnumCapability(value));
            var composition = composer.Build();
            
            Assert.Equal(value, composition.Subject);
            
            
            var typedComposition = (IComposition<TestEnum>)composition;
            var capabilities = typedComposition.GetAll<EnumCapability>();
            Assert.Single(capabilities);
            Assert.Equal(value, capabilities[0].Subject);
        }
    }

    [Fact]
    public void CustomStructValueTypes_CanComposeAndFind()
    {
        var values = new[]
        {
            new Point(0, 0),
            new Point(1, 2),
            new Point(-5, 10),
            new Point(int.MaxValue, int.MinValue)
        };
        
        foreach (var value in values)
        {
            var composer = Composer.For(value);
            composer.Add(new PointCapability(value));
            var composition = composer.Build();
            
            Assert.Equal(value, composition.Subject);
            
            
            var typedComposition = (IComposition<Point>)composition;
            var capabilities = typedComposition.GetAll<PointCapability>();
            Assert.Single(capabilities);
            Assert.Equal(value, capabilities[0].Subject);
        }
    }

    [Fact]
    public void RecordStructValueTypes_CanComposeAndFind()
    {
        var values = new[]
        {
            new PersonRecord("Alice", 30),
            new PersonRecord("Bob", 25),
            new PersonRecord("", 0),
            new PersonRecord("Test", int.MaxValue)
        };
        
        foreach (var value in values)
        {
            var composer = Composer.For(value);
            composer.Add(new PersonRecordCapability(value));
            var composition = composer.Build();
            
            Assert.Equal(value, composition.Subject);
            
            
            var typedComposition = (IComposition<PersonRecord>)composition;
            var capabilities = typedComposition.GetAll<PersonRecordCapability>();
            Assert.Single(capabilities);
            Assert.Equal(value, capabilities[0].Subject);
        }
    }

    [Fact]
    public void MultipleCapabilitiesPerValueType_Work()
    {
        
        var number = 777;
        
        
        var composer = Composer.For(number);
        composer.Add(new IntCapability(number));
        composer.Add(new IntCapability(number)); // Same type, different instance
        var composition = composer.Build();
        
        
        Assert.Equal(number, composition.Subject);
        Assert.Equal(2, composition.TotalCapabilityCount);
        
        
        var typedComposition = (IComposition<int>)composition;
        var capabilities = typedComposition.GetAll<IntCapability>();
        Assert.Equal(2, capabilities.Count);
        Assert.All(capabilities, cap => Assert.Equal(number, cap.Subject));
    }
    
    private class StringCapability(string subject) : ICapability<string>
    {
        public string Subject { get; } = subject;
    }
    
    private class ObjectCapability(object subject) : ICapability<object>
    {
        public object Subject { get; } = subject;
    }
}
