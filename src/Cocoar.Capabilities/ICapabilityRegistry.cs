namespace Cocoar.Capabilities;

public interface ICapabilityRegistry : IDisposable
{
    void RegisterComposer(Composer composer);
    void RemoveComposer(object subject);
    bool TryGetComposer(object subject, out Composer composer);
    void RegisterComposition(IComposition composition);
    bool TryGetComposition(object subject, out IComposition composition);
    void TransitionToComposition(IComposition composition);
    bool Remove(object subject);
}
