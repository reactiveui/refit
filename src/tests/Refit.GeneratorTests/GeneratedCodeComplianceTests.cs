// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.GeneratorTests;

/// <summary>Tests that generated source follows the repository generated-code conventions.</summary>
public class GeneratedCodeComplianceTests
{
    /// <summary>The warning level used by compliance compilation tests.</summary>
    private const int StrictWarningLevel = 9999;

    /// <summary>The generated implementation hint name used by compatibility tests.</summary>
    private const string GeneratedClientHintName = "IGeneratedClient.g.cs";

    /// <summary>The <c>#nullable</c> directive that must be absent from C# 7.3-targeted generated source.</summary>
    private const string NullableDirective = "#nullable";

    /// <summary>Verifies generated source compiles for projects using C# 7.3.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedSourcesCompileWithCSharp73Baseline()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest
            {
                /// <summary>Generated client test interface.</summary>
                public interface IGeneratedClient
                {
                    /// <summary>Gets a generated value.</summary>
                    /// <param name="user">The user name.</param>
                    /// <returns>The generated value.</returns>
                    [Get("/users/{user}")]
                    Task<string> Get(string user);
                }
            }
            """;
        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true, LanguageVersion.CSharp7_3);

        await Assert.That(GetCompilerErrors(result.OutputCompilation)).IsEqualTo(string.Empty);
        foreach (var generatedSource in result.GeneratedSources)
        {
            await Assert.That(generatedSource.Value).DoesNotContain(NullableDirective);
            await Assert.That(generatedSource.Value).DoesNotContain("static (");
        }
    }

    /// <summary>Verifies the unrolled form-url-encoded fast path compiles down to the C# 7.3 baseline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedFormBodyUnrollCompilesWithCSharp73Baseline()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest
            {
                /// <summary>A scalar form body.</summary>
                public class LoginForm
                {
                    /// <summary>Gets or sets the user name.</summary>
                    public string UserName { get; set; }

                    /// <summary>Gets or sets the age.</summary>
                    public int Age { get; set; }
                }

                /// <summary>Generated client test interface.</summary>
                public interface IGeneratedClient
                {
                    /// <summary>Posts a scalar form body.</summary>
                    /// <param name="form">The form body.</param>
                    /// <returns>A task representing the request.</returns>
                    [Post("/login")]
                    Task Login([Body(BodySerializationMethod.UrlEncoded)] LoginForm form);
                }
            }
            """;
        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true, LanguageVersion.CSharp7_3);

        // A stray nullable annotation ('string?') or C# 9 pattern would be a compile error at the 7.3 baseline.
        await Assert.That(GetCompilerErrors(result.OutputCompilation)).IsEqualTo(string.Empty);

        var generated = result.GeneratedSources[GeneratedClientHintName];
        await Assert.That(generated).DoesNotContain(NullableDirective);
        await Assert.That(generated).DoesNotContain(".Add(new(");
        await Assert.That(generated).Contains("new global::System.Collections.Generic.KeyValuePair<string, string>(");
        await Assert.That(generated).Contains(" != null");
    }

    /// <summary>Verifies the form-field descriptor path (collection body) compiles down to the C# 7.3 baseline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedFormDescriptorCompilesWithCSharp73Baseline()
    {
        const string source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest
            {
                /// <summary>A form body with a collection field, which uses the descriptor path.</summary>
                public class RolesForm
                {
                    /// <summary>Gets or sets the user name.</summary>
                    public string UserName { get; set; }

                    /// <summary>Gets or sets the roles.</summary>
                    [Query(CollectionFormat.Multi)]
                    public List<string> Roles { get; set; }
                }

                /// <summary>Generated client test interface.</summary>
                public interface IGeneratedClient
                {
                    /// <summary>Posts a form with a collection field.</summary>
                    /// <param name="form">The form body.</param>
                    /// <returns>A task representing the request.</returns>
                    [Post("/roles")]
                    Task Post([Body(BodySerializationMethod.UrlEncoded)] RolesForm form);
                }
            }
            """;
        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true, LanguageVersion.CSharp7_3);

        // The 'static' lambda (C# 9) and 'object?' cast (C# 8) in the descriptor getter must degrade at the 7.3 baseline.
        await Assert.That(GetCompilerErrors(result.OutputCompilation)).IsEqualTo(string.Empty);

        var generated = result.GeneratedSources[GeneratedClientHintName];
        await Assert.That(generated).DoesNotContain(NullableDirective);
        await Assert.That(generated).DoesNotContain("static body =>");
        await Assert.That(generated).DoesNotContain("(object?)");
        await Assert.That(generated).Contains("global::Refit.FormField<");
        await Assert.That(generated).Contains("body => (object)body.@UserName");
    }

    /// <summary>Verifies generated source emits nullable annotations when the consumer language version supports them.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedSourcesEmitNullableAnnotationsWhenSupported()
    {
        const string source =
            """
            #nullable enable
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest
            {
                /// <summary>Generated client test interface.</summary>
                public interface IGeneratedClient
                {
                    /// <summary>Gets a generated value.</summary>
                    /// <param name="user">The user name.</param>
                    /// <returns>The generated value.</returns>
                    [Get("/users/{user}")]
                    Task<string?> Get(string? user);
                }
            }
            """;
        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true, LanguageVersion.CSharp8);

        await Assert.That(GetCompilerErrors(result.OutputCompilation)).IsEqualTo(string.Empty);
        var generatedClientSource = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(generatedClientSource).Contains("#nullable enable annotations");
        await Assert.That(generatedClientSource).Contains("global::Refit.IRequestBuilder?");
        await Assert.That(generatedClientSource).Contains("string? @user");
    }

    /// <summary>Verifies generated files carry analyzer-recognized generated-code markers.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedSourcesUseAnalyzerRecognizedMarkers()
    {
        const string generatedCodeAttribute = "global::System.CodeDom.Compiler.GeneratedCodeAttribute";
        var result = Fixture.RunGenerator(
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            /// <summary>Generated client test interface.</summary>
            public interface IGeneratedClient
            {
                /// <summary>Gets a generated value.</summary>
                /// <returns>The generated value.</returns>
                [Get("/users")]
                Task<string?> Get();
            }
            """,
            generatedRequestBuilding: null);

        await Assert.That(result.GeneratedSources).IsNotEmpty();
        foreach (var generatedSource in result.GeneratedSources)
        {
            await Assert.That(generatedSource.Value).StartsWith("// <auto-generated/>");
            await Assert.That(generatedSource.Value).Contains("#nullable enable annotations");
            await Assert.That(generatedSource.Value).Contains("#nullable disable warnings");
            await Assert.That(generatedSource.Value).DoesNotContain("#pragma warning disable");
        }

        await Assert.That(result.GeneratedSources[GeneratedClientHintName]).Contains(generatedCodeAttribute);
        await Assert.That(result.GeneratedSources["Generated.g.cs"]).Contains(generatedCodeAttribute);
        await Assert.That(result.GeneratedSources["PreserveAttribute.g.cs"]).Contains(generatedCodeAttribute);
    }

    /// <summary>Verifies generated source can be compiled again under strict warning settings.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedSourcesCompileUnderStrictWarnings()
    {
        const string source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            /// <summary>Generated client test interface.</summary>
            /// <typeparam name="T">The test value type.</typeparam>
            public interface IGeneratedClient<T>
                where T : class, IDisposable
            {
                /// <summary>Gets a generated value.</summary>
                /// <param name="id">The route identifier.</param>
                /// <param name="value">The test value.</param>
                /// <param name="cancellationToken">The cancellation token.</param>
                /// <returns>The generated value.</returns>
                [Get("/users/{id}")]
                Task<string?> Get([AliasAs("id")] int id, T value, CancellationToken cancellationToken);
            }
            """;
        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var syntaxTrees = new List<SyntaxTree>(result.GeneratedSources.Count + 1)
        {
            ParseComplianceSource(source, "Consumer.cs")
        };

        foreach (var generatedSource in result.GeneratedSources)
        {
            syntaxTrees.Add(ParseComplianceSource(generatedSource.Value, generatedSource.Key));
        }

        var compilation = Fixture.CreateLibrary([.. syntaxTrees]);
        compilation = compilation.WithOptions(
            compilation.Options
                .WithNullableContextOptions(NullableContextOptions.Enable)
                .WithWarningLevel(StrictWarningLevel)
                .WithSpecificDiagnosticOptions(
                    [
                        new("CS1591", ReportDiagnostic.Error),
                        new("CS8618", ReportDiagnostic.Error),
                        new("CS8625", ReportDiagnostic.Error)
                    ]));
        var diagnostics = compilation
            .GetDiagnostics()
            .Where(static diagnostic =>
                diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error
                && diagnostic.Id != "CS8019")
            .Select(static diagnostic => diagnostic.ToString());

        await Assert.That(string.Join(Environment.NewLine, diagnostics)).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies generated infrastructure fields do not collide with user interface members.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedInfrastructureFieldsAvoidInterfaceMemberNames()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            /// <summary>Generated client test interface.</summary>
            public interface IGeneratedClient
            {
                /// <summary>Gets the member that collides with the generated request-builder field name.</summary>
                string _requestBuilder { get; }

                /// <summary>Gets the member that collides with the first generated request-builder fallback field name.</summary>
                string _requestBuilder0 { get; }

                /// <summary>Gets the member that collides with the generated settings field name.</summary>
                string _settings { get; }

                /// <summary>Gets the member that collides with the first generated settings fallback field name.</summary>
                string _settings0 { get; }

                /// <summary>Gets the member that collides with the generated type-parameter cache field name.</summary>
                string ______typeParameters { get; }

                /// <summary>Gets the member that collides with the first generated type-parameter fallback field name.</summary>
                string ______typeParameters0 { get; }

                /// <summary>Gets a generated value.</summary>
                /// <param name="id">The route identifier.</param>
                /// <returns>The generated value.</returns>
                [Get("/users/{id}")]
                Task<string> Get(string id);
            }
            """;
        var result = Fixture.RunGenerator(source, generatedRequestBuilding: false);

        await Assert.That(GetCompilerErrors(result.OutputCompilation)).IsEqualTo(string.Empty);

        var generatedClientSource = result.GeneratedSources[GeneratedClientHintName];
        await Assert.That(generatedClientSource).Contains("private readonly global::Refit.IRequestBuilder? _requestBuilder1;");
        await Assert.That(generatedClientSource).Contains("private readonly global::Refit.RefitSettings _settings1;");
        await Assert.That(generatedClientSource).Contains("private static readonly global::System.Type[] ______typeParameters1");
    }

    /// <summary>Parses source with documentation diagnostics enabled.</summary>
    /// <param name="source">The source to parse.</param>
    /// <param name="path">The syntax tree path.</param>
    /// <returns>The parsed syntax tree.</returns>
    private static SyntaxTree ParseComplianceSource(string source, string path) =>
        CSharpSyntaxTree.ParseText(
            source,
            new(documentationMode: DocumentationMode.Diagnose),
            path);

    /// <summary>Gets compiler errors from a generated-output compilation.</summary>
    /// <param name="compilation">The compilation to inspect.</param>
    /// <returns>The formatted compiler errors.</returns>
    private static string GetCompilerErrors(Compilation compilation)
    {
        var diagnostics = compilation
            .GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(static diagnostic => diagnostic.ToString());

        return string.Join(Environment.NewLine, diagnostics);
    }
}
