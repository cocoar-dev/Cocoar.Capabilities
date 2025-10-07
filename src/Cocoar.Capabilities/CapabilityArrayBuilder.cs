namespace Cocoar.Capabilities;

internal static class CapabilityArrayBuilder
{
    internal static (Dictionary<Type, Array> result, int totalCount) Build<TSubject>(
        Dictionary<int, ICapability<TSubject>> capabilitiesById,
        Dictionary<Type, List<int>> typeToIds)
        where TSubject : notnull
    {
        var result = new Dictionary<Type, Array>(typeToIds.Count);
        var totalCount = capabilitiesById.Count;

        foreach (var typeKvp in typeToIds)
        {
            var ids = typeKvp.Value;
            var capabilities = new List<ICapability<TSubject>>(ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                capabilities.Add(capabilitiesById[ids[i]]);
            }

            CapabilityOrdering.SortInPlace(capabilities);

            var arr = Array.CreateInstance(typeof(ICapability<TSubject>), capabilities.Count);
            for (int i = 0; i < capabilities.Count; i++) arr.SetValue(capabilities[i], i);
            result[typeKvp.Key] = arr;
        }

        return (result, totalCount);
    }
}
