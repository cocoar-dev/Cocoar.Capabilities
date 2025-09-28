using Cocoar.Capabilities;

namespace Cocoar.Capabilities.Tests;

public class OrderingTests
{
    private sealed record Subject(int Id);

    private sealed record OrderedCap(int Id, int Priority)
    {
        public int Order => Priority;
    }

    private sealed record PlainCap(int Id);

    private static readonly int[] ExpectedSorted = {10,20,30,40,50};
    private static readonly int[] ExpectedStability = {1,2,3,4,5};
    private static readonly int[] ExpectedPlain = {1,2,3,4,5};

    [Fact]
    public void OrderedCapabilities_AreSortedAscending()
    {
        var scope = new CapabilityScope();
        var subj = new Subject(1);
        var composer = scope.For(subj);

        // Intentionally add in unsorted order
        composer.Add(new OrderedCap(1, 50), order: 50);
        composer.Add(new OrderedCap(2, 10), order: 10);
        composer.Add(new OrderedCap(3, 30), order: 30);
        composer.Add(new OrderedCap(4, 40), order: 40);
        composer.Add(new OrderedCap(5, 20), order: 20);

        var comp = composer.Build();
        var ordered = comp.GetAll<OrderedCap>();

        var priorities = ordered.Select(c => c.Priority).ToArray();
    Assert.Equal(ExpectedSorted, priorities);
    }

    [Fact]
    public void OrderedCapabilities_StableSort_PreservesInsertionForSamePriority()
    {
        var scope = new CapabilityScope();
        var subj = new Subject(2);
        var composer = scope.For(subj);

        // All same priority => resulting order must match insertion order
        composer.Add(new OrderedCap(1, 5), order: 5);
        composer.Add(new OrderedCap(2, 5), order: 5);
        composer.Add(new OrderedCap(3, 5), order: 5);
        composer.Add(new OrderedCap(4, 5), order: 5);
        composer.Add(new OrderedCap(5, 5), order: 5);

        var comp = composer.Build();
        var ordered = comp.GetAll<OrderedCap>();

        var ids = ordered.Select(c => c.Id).ToArray();
    Assert.Equal(ExpectedStability, ids); // stability guarantee
    }

    [Fact]
    public void UnorderedCapabilities_NoSort_OriginalOrderPreserved()
    {
        var scope = new CapabilityScope();
        var subj = new Subject(3);
        var composer = scope.For(subj);

        composer.Add(new PlainCap(1));
        composer.Add(new PlainCap(2));
        composer.Add(new PlainCap(3));
        composer.Add(new PlainCap(4));
        composer.Add(new PlainCap(5));

        var comp = composer.Build();
        var plain = comp.GetAll<PlainCap>();
        var ids = plain.Select(c => c.Id).ToArray();
        Assert.Equal(ExpectedPlain, ids);
    }

    [Fact]
    public void GlobalGetAll_MixedOrderedAndPlain_StableOrderingApplied()
    {
        var scope = new CapabilityScope();
        var subj = new Subject(4);
        var composer = scope.For(subj);

        // Plain (order defaults to 0)
        composer.Add(new PlainCap(1));           // P1
        // Ordered with higher priority numbers placed after plain (0) when positive
        composer.Add(new OrderedCap(101, 10), order: 10);  // O1 (10)
        composer.Add(new PlainCap(2));           // P2
        composer.Add(new OrderedCap(102, 5), order: 5);    // O2 (5)
        composer.Add(new PlainCap(3));           // P3

        var comp = composer.Build();
        var all = comp.GetAll(); // triggers global ordering path

        // Project to a tuple (type, id/priority) for assertion clarity
        var projection = all.Select(c => c switch
        {
            OrderedCap oc => $"O:{oc.Order}:{oc.Id}",
            PlainCap pc => $"P:0:{pc.Id}",
            _ => "?"
        }).ToArray();

        // Expected: all order==0 (plain) in original insertion order among themselves, then ordered by Order ascending (5 then 10)
        var expected = new[]{
            "P:0:1", // Plain 1
            "P:0:2", // Plain 2 (stability among order 0)
            "P:0:3", // Plain 3
            "O:5:102", // Ordered priority 5
            "O:10:101" // Ordered priority 10
        };
        Assert.Equal(expected, projection);
    }
}
