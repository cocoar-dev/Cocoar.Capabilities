using System.Collections.Concurrent;

namespace Cocoar.Capabilities;

internal sealed class SubjectKeyCanonicalizer
{
    private readonly ISubjectKeyMapper[] _builtInMappers = [ new StringSubjectKeyMapper() ];
    private readonly ConcurrentDictionary<Type, ISubjectKeyMapper?> _cache = new();
    private readonly Dictionary<Type, ISubjectKeyMapper> _overrides = new();
    private bool _sealed;

    public SubjectKeyCanonicalizer(IEnumerable<ISubjectKeyMapper>? overrides)
    {
        if (overrides is null) return;
        foreach (var mapper in overrides)
        {
            if (mapper is null) continue;
            foreach (var t in GetSupportedTypes(mapper))
            {
                if (!_overrides.ContainsKey(t))
                {
                    _overrides[t] = mapper;
                }
            }
        }
    }

    public object Canonicalize(object subject, out bool isValueLike)
    {
        ArgumentNullException.ThrowIfNull(subject);
    _sealed = true;
        var type = subject.GetType();

        if (_overrides.TryGetValue(type, out var overrideMapper))
        {
            var overrideKey = overrideMapper.Map(subject);
            isValueLike = overrideKey.GetType().IsValueType;
            return overrideKey;
        }

        var mapper = _cache.GetOrAdd(type, t =>
        {
            foreach (var m in _builtInMappers)
            {
                if (m.CanHandle(t)) return m;
            }
            return null;
        });

        if (mapper is not null)
        {
            var key = mapper.Map(subject);
            isValueLike = key.GetType().IsValueType;
            return key;
        }

        isValueLike = type.IsValueType;
        return subject;
    }

    public bool TryRegisterOverride(ISubjectKeyMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
    if (_sealed) return false;
        var added = false;
        foreach (var t in GetSupportedTypes(mapper))
        {
            if (!_overrides.ContainsKey(t))
            {
                _overrides[t] = mapper;
                added = true;
            }
        }
        return added;
    }

    private static IEnumerable<Type> GetSupportedTypes(ISubjectKeyMapper mapper)
    {
        if (mapper.CanHandle(typeof(string))) yield return typeof(string);
    }
}

public interface ISubjectKeyMapper
{
    bool CanHandle(Type subjectType);
    object Map(object subject);
}

internal sealed class StringSubjectKeyMapper : ISubjectKeyMapper
{
    public bool CanHandle(Type subjectType) => subjectType == typeof(string);
    public object Map(object subject) => new StringSubjectKey((string)subject);
}

internal readonly record struct StringSubjectKey(string Value)
{
    public override string ToString() => Value;
}
