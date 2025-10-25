using Xunit;


namespace Cocoar.Capabilities.Tests.ConfigTests;


public interface IAppSettings
{
    string ApplicationName { get; }
    string Version { get; }
    bool IsProduction { get; }
}

public class AppSettings : IAppSettings
{
    public string ApplicationName { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public bool IsProduction { get; set; }
}

public class ConfigManager
{
    public List<ConfigBuilder> _configurations { get; }
    private readonly CapabilityScope _capabilityScope = new();

    public ConfigManager(Func<ConfigureBuilder, ConfigBuilder[]> configure)
    {
        _configurations = configure?.Invoke(new ConfigureBuilder(_capabilityScope)).Select(s => s.Build()).ToList() ?? new List<ConfigBuilder>();
    }
}


public class ConfigurationTests
{
    [Fact]
    public void BasicConfigurationTest()
    {

        var configManager = new ConfigManager(configure =>
        [
            configure.ConcreteType<AppSettings>().ExposeAs<IAppSettings>(),
        ]);
    }
}
