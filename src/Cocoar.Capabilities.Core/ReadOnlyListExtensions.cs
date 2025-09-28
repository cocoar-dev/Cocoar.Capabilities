namespace Cocoar.Capabilities.Core;

public static class ReadOnlyListExtensions
{
    public static void ForEach<T>(this IReadOnlyList<T> list, Action<T> action)
    {
        if (list is null) throw new ArgumentNullException(nameof(list));
        if (action is null) throw new ArgumentNullException(nameof(action));

        foreach (var item in list)
        {
            action(item);
        }
    }
}
