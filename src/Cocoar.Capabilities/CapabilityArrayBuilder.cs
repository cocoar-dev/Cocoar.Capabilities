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
            var capabilities = new List<(object capability, int? order)>(ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                capabilities.Add(capabilitiesById[ids[i]]);
            }

            CapabilityOrdering.SortInPlace(capabilities);

            var arr = Array.CreateInstance(typeof(object), capabilities.Count);
            for (int i = 0; i < capabilities.Count; i++) 
                arr.SetValue(capabilities[i].capability, i);
            result[typeKvp.Key] = arr;
        }

        return (result, totalCount);
    }
}
