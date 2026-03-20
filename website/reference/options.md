# Configuration Options

## CapabilityScopeOptions

Configuration for `CapabilityScope` behavior, passed at construction time.

```csharp
var scope = new CapabilityScope(new CapabilityScopeOptions
{
    UseComposerRegistry = true,
    UseCompositionRegistry = true,
    SubjectKeyMappers = null
});
```

## Properties

### UseComposerRegistry

| | |
|---|---|
| Type | `bool` |
| Default | `true` |
| Description | When `true`, `Compose()` automatically stores the `Composer` in the registry for later retrieval via `scope.Composers`. |

### UseCompositionRegistry

| | |
|---|---|
| Type | `bool` |
| Default | `true` |
| Description | When `true`, `Build()` automatically stores the `IComposition` in the registry for later retrieval via `scope.Compositions`. |

### SubjectKeyMappers

| | |
|---|---|
| Type | `IEnumerable<ISubjectKeyMapper>?` |
| Default | `null` |
| Description | Custom key mappers for subject canonicalization. When set, subjects are passed through matching mappers before being used as registry keys. |

## Configuration Examples

### Default (all registries on)

```csharp
var scope = new CapabilityScope();
```

### Composition-only (no composer caching)

```csharp
var scope = new CapabilityScope(new CapabilityScopeOptions
{
    UseComposerRegistry = false
});
```

### No registries (fire-and-forget)

```csharp
var scope = new CapabilityScope(new CapabilityScopeOptions
{
    UseComposerRegistry = false,
    UseCompositionRegistry = false
});
```

### Custom key mapping

```csharp
var scope = new CapabilityScope(new CapabilityScopeOptions
{
    SubjectKeyMappers = new ISubjectKeyMapper[]
    {
        new CaseInsensitiveEmailMapper()
    }
});
```
