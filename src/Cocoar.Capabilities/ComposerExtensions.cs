using Cocoar.Capabilities.Core;

namespace Cocoar.Capabilities;

public static class ComposerExtensions
{
    public static IComposition<TSubject> BuildAndRegister<TSubject>(this Composer<TSubject> composer) 
        where TSubject : notnull
    {
        var composition = composer.Build();
        CompositionRegistryCore.Register(composition);
        return composition;
    }

}
