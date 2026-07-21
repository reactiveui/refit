// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.GeneratorTests;

/// <summary>Verifies the generator qualifies types reached through an <c>extern alias</c> and emits the directive,
/// so the generated code compiles for types that are not reachable via <c>global::</c> (issue #1101).</summary>
public sealed class ExternAliasTests
{
    /// <summary>The alias-qualified <c>Widget</c> type name the generator must emit into the generated code.</summary>
    private const string AliasedWidgetQualifiedName = "CompanyLib::Colliding.Widget";

    /// <summary>Source for a separate assembly, referenced only through the extern alias <c>CompanyLib</c>.</summary>
    private const string AliasedLibrarySource =
        "namespace Colliding { public sealed class Widget { public int Size { get; set; } } public enum Color { Red, Green } public sealed class Box<T> { public T Item { get; set; } } }";

    /// <summary>The extern alias directive the generator emits for the aliased library.</summary>
    private const string ExternAliasDirective = "extern alias CompanyLib;";

    /// <summary>Verifies a return type behind an extern alias is emitted as <c>alias::</c> with an <c>extern alias</c>
    /// directive, and the generated code compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExternAliasedReturnTypeGeneratesAliasQualifiedAndCompiles()
    {
        const string consumer =
            """
            extern alias CompanyLib;
            using System.Threading.Tasks;
            using Refit;

            namespace ConsumerNs;

            public interface IWidgetApi
            {
                [Get("/widget")]
                Task<CompanyLib::Colliding.Widget> GetWidget();
            }
            """;

        var generated = await RunAndAssertNoErrors(consumer);
        await Assert.That(generated).Contains(AliasedWidgetQualifiedName);
    }

    /// <summary>Verifies a body parameter type behind an extern alias is emitted as <c>alias::</c> and compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExternAliasedBodyParameterGeneratesAliasQualifiedAndCompiles()
    {
        const string consumer =
            """
            extern alias CompanyLib;
            using System.Threading.Tasks;
            using Refit;

            namespace ConsumerNs;

            public interface IWidgetApi
            {
                [Post("/widget")]
                Task Create([Body] CompanyLib::Colliding.Widget widget);
            }
            """;

        var generated = await RunAndAssertNoErrors(consumer);
        await Assert.That(generated).Contains(AliasedWidgetQualifiedName);
    }

    /// <summary>Verifies an extern-aliased enum flattened into the query string is emitted as <c>alias::</c> and compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExternAliasedQueryEnumGeneratesAliasQualifiedAndCompiles()
    {
        const string consumer =
            """
            extern alias CompanyLib;
            using System.Threading.Tasks;
            using Refit;

            namespace ConsumerNs;

            public interface IWidgetApi
            {
                [Get("/widget")]
                Task Find([Query] CompanyLib::Colliding.Color color);
            }
            """;

        var generated = await RunAndAssertNoErrors(consumer);
        await Assert.That(generated).Contains("CompanyLib::Colliding.Color");
    }

    /// <summary>Verifies an extern-aliased interface property type is emitted as <c>alias::</c> and compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExternAliasedInterfacePropertyGeneratesAliasQualifiedAndCompiles()
    {
        const string consumer =
            """
            extern alias CompanyLib;
            using System.Threading.Tasks;
            using Refit;

            namespace ConsumerNs;

            public interface IWidgetApi
            {
                CompanyLib::Colliding.Widget Config { get; }

                [Get("/widget")]
                Task<string> GetWidget();
            }
            """;

        var generated = await RunAndAssertNoErrors(consumer);
        await Assert.That(generated).Contains(AliasedWidgetQualifiedName);
    }

    /// <summary>Verifies an array of an extern-aliased element type qualifies each element as <c>alias::</c>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExternAliasedArrayBodyParameterGeneratesAliasQualifiedAndCompiles()
    {
        const string consumer =
            """
            extern alias CompanyLib;
            using System.Threading.Tasks;
            using Refit;

            namespace ConsumerNs;

            public interface IWidgetApi
            {
                [Post("/widgets")]
                Task Create([Body] CompanyLib::Colliding.Widget[] widgets);
            }
            """;

        var generated = await RunAndAssertNoErrors(consumer);
        await Assert.That(generated).Contains($"{AliasedWidgetQualifiedName}[]");
    }

