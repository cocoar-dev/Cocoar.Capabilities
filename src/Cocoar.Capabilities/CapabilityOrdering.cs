namespace Cocoar.Capabilities;

internal static class CapabilityOrdering
{
    internal static void SortInPlace(List<CapabilityMetadata> capabilities)
    {
        if (capabilities.Count <= 1)
        {
            return;
        }

        bool hasOrdering = false;
        for (int i = 0; i < capabilities.Count; i++)
        {
            if (capabilities[i].Order.HasValue)
            {
                hasOrdering = true;
                break;
            }
        }
        if (!hasOrdering)
        {
            return;
        }

        // Sort by (order ?? 0, insertionId) for stable, deterministic ordering
        capabilities.Sort((a, b) =>
        {
            int oa = a.Order ?? 0;
            int ob = b.Order ?? 0;
            if (oa != ob)
            {
                return oa.CompareTo(ob);
            }

            // If orders are equal, use insertion ID for stable ordering
            return a.InsertionId.CompareTo(b.InsertionId);
        });
    }
}
