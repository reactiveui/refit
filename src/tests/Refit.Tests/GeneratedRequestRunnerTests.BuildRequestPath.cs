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
            .That(() =>
            {
                var result = GeneratedRequestRunner.BuildRequestPath("/user/{id}", false);
                Console.WriteLine(result);
            }).Throws<ArgumentException>()
            .WithMessage("URL /user/{id} has parameter {id}, but no method parameter matches", StringComparison.Ordinal);

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
            yield return new(("/users/20/orders", usersIdOrders, false, Bind(usersIdOrders, "20")));
            yield return new(("/users/", usersId, false, Bind(usersId, (string?)null)));
            yield return new(("/foo/row_2/col_2", rowCol, false, Bind(rowCol, "2", "2")));
            yield return new(("/users/{id}", usersId, true, []));
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
}
