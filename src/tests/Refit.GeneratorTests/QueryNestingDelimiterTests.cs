// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Verifies the delimiter placed between nested query keys.</summary>
public sealed class QueryNestingDelimiterTests
{
    /// <summary>Verifies an explicit delimiter is used.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ExplicitDelimiterIsUsed()
    {
        var data = new RequestBodyParsing.QueryFormData("::", null, null, null, false, false, null);

        await Assert.That(QueryParameterParsing.ResolveNestingDelimiter(data)).IsEqualTo("::");
    }

    /// <summary>Verifies the default delimiter is used when none is declared.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task MissingDelimiterUsesTheDefault()
    {
        var data = new RequestBodyParsing.QueryFormData(null, null, null, null, false, false, null);

        await Assert.That(QueryParameterParsing.ResolveNestingDelimiter(data)).IsEqualTo(QueryParameterParsing.DefaultNestingDelimiter);
    }
}
