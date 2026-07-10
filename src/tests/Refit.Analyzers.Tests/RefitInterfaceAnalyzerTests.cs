// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Analyzers.Tests;

/// <summary>Tests for Refit interface contract diagnostics.</summary>
public sealed class RefitInterfaceAnalyzerTests
{
    /// <summary>The diagnostic identifier for non-Refit interface members.</summary>
    private const string NonRefitMemberDiagnosticId = "RF001";

    /// <summary>Verifies analysis exits when the compilation does not reference Refit.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DoesNotRunWithoutRefitReference()
    {
        var diagnostics = await AnalyzerFixture.RunWithoutRefitReference(
            """
            public interface IGeneratedClient
            {
            }
            """);

        await Assert.That(diagnostics).IsEmpty();
    }

    /// <summary>Verifies non-interface named types are ignored.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IgnoresNonInterfaceTypes()
    {
        var diagnostics = await AnalyzerFixture.Run(
            """
            using Refit;

            namespace RefitAnalyzerTest;

            public sealed class GeneratedClient
            {
            }
            """);

        await Assert.That(diagnostics).IsEmpty();
    }

    /// <summary>Verifies interfaces without Refit methods are ignored.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IgnoresInterfacesWithoutRefitMethods()
    {
        var diagnostics = await AnalyzerFixture.RunForBody(
            """
            void NonRefitMethod();
            """);

        await Assert.That(diagnostics).IsEmpty();
    }

    /// <summary>Verifies request shape diagnostics are reported outside the generator path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReportsInvalidRequestShapeDiagnostics()
    {
        var diagnostics = await AnalyzerFixture.RunForBody(
            """
            [Obsolete]
            [Get("/bad\\route")]
            Task<string> BadRoute();

            [Get("/tokens")]
            Task<string> MultipleTokens(CancellationToken first, CancellationToken second);

            [Get("/headers")]
            Task<string> InvalidHeaders([HeaderCollection] IDictionary<string, object> headers);
            """);

        var diagnosticIds = diagnostics.Select(static diagnostic => diagnostic.Id).ToArray();

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

        await Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id))
            .Contains(NonRefitMemberDiagnosticId);
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

        await Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id))
            .Contains(NonRefitMemberDiagnosticId);
    }

    /// <summary>Verifies interfaces with inherited Refit methods still validate their own non-Refit members.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReportsNonRefitMembersWhenRefitMethodIsInherited()
    {
        var diagnostics = await AnalyzerFixture.Run(
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitAnalyzerTest;

            public interface IBaseInterface
            {
                [Get("/users")]
                Task<string> Get();
            }

            public interface IGeneratedClient : IBaseInterface
            {
                void NonRefitMethod();
            }
            """);

        await Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id))
            .Contains(NonRefitMemberDiagnosticId);
    }

    /// <summary>Verifies HTTP path extraction handles missing attribute data.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetHttpPathReturnsEmptyForMissingAttribute() =>
        await Assert.That(RefitInterfaceAnalyzer.GetHttpPath(null)).IsEqualTo(string.Empty);

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

        await Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id))
            .DoesNotContain(NonRefitMemberDiagnosticId);
    }

    /// <summary>Verifies custom HTTP method attributes without a string path literal are treated as empty.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CustomHttpMethodAttributesWithoutStringPathAreTreatedAsEmpty()
    {
        var diagnostics = await AnalyzerFixture.Run(
            """
            using System.Net.Http;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitAnalyzerTest;

            public sealed class NoPathAttribute : HttpMethodAttribute
            {
                public NoPathAttribute() : base("/x") { }
                public override HttpMethod Method => HttpMethod.Get;
            }

            public sealed class IntPathAttribute : HttpMethodAttribute
            {
                public IntPathAttribute(int code) : base("/x") { }
                public override HttpMethod Method => HttpMethod.Get;
            }

            public interface IGeneratedClient
            {
                [NoPath]
                Task<string> NoArguments();

                [IntPath(5)]
                Task<string> NonStringArgument();
            }
            """);

        await Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id))
            .DoesNotContain("RF003");
    }

    /// <summary>Verifies properties and their accessors on a Refit interface are ignored.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PropertiesAndAccessorsAreIgnored()
    {
        var diagnostics = await AnalyzerFixture.RunForBody(
            """
            [Get("/users")]
            Task<string> Get();

            string Name { get; set; }
            """);

        await Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id))
            .DoesNotContain(NonRefitMemberDiagnosticId);
    }

    /// <summary>Verifies static and default interface methods are not treated as emittable non-Refit members.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StaticAndDefaultMethodsAreNotEmittable()
    {
        var diagnostics = await AnalyzerFixture.RunForBody(
            """
            [Get("/users")]
            Task<string> Get();

            static string Helper() => string.Empty;

            void DefaultMethod() { }
            """);

        await Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id))
            .DoesNotContain(NonRefitMemberDiagnosticId);
    }

    /// <summary>Verifies inherited non-method members are ignored when detecting inherited Refit methods.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InheritedNonMethodMembersAreIgnored()
    {
        var diagnostics = await AnalyzerFixture.Run(
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitAnalyzerTest;

            public interface IBaseWithProperty
            {
                string SomeProperty { get; }

                [Get("/base")]
                Task<string> BaseGet();
            }

            public interface IGeneratedClient : IBaseWithProperty
            {
                [Get("/derived")]
                Task<string> DerivedGet();
            }
            """);

        await Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id))
            .DoesNotContain(NonRefitMemberDiagnosticId);
    }

    /// <summary>Verifies nullable, non-cancellation, and generic parameter types are classified correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CancellationTokenClassificationHandlesNullableAndGenericParameters()
    {
        var diagnostics = await AnalyzerFixture.RunForBody(
            """
            [Get("/a")]
            Task<string> NullableToken(CancellationToken? token);

            [Get("/b")]
            Task<string> NullableValue(int? value);

            [Get("/c")]
            Task<string> Generic<T>(T value);
            """);

        await Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id))
            .DoesNotContain("RF004");
    }

    /// <summary>Verifies parameters carrying a non-header-collection attribute are handled.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ParameterWithNonHeaderCollectionAttributeIsIgnored()
    {
        var diagnostics = await AnalyzerFixture.RunForBody(
            """
            [Get("/users")]
            Task<string> Get([Query] string filter);
            """);

        await Assert.That(diagnostics.Select(static diagnostic => diagnostic.Id))
            .DoesNotContain("RF005");
    }
}
