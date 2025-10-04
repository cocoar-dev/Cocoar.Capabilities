namespace Cocoar.Capabilities;

public interface ICompositionRegistry : IDisposable
{
    void Register<TSubject>(TSubject subject, IComposition<TSubject> composition) where TSubject : notnull;
    bool TryGet<TSubject>(TSubject subject, out IComposition<TSubject> composition) where TSubject : notnull;
    bool Remove<TSubject>(TSubject subject) where TSubject : notnull;
    
    bool TryGet(object subject, out IComposition composition);
    bool Remove(object subject);
}