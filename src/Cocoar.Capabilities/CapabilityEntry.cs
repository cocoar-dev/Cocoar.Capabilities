namespace Cocoar.Capabilities;

internal sealed class CapabilityEntry
{
    private readonly object? _composer;
    private readonly object? _composition;

    private CapabilityEntry(object? composer, object? composition)
    {
        _composer = composer;
        _composition = composition;
    }

    public static CapabilityEntry FromComposer(object composer) =>
        new(composer ?? throw new ArgumentNullException(nameof(composer)), null);

    public static CapabilityEntry FromComposition(object composition) =>
        new(null, composition ?? throw new ArgumentNullException(nameof(composition)));

    public static CapabilityEntry FromBoth(object composer, object composition) =>
        new(composer ?? throw new ArgumentNullException(nameof(composer)), composition ?? throw new ArgumentNullException(nameof(composition)));

    public bool TryGetComposer(out Composer composer)
    {
        if (_composer is Composer typed)
        {
            composer = typed;
            return true;
        }
        composer = null!;
        return false;
    }

    public bool TryGetComposition(out IComposition composition)
    {
        if (_composition is IComposition typed)
        {
            composition = typed;
            return true;
        }
        composition = null!;
        return false;
    }

    // Dispose any disposable objects we directly reference (composer, composition).
    // Idempotent: if neither implements IDisposable, it's a no-op.
    internal void DisposeOwnedResources()
    {
        if (_composer is IDisposable disposableComposer)
        {
            disposableComposer.Dispose();
        }
        if (_composition is IDisposable disposableComposition && !ReferenceEquals(_composition, _composer))
        {
            disposableComposition.Dispose();
        }
    }
}
