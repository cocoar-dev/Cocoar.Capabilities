# Multiple Contracts

A single capability instance can be registered under multiple contract types, making it discoverable through different interfaces.

## Single Contract

Use `AddAs<TContract>()` to register under a specific type:

```csharp
scope.Compose(host)
    .AddAs<IPlugin>(new EmailPlugin())
    .Build();

var plugins = composition.GetAll<IPlugin>();  // [EmailPlugin]
```

The capability is only discoverable as `IPlugin`, not as `EmailPlugin`.

## Tuple Syntax

Register under multiple contracts at once:

```csharp
public class EmailNotifier : INotifier, IHealthCheck, IDisposable
{
    public string Name => "Email";
    public bool IsHealthy => true;
    public void Dispose() { }
}

scope.Compose(host)
    .AddAs<(INotifier, IHealthCheck, IDisposable)>(new EmailNotifier())
    .Build();

var notifiers = composition.GetAll<INotifier>();     // [EmailNotifier]
var checks = composition.GetAll<IHealthCheck>();      // [EmailNotifier]
var disposables = composition.GetAll<IDisposable>();  // [EmailNotifier]
```

The same instance appears in each contract's collection.

## Grouping Multiple Instances

```csharp
scope.Compose(host)
    .AddAs<IPlugin>(new EmailPlugin())
    .AddAs<IPlugin>(new SmsPlugin())
    .AddAs<IPlugin>(new SlackPlugin())
    .Build();

var plugins = composition.GetAll<IPlugin>();  // [Email, Sms, Slack]
```

## When to Use

Use `AddAs` when:
- A capability implements multiple interfaces and should be discoverable through each
- You want to group heterogeneous types under a common contract
- Consumer code should work with abstractions, not concrete types

Use regular `Add()` when the capability's own type is how it should be discovered.
