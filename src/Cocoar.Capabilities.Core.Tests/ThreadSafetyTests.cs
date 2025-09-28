using System.Collections.Concurrent;

namespace Cocoar.Capabilities.Core.Tests;

public class ThreadSafetyTests
{
    [Fact]
    public async Task Bag_IsImmutable_ThreadSafe()
    {
        
        var subject = new TestSubject();
        var bag = Composer.For(subject)
            .Add(new TestCapability("thread-test-1"))
            .Add(new TestCapability("thread-test-2"))
            .Add(new TestCapability("thread-test-3"))
            .Build();

        var results = new ConcurrentBag<string>();
        var exceptions = new ConcurrentBag<Exception>();

        
        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(() =>
        {
            try
            {
                for (var j = 0; j < 100; j++)
                {
                    // Various read operations
                    var capabilities = bag.GetAll<TestCapability>();
                    results.Add($"Thread-{i}-Iteration-{j}: Found {capabilities.Count} capabilities");
                    
                    if (bag.TryGet<TestCapability>(out var cap))
                    {
                        results.Add($"Thread-{i}-Iteration-{j}: First capability: {cap.Value}");
                    }
                    
                    var count = bag.Count<TestCapability>();
                    results.Add($"Thread-{i}-Iteration-{j}: Count: {count}");
                    
                    var total = bag.TotalCapabilityCount;
                    results.Add($"Thread-{i}-Iteration-{j}: Total: {total}");
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        
        Assert.Empty(exceptions); // No exceptions should occur
        Assert.True(results.Count > 0); // Should have captured results

        Assert.Equal(3, bag.Count<TestCapability>());
        Assert.Equal(3, bag.TotalCapabilityCount);
    }

    [Fact]
    public async Task Builder_IsNotThreadSafe_ButDetectable()
    {
        // This test documents that builders are NOT thread-safe
        // but the design makes race conditions detectable
        
        
        var subject = new TestSubject();
        var builder = Composer.For(subject);
        var exceptions = new ConcurrentBag<Exception>();

        
        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(() =>
        {
            try
            {
                // Add multiple capabilities and try to build multiple times
                for (var j = 0; j < 10; j++)
                {
                    builder.Add(new TestCapability($"thread-{i}-{j}"));
                }
                
                // Multiple threads try to build
                var bag = builder.Build();
                Assert.NotNull(bag);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        
        // At minimum, multiple Build() calls should generate InvalidOperationException
        Assert.NotEmpty(exceptions);
        Assert.Contains(exceptions, ex => ex is InvalidOperationException && 
            (ex.Message.Contains("Build() can only be called once") || 
             ex.Message.Contains("Build() has already been called")));
    }
}
