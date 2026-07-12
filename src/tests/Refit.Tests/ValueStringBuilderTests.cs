// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Tests for allocation-conscious internal helper types.</summary>
public sealed class ValueStringBuilderTests
{
    /// <summary>A multi-item sample sequence used to exercise the many-element peek branch.</summary>
    private static readonly int[] MultiItemSequence = [1, 2];

    /// <summary>Verifies common append and insert operations on a stack-backed builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StackBackedBuilderSupportsAppendInsertAndIndexer()
    {
        var result = BuildInsertedString();

        await Assert.That(result).IsEqualTo("abBXXcde!!!");
    }

    /// <summary>Verifies span APIs and growth from the initial stack buffer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BuilderGrowsAndExposesRequestedSpans()
    {
        const int expectedTextLength = 6;
        var (text, length, capacity, suffix, middle, terminator) = BuildSpanSummary();

        await Assert.That(text).IsEqualTo("abcdef");
        await Assert.That(length).IsEqualTo(expectedTextLength);
        await Assert.That(capacity).IsGreaterThanOrEqualTo(expectedTextLength);
        await Assert.That(suffix).IsEqualTo("cdef");
        await Assert.That(middle).IsEqualTo("bcd");
        await Assert.That(terminator).IsEqualTo('\0');
    }

    /// <summary>Verifies copying succeeds when the destination has enough room.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TryCopyToCopiesWhenDestinationHasEnoughRoom()
    {
        const int expectedCharsWritten = 4;
        var (success, charsWritten, text) = TryCopyToLargeDestination();

        await Assert.That(success).IsTrue();
        await Assert.That(charsWritten).IsEqualTo(expectedCharsWritten);
        await Assert.That(text).IsEqualTo("copy");
    }

    /// <summary>Verifies copying reports failure when the destination is too small.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TryCopyToFailsWhenDestinationIsTooSmall()
    {
        var (success, charsWritten) = TryCopyToSmallDestination();

        await Assert.That(success).IsFalse();
        await Assert.That(charsWritten).IsEqualTo(0);
    }

    /// <summary>Verifies pooled construction, explicit capacity, and no-op null appends.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PooledBuilderSupportsCapacityAndNullNoOps()
    {
        const int minimumCapacity = 2;
        var (text, capacity) = BuildWithPooledInitialCapacity();

        await Assert.That(text).IsEqualTo("z");
        await Assert.That(capacity).IsGreaterThanOrEqualTo(minimumCapacity);
    }

    /// <summary>Verifies less common growth and span termination paths.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BuilderCoversGrowthAndTerminationBranches()
    {
        const int expectedTextLength = 13;
        var (text, length, terminated, first) = BuildThroughGrowthBranches();

        await Assert.That(text).IsEqualTo("yyxbcdefghijj");
        await Assert.That(length).IsEqualTo(expectedTextLength);
        await Assert.That(terminated).IsEqualTo('\0');
        await Assert.That(first).IsEqualTo('y');
    }

    /// <summary>Verifies each append and insert overload grows when its current buffer is full.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BuilderGrowthBranchesAreCoveredPerOverload()
    {
        var text = BuildWithEveryGrowthOverload();

        await Assert.That(text).IsEqualTo("bba|abc|ab|abb|abc|ab");
    }

    /// <summary>Verifies the enumerable peek helper distinguishes empty, single, and multi-item sequences.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TryGetSingleReportsEmptySingleAndMany()
    {
        const int singleSampleValue = 42;
        var emptyState = Array.Empty<int>().TryGetSingle(out var emptyValue);
        var singleState = new[] { singleSampleValue }.TryGetSingle(out var singleValue);
        var manyState = MultiItemSequence.TryGetSingle(out var manyValue);

        await Assert.That(emptyState).IsEqualTo(EnumerablePeek.Empty);
        await Assert.That(emptyValue).IsEqualTo(0);
        await Assert.That(singleState).IsEqualTo(EnumerablePeek.Single);
        await Assert.That(singleValue).IsEqualTo(singleSampleValue);
        await Assert.That(manyState).IsEqualTo(EnumerablePeek.Many);
        await Assert.That(manyValue).IsEqualTo(0);
    }

