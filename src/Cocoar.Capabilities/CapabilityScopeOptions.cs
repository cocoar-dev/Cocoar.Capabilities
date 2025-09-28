namespace Cocoar.Capabilities;

public record CapabilityScopeOptions
{
    public bool UseComposerRegistry { get; init; } = true;
    public bool UseCompositionRegistry { get; init; } = true;
    public IEnumerable<ISubjectKeyMapper>? SubjectKeyMappers { get; init; }
}
