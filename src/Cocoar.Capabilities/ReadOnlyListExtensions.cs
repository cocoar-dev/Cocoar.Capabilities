namespace Cocoar.Capabilities;

public static class ReadOnlyListExtensions
{
    public static void ForEach<T>(this IReadOnlyList<T> list, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in list)
        {
            action(item);
        }
    }
}
