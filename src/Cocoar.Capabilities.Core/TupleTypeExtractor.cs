namespace Cocoar.Capabilities.Core;

internal static class TupleTypeExtractor
{
    public static Type[] GetTupleTypes<TTuple>()
    {
        var tupleType = typeof(TTuple);
        
        if (!tupleType.IsGenericType)
        {
            return [tupleType];
        }
        
        var genericTypeDefinition = tupleType.GetGenericTypeDefinition();
        if (IsValueTupleType(genericTypeDefinition))
        {
            return tupleType.GetGenericArguments();
        }
        
        return [tupleType];
    }
    
    public static void ValidateCapabilityTypes<TSubject>(Type[] types)
    {
        var capabilityInterface = typeof(ICapability<TSubject>);
        
        foreach (var type in types)
        {
            if (!capabilityInterface.IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    $"Type '{type.Name}' must implement ICapability<{typeof(TSubject).Name}> " +
                    $"to be registered as a capability contract.");
            }
        }
    }
    
    private static bool IsValueTupleType(Type type)
    {
        return type.IsGenericTypeDefinition &&
               type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;
    }
}
