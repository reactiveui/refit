// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Tests for building request paths with route parameters via <see cref="GeneratedRequestRunner"/>.</summary>
public partial class GeneratedRequestRunnerTests
{
    /// <summary>Verifies BuildRequestPath returns a path with substituted parameters.</summary>
    /// <param name="expectedResult">The expected result.</param>
    /// <param name="path">The templated path.</param>
    /// <param name="allowUnmatchedRouteParameters">Whether unmatched route parameters are supported.</param>
    /// <param name="uriParams">The URI parameters.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [InstanceMethodDataSource(typeof(GeneratedRequestRunnerTestsDataSources), nameof(GeneratedRequestRunnerTestsDataSources.BuildRequestPathReplacesParametersData))]
    public async Task BuildRequestPathReplacesParameters(string expectedResult, string path, bool allowUnmatchedRouteParameters, params ((int start, int end) location, string? value)[] uriParams)
    {
        var result = GeneratedRequestRunner.BuildRequestPath(path, allowUnmatchedRouteParameters, uriParams);

        await Assert.That(result).EqualTo(expectedResult);
    }

    /// <summary>Verifies BuildRequestPath fails when a parameter is not provided.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRequestPathFailsOnParameterNotFound() =>
        await Assert
            .That(static () =>
            {
                ((int startIdx, int endIdx) range, string? value)[] uriParams = [];
                _ = GeneratedRequestRunner.BuildRequestPath("/user/{id}", false, uriParams);
            })
            .Throws<ArgumentException>()
            .WithMessage("URL /user/{id} has parameter {id}, but no method parameter matches", StringComparison.Ordinal);

    /// <summary>Verifies the span-formattable overload writes an integer into the path without escaping.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRequestPathWritesIntegerWithoutEscaping()
    {
        const int start = 7;
        const int end = 11;
        const int id = 42;
        var result = GeneratedRequestRunner.BuildRequestPath("/users/{id}/posts", false, (start, end), id);

        await Assert.That(result).EqualTo("/users/42/posts");
    }

    /// <summary>Verifies the span-formattable overload renders a negative integer.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRequestPathWritesNegativeInteger()
    {
        const int start = 3;
        const int end = 6;
        const long value = -7;
        var result = GeneratedRequestRunner.BuildRequestPath("/n/{v}", false, (start, end), value);

        await Assert.That(result).EqualTo("/n/-7");
    }

#if NET10_0_OR_GREATER
    /// <summary>Verifies the escaping span-formattable overload percent-encodes reserved characters (net10+).</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRequestPathEscapesSpanFormattableValue()
    {
        const int start = 4;
        const int end = 10;
        const double value = 1e21;
        var result = GeneratedRequestRunner.BuildRequestPath("/at/{when}", false, (start, end), value, null);

        // 1e21 renders as "1E+21"; the '+' is URL-reserved and percent-encoded exactly as Uri.EscapeDataString would.
        await Assert.That(result).EqualTo("/at/1E%2B21");
    }
#endif

    /// <summary>Verifies the span-formattable overload escapes a value that cannot format into the stack buffer.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRequestPathEscapesUnformattableSpanValue()
    {
        const int start = 3;
        const int end = 6;
        var result = GeneratedRequestRunner.BuildRequestPath("/n/{v}", false, (start, end), new AlwaysUnformattableValue());

        await Assert.That(result).IsEqualTo("/n/a%2Fb");
    }

    /// <summary>Verifies the format-taking escaping overload falls back to the string escaper when the value cannot
    /// format into the stack buffer.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRequestPathEscapesUnformattableSpanValueWithFormatOverload()
    {
        const int start = 3;
        const int end = 6;
        var result = GeneratedRequestRunner.BuildRequestPath("/n/{v}", false, (start, end), new AlwaysUnformattableValue(), null);

        await Assert.That(result).IsEqualTo("/n/a%2Fb");
    }

    /// <summary>Verifies the pre-encoded overload returns the template for an empty parameter set.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRequestPathReturnsTemplateForEmptyPreEncodedParameters()
    {
        ((int startIdx, int endIdx) range, string? value, bool preEncoded)[] uriParams = [];

        var result = GeneratedRequestRunner.BuildRequestPath("/plain", true, uriParams);

        await Assert.That(result).IsEqualTo("/plain");
    }

    /// <summary>Provides test data for <see cref="GeneratedRequestRunnerTests"/>.</summary>
    internal static class GeneratedRequestRunnerTestsDataSources
    {
        /// <summary>Data source for the <see cref="BuildRequestPathReplacesParameters"/> test.</summary>
        /// <returns>Test data.</returns>
        internal static
            IEnumerable<TestDataRow<(string expectedResult, string path, bool allowUnmatchedRouteParameters, ((int start, int end) location
                ,
                string? value)[] uriParams)>> BuildRequestPathReplacesParametersData()
        {
            const string usersId = "/users/{id}";
            const string usersIdOrders = "/users/{id}/orders";
            const string rowCol = "/foo/row_{idx}/col_{idx}";

            yield return new(("/users/20", usersId, false, Bind(usersId, "20")));
            yield return new(("/users/20", usersId, true, Bind(usersId, "20")));
            yield return new(("/users/20/", $"{usersId}/", true, Bind(usersId, "20")));
            yield return new(("/users/20/foo{bar", $"{usersId}/foo{{bar", false, Bind(usersId, "20")));
            yield return new(("/users/20/foo}{bar", $"{usersId}/foo}}{{bar", false, Bind(usersId, "20")));
            yield return new(("/users/20/orders", usersIdOrders, false, Bind(usersIdOrders, "20")));
            yield return new(("/users/", usersId, false, Bind(usersId, (string?)null)));
            yield return new(("/foo/row_2/col_2", rowCol, false, Bind(rowCol, "2", "2")));
            yield return new(("/users/{id}", usersId, true, []));
            yield return new(("/users/%7B20%7D", usersId, true, Bind(usersId, "{20}")));
        }

        /// <summary>Builds parameter locations by scanning the template for <c>{placeholder}</c> spans.</summary>
        /// <param name="template">The templated path.</param>
        /// <param name="values">The value for each placeholder, in order.</param>
        /// <returns>The located parameters paired with their values.</returns>
        private static ((int start, int end) location, string? value)[] Bind(string template, params string?[] values)
        {
            var located = new List<((int start, int end) location, string? value)>();
            var search = 0;
            var index = 0;
            int open;
            while ((open = template.IndexOf('{', search)) >= 0)
            {
                var close = template.IndexOf('}', open) + 1;
                located.Add(((open, close), values[index]));
                search = close;
                index++;
            }

            return [.. located];
        }
    }

    /// <summary>A span-formattable value whose <see cref="ISpanFormattable.TryFormat"/> always fails.</summary>
    private sealed class AlwaysUnformattableValue : ISpanFormattable
    {
        /// <inheritdoc/>
        public string ToString(string? format, IFormatProvider? formatProvider) => "a/b";

        /// <inheritdoc/>
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            charsWritten = 0;
            return false;
        }
    }
}
