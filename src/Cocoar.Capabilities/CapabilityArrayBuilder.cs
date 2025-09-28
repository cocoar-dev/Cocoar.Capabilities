namespace Cocoar.Capabilities;

internal static class CapabilityArrayBuilder
{
    internal static (Dictionary<Type, Array> result, int totalCount) Build(
        Dictionary<int, (object capability, int? order)> capabilitiesById,
        Dictionary<Type, List<int>> typeToIds)
    {
        var result = new Dictionary<Type, Array>(typeToIds.Count);
        var totalCount = capabilitiesById.Count;

        foreach (var typeKvp in typeToIds)
        {
            var ids = typeKvp.Value;
            var capabilities = new List<CapabilityMetadata>(ids.Count);
            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                var entry = capabilitiesById[id];
                capabilities.Add(new CapabilityMetadata(entry.capability, entry.order, id));
            }

            CapabilityOrdering.SortInPlace(capabilities);

            var arr = new CapabilityMetadata[capabilities.Count];
            for (var i = 0; i < capabilities.Count; i++) 
                arr[i] = capabilities[i];
            result[typeKvp.Key] = arr;
        }

        return (result, totalCount);
    }
}
