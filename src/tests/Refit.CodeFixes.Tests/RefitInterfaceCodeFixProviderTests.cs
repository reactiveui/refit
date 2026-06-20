// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.CodeFixes.Tests;

/// <summary>Tests for Refit interface code fixes.</summary>
public sealed class RefitInterfaceCodeFixProviderTests
{
    /// <summary>Verifies RF003 replaces backslashes in route literals.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RouteBackslashFixUsesForwardSlashes()
    {
        var fixedSource = await CodeFixFixture.ApplyFirstFix(
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitCodeFixTest;

            public interface IGeneratedClient
            {
                [Get("/bad\\route")]
                Task<string> BadRoute();
            }
            """,
            "RF003");

        await Assert.That(fixedSource).Contains("[Get(\"/bad/route\")]");
    }

    /// <summary>Verifies RF005 changes HeaderCollection parameters to the supported dictionary type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HeaderCollectionFixUsesSupportedDictionaryType()
    {
        var fixedSource = await CodeFixFixture.ApplyFirstFix(
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitCodeFixTest;

            public interface IGeneratedClient
            {
                [Get("/headers")]
                Task<string> Headers([HeaderCollection] IReadOnlyDictionary<string, string> headers);
            }
            """,
            "RF005");

        await Assert.That(fixedSource)
            .Contains("global::System.Collections.Generic.IDictionary<string, string> headers");
    }
}
