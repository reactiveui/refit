namespace Refit;

internal static class EnumerableExtensions
{
    internal static EnumerablePeek TryGetSingle<T>(this IEnumerable<T> enumerable, out T? value)
    {
        value = default;
        using var enumerator = enumerable.GetEnumerator();
        var hasFirst = enumerator.MoveNext();
        if (!hasFirst)
            return EnumerablePeek.Empty;

        value = enumerator.Current;
        if (!enumerator.MoveNext())
            return EnumerablePeek.Single;

        value = default;
        return EnumerablePeek.Many;
    }
}

internal static class EmptyDictionary<TKey, TValue> where TKey : notnull
{
    private static readonly Dictionary<TKey, TValue> Value = [];

    internal static Dictionary<TKey, TValue> Get() => Value;
}

internal enum EnumerablePeek
{
    Empty,
    Single,
    Many
}