    /// <summary>Verifies request-execution option value equality.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestExecutionOptionsCompareAllFields()
    {
        var value = new RequestExecutionOptions(true, false, true, false);
        var same = new RequestExecutionOptions(true, false, true, false);
        var differentApiResponse = new RequestExecutionOptions(false, false, true, false);
        var differentDispose = new RequestExecutionOptions(true, true, true, false);
        var differentBuffer = new RequestExecutionOptions(true, false, false, false);
        var differentAuthorization = new RequestExecutionOptions(true, false, true, true);

        await Assert.That(value == same).IsTrue();
        await Assert.That(value != same).IsFalse();
        await Assert.That(value.Equals((object)same)).IsTrue();
        await Assert.That(value.Equals("not-options")).IsFalse();
        await Assert.That(value.GetHashCode()).IsEqualTo(same.GetHashCode());
        await Assert.That(value.Equals(differentApiResponse)).IsFalse();
        await Assert.That(value.Equals(differentDispose)).IsFalse();
        await Assert.That(value.Equals(differentBuffer)).IsFalse();
        await Assert.That(value.Equals(differentAuthorization)).IsFalse();
    }

    /// <summary>Verifies closed generic method keys compare method definitions and type arguments.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CloseGenericMethodKeyComparesMethodAndTypes()
    {
        var openMethod = typeof(IGenericMethodKeyFixture).GetMethod(nameof(IGenericMethodKeyFixture.GenericMethod))!;
        var otherOpenMethod = typeof(IGenericMethodKeyFixture).GetMethod(nameof(IGenericMethodKeyFixture.OtherGenericMethod))!;
        var value = new CloseGenericMethodKey(openMethod, [typeof(string), typeof(int)]);
        var same = new CloseGenericMethodKey(openMethod, [typeof(string), typeof(int)]);
        var differentMethod = new CloseGenericMethodKey(otherOpenMethod, [typeof(string), typeof(int)]);
        var differentLength = new CloseGenericMethodKey(openMethod, [typeof(string)]);
        var differentType = new CloseGenericMethodKey(openMethod, [typeof(string), typeof(long)]);

        await Assert.That(value.Equals(same)).IsTrue();
        await Assert.That(value.Equals((object)same)).IsTrue();
        await Assert.That(value.Equals("not-key")).IsFalse();
        await Assert.That(value.GetHashCode()).IsEqualTo(same.GetHashCode());
        await Assert.That(value.Equals(differentMethod)).IsFalse();
        await Assert.That(value.Equals(differentLength)).IsFalse();
        await Assert.That(value.Equals(differentType)).IsFalse();
    }

