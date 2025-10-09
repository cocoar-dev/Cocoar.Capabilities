namespace Cocoar.Capabilities;

internal static class CapabilityOrdering
{
    internal static void SortInPlace(List<(object capability, int? order)> capabilities)
    {
        if (capabilities.Count <= 1) return;

        var hasOrdering = false;
        for (int i = 0; i < capabilities.Count; i++)
        {
            if (capabilities[i].order.HasValue)
            {
                hasOrdering = true;
                break;
            }
        }
        if (!hasOrdering) return;

        var originals = new Dictionary<object, int>(capabilities.Count);
        for (int i = 0; i < capabilities.Count; i++) 
            originals[capabilities[i].capability] = i;

        capabilities.Sort((a, b) =>
        {
            var oa = a.order ?? 0;
            var ob = b.order ?? 0;
            if (oa != ob) return oa.CompareTo(ob);
            return originals[a.capability].CompareTo(originals[b.capability]);
        });
    }
}
