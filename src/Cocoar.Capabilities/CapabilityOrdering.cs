namespace Cocoar.Capabilities;

internal static class CapabilityOrdering
{
    internal static void SortInPlace<TSubject>(List<ICapability<TSubject>> capabilities)
        where TSubject : notnull
    {
        if (capabilities.Count <= 1) return;

        var hasOrdered = false;
        for (int i = 0; i < capabilities.Count; i++)
        {
            if (capabilities[i] is IOrderedCapability)
            {
                hasOrdered = true;
                break;
            }
        }
        if (!hasOrdered) return;

        var originals = new Dictionary<ICapability<TSubject>, int>(capabilities.Count);
        for (int i = 0; i < capabilities.Count; i++) originals[capabilities[i]] = i;

        capabilities.Sort((a, b) =>
        {
            var oa = (a as IOrderedCapability)?.Order ?? 0;
            var ob = (b as IOrderedCapability)?.Order ?? 0;
            if (oa != ob) return oa.CompareTo(ob);
            return originals[a].CompareTo(originals[b]);
        });
    }
}