    /// <summary>Builds a string using append, insert, span, and indexer operations.</summary>
    /// <returns>The built string.</returns>
    private static string BuildInsertedString()
    {
        const int insertPosition = 2;
        const int repeatCount = 2;
        const int exclamationCount = 3;
        var builder = new ValueStringBuilder(stackalloc char[4]);
        try
        {
            builder.Append('a');
            builder.Append("b");
            builder.Append("cd".AsSpan());
            builder.AppendSpan(1)[0] = 'e';
            builder[1] = 'B';
            builder.Insert(insertPosition, 'X', repeatCount);
            builder.Insert(1, "b");
            builder.Append('!', exclamationCount);
            return builder.ToString();
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>Builds a string and captures span slices plus the null terminator.</summary>
    /// <returns>A summary of the builder state before disposal.</returns>
    private static (string Text, int Length, int Capacity, string Suffix, string Middle, char Terminator) BuildSpanSummary()
    {
        const int suffixStart = 2;
        const int middleLength = 3;
        var builder = new ValueStringBuilder(stackalloc char[2]);
        try
        {
            builder.EnsureCapacity(1);
            builder.Append("ab");
            builder.Append("cdef");
            _ = builder.GetPinnableReference(terminate: true);

            var text = builder.AsSpan().ToString();
            var suffix = builder.AsSpan(suffixStart).ToString();
            var middle = builder.AsSpan(1, middleLength).ToString();
            var terminator = builder.RawChars[builder.Length];
            var length = builder.Length;
            var capacity = builder.Capacity;

            return (text, length, capacity, suffix, middle, terminator);
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>Copies builder contents into a destination large enough to hold them.</summary>
    /// <returns>The copy result and copied text.</returns>
    private static (bool Success, int CharsWritten, string Text) TryCopyToLargeDestination()
    {
        var builder = new ValueStringBuilder(stackalloc char[4]);
        try
        {
            builder.Append("copy");
            Span<char> destination = stackalloc char[8];

            var success = builder.TryCopyTo(destination, out var charsWritten);

            return (success, charsWritten, destination[..charsWritten].ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>Attempts to copy builder contents into a destination that is too small.</summary>
    /// <returns>The copy result.</returns>
    private static (bool Success, int CharsWritten) TryCopyToSmallDestination()
    {
        var builder = new ValueStringBuilder(stackalloc char[4]);
        try
        {
            builder.Append("copy");
            Span<char> destination = stackalloc char[2];

            var success = builder.TryCopyTo(destination, out var charsWritten);

            return (success, charsWritten);
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>Builds a short string using a pooled initial capacity.</summary>
    /// <returns>The built text and capacity.</returns>
    private static (string Text, int Capacity) BuildWithPooledInitialCapacity()
    {
        const int initialCapacity = 2;
        var builder = new ValueStringBuilder(initialCapacity);
        try
        {
            builder.Append(null);
            builder.Insert(0, null);
            builder.Append('z');
            var capacity = builder.Capacity;
            var text = builder.ToString();

            return (text, capacity);
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>Builds a string through growth paths that are uncommon in request formatting.</summary>
    /// <returns>The built text, length, terminator, and first character.</returns>
    private static (string Text, int Length, char Terminator, char First) BuildThroughGrowthBranches()
    {
        const int ensuredCapacity = 4;
        const int repeatCount = 2;
        const int insertPosition = 3;
        var builder = new ValueStringBuilder(stackalloc char[1]);
        try
        {
            builder.Length = 0;
            builder.EnsureCapacity(ensuredCapacity);
            builder.Append('x');
            builder.Insert(0, 'y', repeatCount);
            builder.Insert(insertPosition, "bcdef");
            builder.Append("ghij".AsSpan());
            builder.Append('k');
            builder.Length--;
            builder.AppendSpan(1)[0] = 'j';

            var first = builder.GetPinnableReference();
            _ = builder.AsSpan(terminate: true);
            var length = builder.Length;
            var terminated = builder.RawChars[length];
            var text = builder.AsSpan().ToString();

            return (text, length, terminated, first);
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>Builds strings through the grow branch of each mutating overload.</summary>
    /// <returns>The combined text.</returns>
    private static string BuildWithEveryGrowthOverload()
    {
        var insertChars = BuildInsertCharsGrowth();
        var insertString = BuildInsertStringGrowth();
        var appendChar = BuildAppendCharGrowth();
        var appendRepeatedChar = BuildAppendRepeatedCharGrowth();
        var appendSpan = BuildAppendSpanGrowth();
        var pooled = BuildPooledGrowth();

        return string.Join('|', insertChars, insertString, appendChar, appendRepeatedChar, appendSpan, pooled);
    }

    /// <summary>Builds through repeated-character insert growth.</summary>
    /// <returns>The built text.</returns>
    private static string BuildInsertCharsGrowth()
    {
        const int repeatCount = 2;
        var builder = new ValueStringBuilder(stackalloc char[1]);
        try
        {
            builder.Append('a');
            builder.Insert(0, 'b', repeatCount);
            return builder.ToString();
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>Builds through string insert growth.</summary>
    /// <returns>The built text.</returns>
    private static string BuildInsertStringGrowth()
    {
        var builder = new ValueStringBuilder(stackalloc char[1]);
        try
        {
            builder.Append('a');
            builder.Insert(1, "bc");
            return builder.ToString();
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>Builds through single-character append growth.</summary>
    /// <returns>The built text.</returns>
    private static string BuildAppendCharGrowth()
    {
        var builder = new ValueStringBuilder(stackalloc char[1]);
        try
        {
            builder.Append('a');
            builder.Append('b');
            return builder.ToString();
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>Builds through repeated-character append growth.</summary>
    /// <returns>The built text.</returns>
    private static string BuildAppendRepeatedCharGrowth()
    {
        const int repeatCount = 2;
        var builder = new ValueStringBuilder(stackalloc char[1]);
        try
        {
            builder.Append('a');
            builder.Append('b', repeatCount);
            return builder.ToString();
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>Builds through span append growth.</summary>
    /// <returns>The built text.</returns>
    private static string BuildAppendSpanGrowth()
    {
        var builder = new ValueStringBuilder(stackalloc char[1]);
        try
        {
            builder.Append('a');
            builder.Append("bc".AsSpan());
            return builder.ToString();
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>Builds through pooled-buffer growth to return an old rented array.</summary>
    /// <returns>The built text.</returns>
    private static string BuildPooledGrowth()
    {
        var builder = new ValueStringBuilder(1);
        try
        {
            builder.Append('a');
            builder.Append('b');
            return builder.ToString();
        }
        finally
        {
            builder.Dispose();
        }
    }
}