    /// <summary>Verifies a nullable extern-aliased enum query parameter qualifies its underlying type as <c>alias::</c>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExternAliasedNullableEnumQueryGeneratesAliasQualifiedAndCompiles()
    {
        const string consumer =
            """
            extern alias CompanyLib;
            using System.Threading.Tasks;
            using Refit;

            namespace ConsumerNs;

            public interface IWidgetApi
            {
                [Get("/widget")]
                Task Find([Query] CompanyLib::Colliding.Color? color);
            }
            """;

        var generated = await RunAndAssertNoErrors(consumer);
        await Assert.That(generated).Contains("CompanyLib::Colliding.Color");
    }

    /// <summary>Verifies an extern-aliased generic type closed over a method type parameter renders the argument
    /// through the display fallback while still qualifying the aliased outer type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExternAliasedGenericOverTypeParameterGeneratesAliasQualified()
    {
        const string consumer =
            """
            extern alias CompanyLib;
            using System.Threading.Tasks;
            using Refit;

            namespace ConsumerNs;

            public interface IWidgetApi
            {
                [Post("/boxes")]
                Task Store<T>([Body] CompanyLib::Colliding.Box<T> box);
            }
            """;

        // The method is generic, so it falls back, but the parameter type is still alias-qualified during parsing.
        var aliasedLibrary = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(AliasedLibrarySource))
            .ToMetadataReference()
            .WithAliases(["CompanyLib"]);
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(consumer)).AddReferences(aliasedLibrary);
        var result = Fixture.RunGenerator(compilation, generatedRequestBuilding: true, false, null);
        var generated = string.Join("\n", result.GeneratedSources.Values);

        await Assert.That(generated).Contains("CompanyLib::Colliding.Box<");
    }

    /// <summary>Verifies a reference imported under the <c>global</c> alias stays reachable without qualification,
    /// so the generator emits the plain <c>global::</c> name rather than an extern alias.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GlobalAliasedReferenceGeneratesPlainQualifiedNameAndCompiles()
    {
        const string library =
            "namespace GlobalNs { public sealed class Parcel { public int Weight { get; set; } } }";
        const string consumer =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace ConsumerNs;

            public interface IParcelApi
            {
                [Post("/parcels")]
                Task Ship([Body] global::GlobalNs.Parcel parcel);
            }
            """;

        // Aliasing the reference as "global" merges it into the global namespace, so ResolveExternAlias must treat
        // it as reachable without an extern alias.
        var globalLibrary = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(library))
            .ToMetadataReference()
            .WithAliases(["global"]);
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(consumer)).AddReferences(globalLibrary);
        var result = Fixture.RunGenerator(compilation, generatedRequestBuilding: true, false, null);
        var generated = string.Join("\n", result.GeneratedSources.Values);

        var errors = result.OutputCompilation.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(static diagnostic => diagnostic.ToString())
            .ToArray();
        await Assert.That(errors).IsEmpty();
        await Assert.That(generated).DoesNotContain(ExternAliasDirective);
        await Assert.That(generated).Contains("global::GlobalNs.Parcel");
    }

    /// <summary>Runs the generator over a consumer that reaches the aliased library, asserting the emitted directive,
    /// the absence of any <c>global::Colliding</c> reference, and a clean compile.</summary>
    /// <param name="consumer">The consumer interface source referencing the aliased library.</param>
    /// <returns>The concatenation of every generated source, for content assertions.</returns>
    private static async Task<string> RunAndAssertNoErrors(string consumer)
    {
        // The Colliding types are reachable only through the extern alias "CompanyLib": if the generator emitted
        // global::Colliding.* the code would not compile, so the compile check below is the real assertion.
        var aliasedLibrary = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(AliasedLibrarySource))
            .ToMetadataReference()
            .WithAliases(["CompanyLib"]);

        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(consumer)).AddReferences(aliasedLibrary);
        var result = Fixture.RunGenerator(compilation, generatedRequestBuilding: true, false, null);

        // The interface stub file name varies (generic arity), so assert across every generated source.
        var generated = string.Join("\n", result.GeneratedSources.Values);
        await Assert.That(generated).Contains(ExternAliasDirective);
        await Assert.That(generated).DoesNotContain("global::Colliding");

        var errors = result.OutputCompilation.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(static diagnostic => diagnostic.ToString())
            .ToArray();
        await Assert.That(errors).IsEmpty();
        return generated;
    }
}
