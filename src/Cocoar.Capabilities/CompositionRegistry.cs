using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Cocoar.Capabilities.Core;

namespace Cocoar.Capabilities;

/// <summary>
/// Pluggable provider interface for the composition registry.
/// Allows frameworks to bridge registry access across plugin contexts.
/// </summary>
public interface ICompositionRegistryProvider
{
    void Register(object subject, IComposition composition);
    bool TryGet(object subject, out IComposition composition);
    bool Remove(object subject);
}

internal static class CompositionRegistryCore
{
    private static readonly ConcurrentDictionary<object, IComposition> _valueTypeStorage = new();

    private sealed class DefaultProvider : ICompositionRegistryProvider
    {
        private readonly ConditionalWeakTable<object, IComposition> _table = new();
        
        public void Register(object subject, IComposition composition)
        {
            // ConditionalWeakTable requires Remove before Add for updates
            _table.Remove(subject);
            _table.Add(subject, composition);
        }
            
        public bool TryGet(object subject, out IComposition composition)
        {
            return _table.TryGetValue(subject, out composition!);
        }
        
        public bool Remove(object subject) => _table.Remove(subject);
    }

    private static ICompositionRegistryProvider _provider = new DefaultProvider();

    internal static ICompositionRegistryProvider Provider
    {
        get => _provider;
        set => _provider = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal static void Register<TSubject>(IComposition<TSubject> composition) where TSubject : class
    {
        if (composition is null) throw new ArgumentNullException(nameof(composition));
        _provider.Register(composition.Subject, composition);
    }

    internal static void Register(IComposition composition)
    {
        if (composition is null) throw new ArgumentNullException(nameof(composition));
        
        // Value types stored with strong references to prevent GC collection issues
        // Reference types use weak references for automatic cleanup when subjects are no longer referenced
        if (composition.Subject.GetType().IsValueType)
        {
            _valueTypeStorage[composition.Subject] = composition;
        }
        else
        {
            _provider.Register(composition.Subject, composition);
        }
    }

    internal static bool TryGet<TSubject>(TSubject subject, out IComposition<TSubject> composition) where TSubject : notnull
    {
        if (subject is null) throw new ArgumentNullException(nameof(subject));
        
        if (typeof(TSubject).IsValueType)
        {
            if (_valueTypeStorage.TryGetValue(subject, out var comp) && comp is IComposition<TSubject> typedComp)
            {
                composition = typedComp;
                return true;
            }
        }
        else
        {
            if (_provider.TryGet(subject, out var comp) && comp is IComposition<TSubject> typedComp)
            {
                composition = typedComp;
                return true;
            }
        }
        
        composition = default!;
        return false;
    }

    internal static bool TryGet(object subject, out IComposition composition)
    {
        if (subject is null) throw new ArgumentNullException(nameof(subject));
        
        if (subject.GetType().IsValueType)
        {
            return _valueTypeStorage.TryGetValue(subject, out composition!);
        }
        else
        {
            return _provider.TryGet(subject, out composition!);
        }
    }

    internal static bool Remove(object subject)
    {
        if (subject is null) throw new ArgumentNullException(nameof(subject));
        
        if (subject.GetType().IsValueType)
        {
            return _valueTypeStorage.TryRemove(subject, out _);
        }
        else
        {
            return _provider.Remove(subject);
        }
    }
    
    internal static void ClearValueTypes()
    {
        _valueTypeStorage.Clear();
    }
    
    internal static int ValueTypeCount => _valueTypeStorage.Count;
}

public static class CompositionRegistryConfiguration
{
    public static ICompositionRegistryProvider Provider
    {
        get => CompositionRegistryCore.Provider;
        set => CompositionRegistryCore.Provider = value;
    }
    
    public static void ClearValueTypes()
    {
        CompositionRegistryCore.ClearValueTypes();
    }
    
    public static int ValueTypeCount => CompositionRegistryCore.ValueTypeCount;
}
