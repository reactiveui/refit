namespace Refit;

internal static class EnumerableExtensions
{
    internal static EnumerablePeek TryGetSingle<T>(this IEnumerable<T> enumerable, out T? value)
    {
        value = default(T);
        using var enumerator = enumerable.GetEnumerator();
        var hasFirst = enumerator.MoveNext();
        if (!hasFirst)
            return EnumerablePeek.Empty;

        value = enumerator.Current;
        if (!enumerator.MoveNext())
            return EnumerablePeek.Single;

        value = default(T);
        return EnumerablePeek.Many;
    }
}

internal enum EnumerablePeek
{
    Empty,
    Single,
    Many
}
