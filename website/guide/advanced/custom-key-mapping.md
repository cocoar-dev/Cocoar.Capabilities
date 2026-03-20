# Custom Key Mapping

By default, subjects are used directly as registry keys. String subjects are mapped to a `StringSubjectKey` for consistent handling. You can customize this mapping for specific subject types.

## ISubjectKeyMapper Interface

```csharp
public interface ISubjectKeyMapper
{
    bool CanHandle(Type subjectType);
    object Map(object subject);
}
```

## Registering Mappers

```csharp
var scope = new CapabilityScope(new CapabilityScopeOptions
{
    SubjectKeyMappers = new[] { new CaseInsensitiveEmailMapper() }
});
```

## Example: Case-Insensitive Email

```csharp
public class CaseInsensitiveEmailMapper : ISubjectKeyMapper
{
    public bool CanHandle(Type subjectType) => subjectType == typeof(string);

    public object Map(object subject)
    {
        var email = (string)subject;
        return email.ToLowerInvariant();
    }
}
```

With this mapper, `"User@Example.COM"` and `"user@example.com"` map to the same registry entry.

## Default Behavior

Without custom mappers, string subjects are wrapped in `StringSubjectKey` for consistent dictionary key behavior. All other types use their default `GetHashCode()` and `Equals()` implementations.

## When to Use

Custom key mapping is rarely needed. Consider it when:
- You need case-insensitive or normalized key matching
- Your subjects have custom equality semantics that differ from their default implementation
- You're using domain objects as subjects and need a stable key across instances
