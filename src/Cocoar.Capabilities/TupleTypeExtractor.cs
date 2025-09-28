namespace Cocoar.Capabilities;

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
    
    public static void ValidateCapabilityTypes(Type[] types)
    {
        // No validation needed - any type can be a capability
    }
    
    private static bool IsValueTupleType(Type type)
    {
        return type.IsGenericTypeDefinition &&
               type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;
    }
}
