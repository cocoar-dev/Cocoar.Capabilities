using Cocoar.Capabilities.Core;

namespace Cocoar.Capabilities;

public static class Composition
{
    public static bool TryFind<TSubject>(TSubject subject, out IComposition<TSubject> composition) where TSubject : notnull
    {
        return CompositionRegistryCore.TryGet(subject, out composition);
    }

    public static IComposition<TSubject>? FindOrDefault<TSubject>(TSubject subject) where TSubject : notnull
    {
        return CompositionRegistryCore.TryGet(subject, out var composition) ? composition : null;
    }

    public static IComposition<TSubject> FindRequired<TSubject>(TSubject subject) where TSubject : notnull
    {
        if (CompositionRegistryCore.TryGet(subject, out var composition))
            return composition;
        
        throw new InvalidOperationException($"No composition found for subject of type '{typeof(TSubject).Name}'.");
    }

    public static bool TryFind(object subject, out IComposition composition)
    {
        if (subject is null) throw new ArgumentNullException(nameof(subject));
        return CompositionRegistryCore.TryGet(subject, out composition!);
    }

    public static IComposition? FindOrDefault(object subject)
    {
        if (subject is null) throw new ArgumentNullException(nameof(subject));
        return CompositionRegistryCore.TryGet(subject, out IComposition composition) ? composition : null;
    }

    public static IComposition FindRequired(object subject)
    {
        if (subject is null) throw new ArgumentNullException(nameof(subject));
        if (CompositionRegistryCore.TryGet(subject, out IComposition composition))
            return composition;
        
        throw new InvalidOperationException($"No composition found for subject of type '{subject.GetType().Name}'.");
    }
    
    public static bool Remove<TSubject>(TSubject subject) where TSubject : notnull
    {
        if (subject is null) throw new ArgumentNullException(nameof(subject));
        return CompositionRegistryCore.Remove(subject);
    }
    
    public static bool Remove(object subject)
    {
        if (subject is null) throw new ArgumentNullException(nameof(subject));
        return CompositionRegistryCore.Remove(subject);
    }
}
