// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Analyzers.Tests;

/// <summary>Tests for Refit interface contract diagnostics.</summary>
public sealed class RefitInterfaceAnalyzerTests
{
    /// <summary>Verifies request shape diagnostics are reported outside the generator path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReportsInvalidRequestShapeDiagnostics()
    {
        var diagnostics = await AnalyzerFixture.RunForBody(
            """
            [Get("/bad\\route")]
            Task<string> BadRoute();

            [Get("/tokens")]
            Task<string> MultipleTokens(CancellationToken first, CancellationToken second);

            [Get("/headers")]
            Task<string> InvalidHeaders([HeaderCollection] IDictionary<string, object> headers);
            """);

        var diagnosticIds = diagnostics.Select(diagnostic => diagnostic.Id).ToArray();

        await Assert.That(diagnosticIds).Contains("RF003");
        await Assert.That(diagnosticIds).Contains("RF004");
        await Assert.That(diagnosticIds).Contains("RF005");
    }

    /// <summary>Verifies non-Refit members on Refit interfaces are reported.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReportsDirectNonRefitMembers()
    {
        var diagnostics = await AnalyzerFixture.RunForBody(
            """
            [Get("/users")]
            Task<string> Get();

            void NonRefitMethod();
            """);

        await Assert.That(diagnostics.Select(diagnostic => diagnostic.Id))
            .Contains("RF001");
    }

    /// <summary>Verifies inherited non-Refit members on Refit interfaces are reported.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReportsInheritedNonRefitMembers()
    {
        var diagnostics = await AnalyzerFixture.Run(
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitAnalyzerTest;

            public interface IBaseInterface
            {
                void NonRefitMethod();
            }

            public interface IGeneratedClient : IBaseInterface
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);

        await Assert.That(diagnostics.Select(diagnostic => diagnostic.Id))
            .Contains("RF001");
    }

    /// <summary>Verifies IDisposable inheritance does not produce RF001.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DoesNotReportDisposeMethod()
    {
        var diagnostics = await AnalyzerFixture.Run(
            """
            using System;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitAnalyzerTest;

            public interface IGeneratedClient : IDisposable
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);

        await Assert.That(diagnostics.Select(diagnostic => diagnostic.Id))
            .DoesNotContain("RF001");
    }
}
