// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies the query-string parser that seeds the reflection builder's query entries.</summary>
public sealed class ParseQueryStringIntoTests
{
    /// <summary>Verifies a null or empty query string adds nothing.</summary>
    /// <param name="queryString">The query string to parse.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task NullOrEmptyQueryStringAddsNothing(string? queryString)
    {
        List<QueryParameterEntry>? entries = null;

        RequestBuilderImplementation.ParseQueryStringInto(queryString, ref entries);

        await Assert.That(entries).IsNull();
    }

    /// <summary>Verifies a query string is parsed with or without its leading question mark.</summary>
    /// <param name="queryString">The query string to parse.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [Arguments("?a=1")]
    [Arguments("a=1")]
    public async Task LeadingQuestionMarkIsOptional(string queryString)
    {
        List<QueryParameterEntry>? entries = null;

        RequestBuilderImplementation.ParseQueryStringInto(queryString, ref entries);

        await Assert.That(entries!.Count).IsEqualTo(1);
        await Assert.That(entries[0].Key).IsEqualTo("a");
        await Assert.That(entries[0].Value).IsEqualTo("1");
    }
}
