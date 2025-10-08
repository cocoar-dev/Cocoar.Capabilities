namespace Cocoar.Capabilities;

public interface IComposerRegistry : IDisposable
{
    void Register<TSubject>(TSubject subject, Composer composer) where TSubject : notnull;
    bool TryGet<TSubject>(TSubject subject, out Composer composer) where TSubject : notnull;
    bool Remove<TSubject>(TSubject subject) where TSubject : notnull;
    
    bool TryGet(object subject, out object composer);
    bool Remove(object subject);
}