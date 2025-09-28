namespace Cocoar.Capabilities.Core.Tests;

public class SameTypeMultipleRegistrationTests
{
    public interface ILogCapability<in TSubject> : ICapability<TSubject> 
    {
        string Level { get; }
    }
    
    public interface IAuditCapability<in TSubject> : ICapability<TSubject>
    {
        string Level { get; }
    }
    
    public class LogCapability<TSubject> : ILogCapability<TSubject>, IAuditCapability<TSubject>
    {
        public string Level { get; }
        public LogCapability(string level) => Level = level;
        public override string ToString() => $"Log[{Level}]";
    }

    [Fact]
    public void SameConcreteType_DifferentRegistrations_QueryableExactlyAsRegistered()
    {
        var subject = "test-subject";
        
        // Create 4 instances of the SAME concrete type
        var log1 = new LogCapability<string>("Debug");
        var log2 = new LogCapability<string>("Info"); 
        var log3 = new LogCapability<string>("Warning");
        var log4 = new LogCapability<string>("Error");
        
        var bag = Composer.For(subject)
            .Add(log1)                                          // Concrete only
            .AddAs<ILogCapability<string>>(log2)                // ILogCapability only
            .AddAs<IAuditCapability<string>>(log3)              // IAuditCapability only
            .AddAs<(ILogCapability<string>, LogCapability<string>)>(log4)  // Both ILogCapability and concrete
            .Build();
        
        // Query by concrete type
        var concreteResults = bag.GetAll<LogCapability<string>>();
        Assert.Equal(2, concreteResults.Count);  // Only log1 and log4
        Assert.Contains(log1, concreteResults);  // Add() registered for concrete
        Assert.DoesNotContain(log2, concreteResults);  // AddAs<ILogCapability> - NOT concrete
        Assert.DoesNotContain(log3, concreteResults);  // AddAs<IAuditCapability> - NOT concrete
        Assert.Contains(log4, concreteResults);  // Tuple included concrete type
        
        // Query by ILogCapability interface
        var logResults = bag.GetAll<ILogCapability<string>>();
        Assert.Equal(2, logResults.Count);  // Only log2 and log4
        Assert.DoesNotContain(log1, logResults);  // Add() - NOT interface
        Assert.Contains(log2, logResults);  // AddAs<ILogCapability> registered for this interface
        Assert.DoesNotContain(log3, logResults);  // AddAs<IAuditCapability> - different interface
        Assert.Contains(log4, logResults);  // Tuple included this interface
        
        // Query by IAuditCapability interface
        var auditResults = bag.GetAll<IAuditCapability<string>>();
        Assert.Single(auditResults);  // Only log3
        Assert.DoesNotContain(log1, auditResults);  // Add() - NOT this interface
        Assert.DoesNotContain(log2, auditResults);  // AddAs<ILogCapability> - different interface
        Assert.Contains(log3, auditResults);  // AddAs<IAuditCapability> registered for this interface
        Assert.DoesNotContain(log4, auditResults);  // Tuple didn't include this interface
    }
    
    [Fact]
    public void RemoveWhere_RemovesSpecificCapabilityFromAllItsRegisteredTypes_LeavesOthersUnaffected()
    {
        var subject = "test-removal";
        
        // Create capabilities with overlapping interface registrations
        var log1 = new LogCapability<string>("Debug");   // Will be concrete only
        var log2 = new LogCapability<string>("Info");    // Will be ILogCapability only
        var log3 = new LogCapability<string>("Warning"); // Will be IAuditCapability only
        var log4 = new LogCapability<string>("Error");   // Will be registered for BOTH interfaces
        
        var builder = Composer.For(subject)
            .Add(log1)                                          // Concrete only
            .AddAs<ILogCapability<string>>(log2)                // ILogCapability only
            .AddAs<IAuditCapability<string>>(log3)              // IAuditCapability only
            .AddAs<(ILogCapability<string>, IAuditCapability<string>)>(log4);  // BOTH interfaces
        
        // Before removal - verify we can check by building a temp bag
        var tempBuilder = Composer.For("temp-check")
            .Add(log1)
            .AddAs<ILogCapability<string>>(log2)
            .AddAs<IAuditCapability<string>>(log3)
            .AddAs<(ILogCapability<string>, IAuditCapability<string>)>(log4);
        var tempBag = tempBuilder.Build();
        Assert.Equal(4, tempBag.TotalCapabilityCount);
        
        // Remove log4 specifically (the one registered for both interfaces)
        builder.RemoveWhere(cap => cap is LogCapability<string> log && log.Level == "Error");
        
        // Build and test final queries
        var bag = builder.Build();
        Assert.Equal(3, bag.TotalCapabilityCount);  // log4 was removed
        
        // Concrete type query - should only have log1 now (log4 was removed)
        var concreteResults = bag.GetAll<LogCapability<string>>();
        Assert.Single(concreteResults);
        Assert.Contains(log1, concreteResults);
        Assert.DoesNotContain(log4, concreteResults);  // log4 removed from concrete type too
        
        // ILogCapability query - should only have log2 now (log4 was removed)
        var logResults = bag.GetAll<ILogCapability<string>>();
        Assert.Single(logResults);
        Assert.Contains(log2, logResults);  // log2 still there
        Assert.DoesNotContain(log4, logResults);  // log4 removed from this interface
        
        // IAuditCapability query - should only have log3 (log4 was removed)
        var auditResults = bag.GetAll<IAuditCapability<string>>();
        Assert.Single(auditResults);
        Assert.Contains(log3, auditResults);  // log3 still there - unaffected!
        Assert.DoesNotContain(log4, auditResults);  // log4 removed from this interface too
        
        // KEY INSIGHT: Removing log4 removed it from ALL its registered types,
        // but left other capabilities registered under the same types completely unaffected
    }
}
