namespace Cocoar.Capabilities;

public interface ICapabilityRegistry : IDisposable
{
    void RegisterComposer<TSubject>(Composer<TSubject> composer) where TSubject : notnull;
    void RemoveComposer<TSubject>(TSubject subject) where TSubject : notnull;
    bool TryGetComposer<TSubject>(TSubject subject, out Composer<TSubject> composer) where TSubject : notnull;
    void RegisterComposition<TSubject>(IComposition<TSubject> composition) where TSubject : notnull;
    bool TryGetComposition<TSubject>(TSubject subject, out IComposition<TSubject> composition) where TSubject : notnull;
    void TransitionToComposition<TSubject>(IComposition<TSubject> composition) where TSubject : notnull;
    bool Remove<TSubject>(TSubject subject) where TSubject : notnull;
    bool TryGetComposer(object subject, out object composer);
    bool TryGetComposition(object subject, out IComposition composition);
    bool Remove(object subject);
}
