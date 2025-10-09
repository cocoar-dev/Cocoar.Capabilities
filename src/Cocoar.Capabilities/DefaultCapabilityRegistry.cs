using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Cocoar.Capabilities;

public sealed class DefaultCapabilityRegistry : ICapabilityRegistry
{
    private readonly ConditionalWeakTable<object, CapabilityEntry> _refTypeEntries = new();
    private readonly ConcurrentDictionary<object, CapabilityEntry> _valueTypeEntries = new();
    private readonly SubjectKeyCanonicalizer _canonicalizer;
    private bool _disposed;

    internal DefaultCapabilityRegistry(SubjectKeyCanonicalizer? canonicalizer = null)
    {
        _canonicalizer = canonicalizer ?? new SubjectKeyCanonicalizer(null);
    }

    public void RegisterComposer(Composer composer)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(composer);
        var key = Canonicalize(composer.Subject!, out var valueLike);
        var newEntry = CapabilityEntry.FromComposer(composer);
        if (TryGetEntry(key, valueLike, out var existing) && existing.TryGetComposition(out var comp))
        {
            newEntry = CapabilityEntry.FromBoth(composer, comp);
        }
        StoreEntry(key, valueLike, newEntry);
    }


    public void RemoveComposer(object subject)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(subject);
        var key = Canonicalize(subject!, out var valueLike);
        if (TryGetEntry(key, valueLike, out var existing) && existing.TryGetComposition(out var comp))
        {
            StoreEntry(key, valueLike, CapabilityEntry.FromComposition(comp));
        }
        else
        {
            RemoveEntry(key, valueLike);
        }
    }

    public bool TryGetComposer(object subject, out Composer composer)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(subject);
        var key = Canonicalize(subject!, out var valueLike);
        if (TryGetEntry(key, valueLike, out var entry) && entry.TryGetComposer(out composer))
        {
            return true;
        }
        composer = default!;
        return false;
    }

    public void RegisterComposition(IComposition composition)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(composition);
        var key = Canonicalize(composition.Subject!, out var valueLike);
        var newEntry = CapabilityEntry.FromComposition(composition);
        if (TryGetEntry(key, valueLike, out var existing) && existing.TryGetComposer(out Composer existingComposer))
        {
            newEntry = CapabilityEntry.FromBoth(existingComposer, composition);
        }
        StoreEntry(key, valueLike, newEntry);
    }

    public bool TryGetComposition(object subject, out IComposition composition)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(subject);
        var key = Canonicalize(subject!, out var valueLike);
        if (TryGetEntry(key, valueLike, out var entry) && entry.TryGetComposition(out composition))
        {
            return true;
        }
        composition = default!;
        return false;
    }

    public void TransitionToComposition(IComposition composition)
    {
        ThrowIfDisposed();
        
        ArgumentNullException.ThrowIfNull(composition);
        var key = Canonicalize(composition.Subject!, out var valueLike);
        var entry = CapabilityEntry.FromComposition(composition);
        StoreEntry(key, valueLike, entry);
    }

    public bool Remove(object subject)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(subject);
        var key = Canonicalize(subject!, out var valueLike);
        return valueLike ? _valueTypeEntries.TryRemove(key, out _) : _refTypeEntries.Remove(key);
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        foreach (var kvp in _valueTypeEntries)
        {
            kvp.Value.DisposeOwnedResources();
        }

        _valueTypeEntries.Clear();
        // ConditionalWeakTable entries are collected by GC; explicit enumeration not supported.
        
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    // Helpers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object Canonicalize(object subject, out bool valueLike) => _canonicalizer.Canonicalize(subject, out valueLike);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetEntry(object key, bool valueLike, out CapabilityEntry entry)
    {
        if (valueLike)
        {
            return _valueTypeEntries.TryGetValue(key, out entry!);
        }
        return _refTypeEntries.TryGetValue(key, out entry!);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void StoreEntry(object key, bool valueLike, CapabilityEntry entry)
    {
        if (valueLike)
        {
            _valueTypeEntries[key] = entry;
        }
        else
        {
            _refTypeEntries.AddOrUpdate(key, entry);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveEntry(object key, bool valueLike)
    {
        if (valueLike)
        {
            _valueTypeEntries.TryRemove(key, out _);
        }
        else
        {
            _refTypeEntries.Remove(key);
        }
    }
}
