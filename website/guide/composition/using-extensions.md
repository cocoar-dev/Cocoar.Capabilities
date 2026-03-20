# Using* Extensions

The `Using*` extension methods provide fluent inline access to capabilities without extracting them first. They are defined on `IComposition`.

## UsingFirst / UsingLast

Execute an action on the first or last capability of a type:

```csharp
// Action — returns IComposition for chaining
composition
    .UsingFirst<ILogger>(logger => logger.Log("Starting"))
    .UsingFirst<IMetrics>(metrics => metrics.Increment("requests"));

// Function — returns the result
string name = composition.UsingFirst<ServiceIdentity, string>(id => id.Name);
```

`UsingFirst` throws if no capability of that type exists. Use `UsingFirstOrDefault` for a safe version that does nothing if the capability is missing:

```csharp
composition.UsingFirstOrDefault<ILogger>(logger => logger.Log("optional"));
```

`UsingLast` and `UsingLastOrDefault` work the same way but use the last capability.

## UsingEach

Execute an action for every capability of a type:

```csharp
// Action — returns IComposition for chaining
composition.UsingEach<IPlugin>(plugin => plugin.Initialize());

// Function — returns results as a list
IReadOnlyList<string> names = composition.UsingEach<IPlugin, string>(p => p.Name);
```

## UsingAll

Work with the full collection at once:

```csharp
// Action
composition.UsingAll<IPlugin>(plugins =>
{
    foreach (var plugin in plugins)
        plugin.Initialize();
});

// Function
int count = composition.UsingAll<IPlugin, int>(plugins => plugins.Count);
```

## Fluent Chaining

Action-based overloads return `IComposition`, enabling chains:

```csharp
composition
    .UsingFirst<ILogger>(l => l.Log("init"))
    .UsingEach<IPlugin>(p => p.Start())
    .UsingFirstOrDefault<IMetrics>(m => m.Record("started"));
```

## When Using* vs Get*

| Goal | Use |
|------|-----|
| Extract a value to use later | `GetFirstOrDefault<T>()`, `GetAll<T>()` |
| Inline side effect (logging, init) | `UsingFirst<T>()`, `UsingEach<T>()` |
| Transform and return | `UsingFirst<T, TResult>()` |
| Chain multiple operations | `UsingFirst` / `UsingEach` (action overloads) |

## Delegation Mapping

| Using* Method | Delegates to |
|---------------|-------------|
| `UsingFirst<T>(action)` | `GetRequiredFirst<T>()` |
| `UsingFirst<T, R>(func)` | `GetRequiredFirst<T>()` |
| `UsingFirstOrDefault<T>(action)` | `GetFirstOrDefault<T>()` |
| `UsingLast<T>(action)` | `GetRequiredLast<T>()` |
| `UsingLast<T, R>(func)` | `GetRequiredLast<T>()` |
| `UsingLastOrDefault<T>(action)` | `GetLastOrDefault<T>()` |
| `UsingEach<T>(action)` | `GetAll<T>()` |
| `UsingEach<T, R>(func)` | `GetAll<T>()` |
| `UsingAll<T>(action)` | `GetAll<T>()` |
| `UsingAll<T, R>(func)` | `GetAll<T>()` |
