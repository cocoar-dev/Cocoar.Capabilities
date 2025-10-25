using Xunit;

namespace Cocoar.Capabilities.Tests;

/// <summary>
/// Demonstrates real-world usage scenarios for Owner and Anchor functionality.
/// These tests serve as both verification and documentation.
/// </summary>
public class OwnerAndAnchorExampleTests
{
    // Simulated domain objects
    private class PipelineHost
    {
        public string Name { get; init; } = string.Empty;
    }

    private class EnvironmentContext
    {
        public string Environment { get; init; } = string.Empty;
    }

    private class TenantContext
    {
        public string TenantId { get; init; } = string.Empty;
    }

    private class ConfigurationManager
    {
        public Dictionary<string, string> Settings { get; } = new();
    }

    // Sample capabilities
    private record DiagnosticsCapability(bool Enabled);
    private record EnvironmentCapability(string EnvName);
    private record TenantCapability(string TenantId);
    private record ConfigCapability(string Key, string Value);

    [Fact]
    public void Example_PipelineWithMultipleContexts()
    {
        // Scenario: A pipeline framework where the scope is associated with
        // multiple contextual objects that need capabilities

        using var scope = new CapabilityScope();

        var pipeline = new PipelineHost { Name = "DataPipeline" };
        var env = new EnvironmentContext { Environment = "Production" };
        var tenant = new TenantContext { TenantId = "tenant-123" };

        // Initialize scope with owner and anchors
        scope.Owner.Set(pipeline);
        scope.Anchors
         .Set(env);
        scope.Anchors
        .Set("tenant", tenant);

        // Later, with only scope available, compose capabilities for different subjects

        // Add diagnostics to the pipeline itself
        scope.Owner.Compose<PipelineHost>()
             .Add(new DiagnosticsCapability(true))
             .Build();

        // Add environment-specific capability
        scope.Anchors.Compose<EnvironmentContext>()
             .Add(new EnvironmentCapability("Production"))
             .Build();

        // Add tenant-specific capability
        scope.Anchors.Compose("tenant")
             .Add(new TenantCapability("tenant-123"))
             .Build();

        // Verify all compositions exist
        var pipelineComp = scope.Compositions.GetOrDefault(pipeline);
        var envComp = scope.Compositions.GetOrDefault(env);
        var tenantComp = scope.Compositions.GetOrDefault(tenant);

        Assert.NotNull(pipelineComp);
        Assert.NotNull(envComp);
        Assert.NotNull(tenantComp);

        Assert.True(pipelineComp!.GetAll<DiagnosticsCapability>()[0].Enabled);
        Assert.Equal("Production", envComp!.GetAll<EnvironmentCapability>()[0].EnvName);
        Assert.Equal("tenant-123", tenantComp!.GetAll<TenantCapability>()[0].TenantId);
    }

    [Fact]
    public void Example_ConfigurationSystemWithOwner()
    {
        // Scenario: A configuration system where the manager owns the scope

        using var scope = new CapabilityScope();
        var configManager = new ConfigurationManager();
        configManager.Settings["LogLevel"] = "Debug";
        configManager.Settings["Timeout"] = "30";

        scope.Owner.Set(configManager);

        // Extension code receives only the scope
        EnrichConfiguration(scope);

        // Verify capabilities were added to the config manager
        var composition = scope.Compositions.GetOrDefault(configManager);
        Assert.NotNull(composition);

        var configs = composition!.GetAll<ConfigCapability>();
        Assert.Equal(2, configs.Count);
    }

    // Helper method that only receives the scope, not the manager
    private static void EnrichConfiguration(CapabilityScope scope)
    {
        var manager = scope.Owner.Get<ConfigurationManager>();

        var composer = scope.Owner.Compose();
        foreach (var setting in manager.Settings)
        {
            composer.Add(new ConfigCapability(setting.Key, setting.Value));
        }
        composer.Build();
    }

    [Fact]
    public void Example_MultiTenantPlatform()
    {
        // Scenario: Multi-tenant platform where each scope represents a tenant session

        using var scope = new CapabilityScope();

        var tenant1 = new TenantContext { TenantId = "acme-corp" };
        var tenant2 = new TenantContext { TenantId = "globex-inc" };

        scope.Owner.Set(tenant1);
            scope.Anchors
             .Set("primary-tenant", tenant1)
             .Set("secondary-tenant", tenant2);

        // Compose for primary tenant (owner)
        scope.Owner.Compose<TenantContext>()
             .Add(new TenantCapability("acme-corp"))
             .Build();

        // Compose for secondary tenant (anchor)
        scope.Anchors.Compose("secondary-tenant")
             .Add(new TenantCapability("globex-inc"))
             .Build();

        var primary = scope.Compositions.GetOrDefault(tenant1);
        var secondary = scope.Compositions.GetOrDefault(tenant2);

        Assert.NotNull(primary);
        Assert.NotNull(secondary);
        Assert.Equal("acme-corp", primary!.GetAll<TenantCapability>()[0].TenantId);
        Assert.Equal("globex-inc", secondary!.GetAll<TenantCapability>()[0].TenantId);
    }

    [Fact]
    public void Example_WorkflowEngineWithNamedAnchors()
    {
        // Scenario: Workflow engine with multiple named contexts

        using var scope = new CapabilityScope();

        var workflowDef = new { Name = "OrderProcessing", Version = "1.0" };
        var executionCtx = new { ExecutionId = "exec-456", StartTime = DateTime.UtcNow };

        scope.Owner.Set(workflowDef);
            scope.Anchors
             .Set("execution", executionCtx);
             scope.Anchors
             .Set("environment", new EnvironmentContext { Environment = "Staging" });

        // Compose for execution context using named anchor
        scope.Anchors.Compose("execution")
             .Add(new DiagnosticsCapability(true))
             .Build();

        // Compose for environment using named anchor
        scope.Anchors.Compose("environment")
             .Add(new EnvironmentCapability("Staging"))
             .Build();

        var execComp = scope.Compositions.GetOrDefault(executionCtx);
        Assert.NotNull(execComp);
        Assert.True(execComp!.GetAll<DiagnosticsCapability>()[0].Enabled);
    }

    [Fact]
    public void Example_FluentChainingForSetup()
    {
        // Demonstrate fluent API for scope initialization

        using var scope = new CapabilityScope();

        var host = new PipelineHost { Name = "Main" };
        var env = new EnvironmentContext { Environment = "Dev" };
        var config = new ConfigurationManager();

        // All setup in one fluent chain
        scope.Owner.Set(host);
            scope.Anchors.Set(env)
            .Set(config)
            .Set("app-name", "MyApp")
            .Set("version", "2.0.0");

        // Verify all are accessible
        Assert.Same(host, scope.Owner.Get<PipelineHost>());
        Assert.True(scope.Anchors.TryGet<EnvironmentContext>(out var e) && e == env);
        Assert.True(scope.Anchors.TryGet<ConfigurationManager>(out var c) && c == config);
        Assert.True(scope.Anchors.TryGet("app-name", out var appName) && appName?.Equals("MyApp") == true);
        Assert.True(scope.Anchors.TryGet("version", out var ver) && ver?.Equals("2.0.0") == true);
    }
}
