#nullable enable

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Refit.TestSupport;

internal static class Assert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition)
            Fail(message ?? "Expected true but found false.");
    }

    public static void False(bool condition, string? message = null)
    {
        if (condition)
            Fail(message ?? "Expected false but found true.");
    }

    public static void Null(object? value)
    {
        if (value is not null)
            Fail($"Expected null but found {Format(value)}.");
    }

    public static T NotNull<T>([NotNull] T? value)
    {
        if (value is null)
            Fail("Expected a non-null value.");

        return value;
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (expected is string expectedString && actual is string actualString)
        {
            if (!string.Equals(expectedString, actualString, StringComparison.Ordinal))
                FailEqual(expected, actual);

            return;
        }

        if (TryCompareEnumerables(expected, actual, out var message))
        {
            if (message is not null)
                Fail(message);

            return;
        }

        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            FailEqual(expected, actual);
    }

    public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        var expectedArray = expected.ToArray();
        var actualArray = actual.ToArray();
        if (expectedArray.Length != actualArray.Length)
            Fail($"Expected sequence length {expectedArray.Length} but found {actualArray.Length}.");

        for (var i = 0; i < expectedArray.Length; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(expectedArray[i], actualArray[i]))
                Fail($"Expected item {i} to be {Format(expectedArray[i])} but found {Format(actualArray[i])}.");
        }
    }

    public static void NotEqual<T>(T expected, T actual)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
            Fail($"Expected values to differ, but both were {Format(actual)}.");
    }

    public static T IsType<T>(object? value)
    {
        if (value is null)
            Fail($"Expected type {typeof(T)} but found null.");

        if (value.GetType() != typeof(T))
            Fail($"Expected type {typeof(T)} but found {value.GetType()}.");

        return (T)value;
    }

    public static void Same(object? expected, object? actual)
    {
        if (!ReferenceEquals(expected, actual))
            Fail("Expected both values to reference the same object.");
    }

    public static void NotSame(object? expected, object? actual)
    {
        if (ReferenceEquals(expected, actual))
            Fail("Expected values to reference different objects.");
    }

    public static T Single<T>(IEnumerable<T> values)
    {
        using var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext())
            Fail("Expected a single item but the sequence was empty.");

        var result = enumerator.Current;
        if (enumerator.MoveNext())
            Fail("Expected a single item but the sequence contained more than one item.");

        return result;
    }

    public static void Empty(IEnumerable values)
    {
        foreach (var _ in values)
            Fail("Expected an empty sequence.");
    }

    public static void Empty(string? value)
    {
        if (!string.IsNullOrEmpty(value))
            Fail($"Expected an empty string but found {Format(value)}.");
    }

    public static void NotEmpty(IEnumerable values)
    {
        foreach (var _ in values)
            return;

        Fail("Expected a non-empty sequence.");
    }

    public static void Contains(string expectedSubstring, string? actualString) =>
        Contains(expectedSubstring, actualString, StringComparison.Ordinal);

    public static void Contains(
        string expectedSubstring,
        string? actualString,
        StringComparison comparisonType
    )
    {
        if (actualString is null || actualString.IndexOf(expectedSubstring, comparisonType) < 0)
            Fail($"Expected string to contain {Format(expectedSubstring)} but found {Format(actualString)}.");
    }

    public static void Contains<T>(T expected, IEnumerable<T> collection)
    {
        if (!collection.Any(item => AreEquivalent(expected, item)))
            Fail($"Expected sequence to contain {Format(expected)}.");
    }

    public static T Contains<T>(IEnumerable<T> collection, Predicate<T> predicate)
    {
        foreach (var item in collection)
        {
            if (predicate(item))
                return item;
        }

        Fail("Expected sequence to contain an item matching the predicate.");
        return default!;
    }

    public static void DoesNotContain<T>(T expected, IEnumerable<T> collection)
    {
        if (collection.Contains(expected))
            Fail($"Expected sequence not to contain {Format(expected)}.");
    }

    public static void StartsWith(
        string expectedStartString,
        string? actualString,
        StringComparison comparisonType = StringComparison.Ordinal
    )
    {
        if (actualString is null || !actualString.StartsWith(expectedStartString, comparisonType))
            Fail($"Expected string to start with {Format(expectedStartString)} but found {Format(actualString)}.");
    }

    public static void EndsWith(
        string expectedEndString,
        string? actualString,
        StringComparison comparisonType = StringComparison.Ordinal
    )
    {
        if (actualString is null || !actualString.EndsWith(expectedEndString, comparisonType))
            Fail($"Expected string to end with {Format(expectedEndString)} but found {Format(actualString)}.");
    }

    public static TException Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            if (ex.GetType() == typeof(TException))
                return (TException)ex;

            Fail($"Expected exception {typeof(TException)} but found {ex.GetType()}.");
        }

        Fail($"Expected exception {typeof(TException)} but no exception was thrown.");
        return null!;
    }

    public static TException Throws<TException>(Func<object?> action)
        where TException : Exception
    {
        try
        {
            _ = action();
        }
        catch (Exception ex)
        {
            if (ex.GetType() == typeof(TException))
                return (TException)ex;

            Fail($"Expected exception {typeof(TException)} but found {ex.GetType()}.");
        }

        Fail($"Expected exception {typeof(TException)} but no exception was thrown.");
        return null!;
    }

    public static async Task<TException> ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex.GetType() == typeof(TException))
                return (TException)ex;

            Fail($"Expected exception {typeof(TException)} but found {ex.GetType()}.");
        }

        Fail($"Expected exception {typeof(TException)} but no exception was thrown.");
        return null!;
    }

    public static void Collection<T>(IEnumerable<T> collection, params Action<T>[] inspectors)
    {
        var items = collection.ToArray();
        Equal(inspectors.Length, items.Length);

        for (var i = 0; i < inspectors.Length; i++)
        {
            inspectors[i](items[i]);
        }
    }

    [DoesNotReturn]
    public static void Fail(string? message = null) => throw new Exception(message ?? "Assertion failed.");

    static bool TryCompareEnumerables<T>(T expected, T actual, out string? message)
    {
        if (expected is null || actual is null || expected is string || actual is string)
        {
            message = null;
            return false;
        }

        if (expected is not IEnumerable expectedEnumerable || actual is not IEnumerable actualEnumerable)
        {
            message = null;
            return false;
        }

        var expectedItems = expectedEnumerable.Cast<object?>().ToArray();
        var actualItems = actualEnumerable.Cast<object?>().ToArray();

        if (expectedItems.Length != actualItems.Length)
        {
            message = $"Expected sequence length {expectedItems.Length} but found {actualItems.Length}.";
            return true;
        }

        for (var i = 0; i < expectedItems.Length; i++)
        {
            if (!Equals(expectedItems[i], actualItems[i]))
            {
                message = $"Expected item {i} to be {Format(expectedItems[i])} but found {Format(actualItems[i])}.";
                return true;
            }
        }

        message = null;
        return true;
    }

    static void FailEqual<T>(T expected, T actual) =>
        Fail($"Expected {Format(expected)} but found {Format(actual)}.");

    static bool AreEquivalent(object? expected, object? actual)
    {
        if (Equals(expected, actual))
            return true;

        if (expected is string || actual is string)
            return false;

        var expectedType = expected?.GetType();
        if (
            expectedType is not null
            && actual is not null
            && expectedType == actual.GetType()
            && expectedType.IsGenericType
            && expectedType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
        )
        {
            return AreEquivalent(
                    expectedType.GetProperty("Key")!.GetValue(expected),
                    expectedType.GetProperty("Key")!.GetValue(actual)
                )
                && AreEquivalent(
                    expectedType.GetProperty("Value")!.GetValue(expected),
                    expectedType.GetProperty("Value")!.GetValue(actual)
                );
        }

        if (expected is IEnumerable expectedEnumerable && actual is IEnumerable actualEnumerable)
        {
            var expectedItems = expectedEnumerable.Cast<object?>().ToArray();
            var actualItems = actualEnumerable.Cast<object?>().ToArray();
            return expectedItems.Length == actualItems.Length
                && expectedItems.Zip(actualItems, AreEquivalent).All(result => result);
        }

        return false;
    }

    static string Format(object? value) => value is null ? "null" : $"'{value}'";
}
