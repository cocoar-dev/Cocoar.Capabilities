namespace Cocoar.Capabilities.Core.Tests;

public class TryAddMethodsTests
{
    public interface ILogCapability<in TSubject> : ICapability<TSubject>
    {
        string Level { get; }
    }
    
    public interface ICacheCapability<in TSubject> : ICapability<TSubject>
    {
        TimeSpan Duration { get; }
    }
    
    public class LogCapability<TSubject> : ILogCapability<TSubject>
    {
        public string Level { get; }
        public LogCapability(string level) => Level = level;
        public override string ToString() => $"Log[{Level}]";
    }
    
    public class CacheCapability<TSubject> : ICacheCapability<TSubject>
    {
        public TimeSpan Duration { get; }
        public CacheCapability(TimeSpan duration) => Duration = duration;
        public override string ToString() => $"Cache[{Duration}]";
    }

    [Fact]
    public void Has_ShouldReturnTrueWhenCapabilityExists()
    {
        var subject = "test-has-capability";
        
        var builder = Composer.For(subject)
            .Add(new LogCapability<string>("Debug"))
            .AddAs<ICacheCapability<string>>(new CacheCapability<string>(TimeSpan.FromMinutes(5)));
        
        Assert.True(builder.Has<LogCapability<string>>());
        
        Assert.True(builder.Has<ICacheCapability<string>>());
        
        Assert.False(builder.Has<SimpleTestCapability>());
    }

    [Fact]
    public void TryAdd_ShouldAddWhenCapabilityDoesNotExist()
    {
        var subject = "test-try-add";
        
        var log1 = new LogCapability<string>("Debug");
        var log2 = new LogCapability<string>("Info");
        
        var builder = Composer.For(subject);
        
        Assert.False(builder.Has<LogCapability<string>>());
        builder.TryAdd(log1);
        Assert.True(builder.Has<LogCapability<string>>());
        
        builder.TryAdd(log2);
        
        var bag = builder.Build();
        var allLogs = bag.GetAll<LogCapability<string>>();
        Assert.Single(allLogs);
        Assert.Contains(log1, allLogs);
        Assert.DoesNotContain(log2, allLogs);
    }
    
    [Fact]
    public void TryAddAs_ShouldAddWhenContractDoesNotExist()
    {
        var subject = "test-try-add-as";
        
        var log1 = new LogCapability<string>("Debug");
        var log2 = new LogCapability<string>("Info");
        
        var builder = Composer.For(subject);
        
        Assert.False(builder.Has<ILogCapability<string>>());
        builder.TryAddAs<ILogCapability<string>>(log1);
        Assert.True(builder.Has<ILogCapability<string>>());
        
        builder.TryAddAs<ILogCapability<string>>(log2);
        
        var bag = builder.Build();
        var allLogInterfaces = bag.GetAll<ILogCapability<string>>();
        Assert.Single(allLogInterfaces);
        Assert.Contains(log1, allLogInterfaces);
        Assert.DoesNotContain(log2, allLogInterfaces);
    }
    
    [Fact]
    public void TryAdd_And_TryAddAs_ShouldWorkIndependently()
    {
        var subject = "test-try-methods-independent";
        
        var log1 = new LogCapability<string>("Debug");
        var log2 = new LogCapability<string>("Info");
        
        var builder = Composer.For(subject);
        
        builder.TryAdd(log1);
        Assert.True(builder.Has<LogCapability<string>>());
        Assert.False(builder.Has<ILogCapability<string>>());
        
        builder.TryAddAs<ILogCapability<string>>(log2);
        Assert.True(builder.Has<LogCapability<string>>());
        Assert.True(builder.Has<ILogCapability<string>>());
        
        var bag = builder.Build();
        
        var concreteResults = bag.GetAll<LogCapability<string>>();
        var interfaceResults = bag.GetAll<ILogCapability<string>>();
        
        Assert.Single(concreteResults);
        Assert.Single(interfaceResults);
        Assert.Contains(log1, concreteResults);
        Assert.Contains(log2, interfaceResults);
    }
    
    [Fact]
    public void TryAdd_Methods_ShouldSupportFluentChaining()
    {
        var subject = "test-fluent-try-add";
        
        var log = new LogCapability<string>("Debug");
        var cache = new CacheCapability<string>(TimeSpan.FromMinutes(5));
        var duplicate = new LogCapability<string>("Info");
        
        var bag = Composer.For(subject)
            .TryAdd(log)
            .TryAddAs<ICacheCapability<string>>(cache)
            .TryAdd(duplicate)
            .Build();
        
        Assert.Single(bag.GetAll<LogCapability<string>>());
        Assert.Single(bag.GetAll<ICacheCapability<string>>());
        Assert.Contains(log, bag.GetAll<LogCapability<string>>());
        Assert.DoesNotContain(duplicate, bag.GetAll<LogCapability<string>>());
    }
}
