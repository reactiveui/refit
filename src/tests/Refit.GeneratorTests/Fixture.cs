// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Helpers for compiling sources and verifying the Refit source generator output.</summary>
[UnconditionalSuppressMessage(
    "SingleFile",
    "IL3000:Avoid accessing Assembly file path when publishing as a single file",
    Justification = "Compiles generator inputs against on-disk assemblies; never run as a single-file app.")]
public static class Fixture
{
    /// <summary>The runtime data key containing framework assemblies available to the current test host.</summary>
    private const string TrustedPlatformAssemblies = "TRUSTED_PLATFORM_ASSEMBLIES";

    /// <summary>The metadata reference for the Refit assembly with documentation.</summary>
    private static readonly MetadataReference _refitAssembly = MetadataReference.CreateFromFile(
        typeof(GetAttribute).Assembly.Location,
        documentation: XmlDocumentationProvider.CreateFromFile(
            Path.ChangeExtension(typeof(GetAttribute).Assembly.Location, ".xml")));

    /// <summary>Types whose assemblies must be referenced during code generation.</summary>
    private static readonly Type[] _importantAssemblies =
    [
        typeof(Binder),
        typeof(GetAttribute),
        typeof(System.Reactive.Unit),
        typeof(Enumerable),
        typeof(Newtonsoft.Json.JsonConvert),
        typeof(TestAttribute),
        typeof(HttpContent),
        typeof(Attribute)
    ];

    /// <summary>Verifies generator output for an interface body snippet, ignoring non-interface results.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <returns>A task representing the verification.</returns>
    public static Task VerifyForBody(string body) => VerifyForBody(body, true);

    /// <summary>Verifies generator output for an interface body snippet.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <param name="ignoreNonInterfaces">Whether to ignore non-interface generated results.</param>
    /// <returns>A task representing the verification.</returns>
    public static Task VerifyForBody(string body, bool ignoreNonInterfaces)
    {
        var source =
            $$"""
              using System;
              using System.Collections.Generic;
              using System.Linq;
              using System.Net.Http;
              using System.Text;
              using System.Threading;
              using System.Threading.Tasks;
              using Refit;

              namespace RefitGeneratorTest;

              public interface IGeneratedClient
              {
              {{body}}
              }
              """;

        return VerifyGenerator(source, ignoreNonInterfaces);
    }

    /// <summary>Generates output for an interface body snippet and returns the requested generated file.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <param name="hintName">The generated source hint name.</param>
    /// <returns>The generated source text.</returns>
    public static string GenerateForBody(string body, string hintName) =>
        GenerateForBody(body, hintName, null);

    /// <summary>Generates output for an interface body snippet and returns the requested generated file.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <param name="hintName">The generated source hint name.</param>
    /// <param name="generatedRequestBuilding">Whether generated request construction is explicitly configured.</param>
    /// <returns>The generated source text.</returns>
    public static string GenerateForBody(string body, string hintName, bool? generatedRequestBuilding)
    {
        var source = BuildBodySource(body);

        return Generate(source, hintName, generatedRequestBuilding);
    }

    /// <summary>Generates output for an interface body snippet, compiles it, and returns error diagnostics.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <param name="generatedRequestBuilding">Whether generated request construction is explicitly configured.</param>
    /// <returns>The compiler and generator errors produced by the generated output.</returns>
    public static ImmutableArray<Diagnostic> GenerateErrorsForBody(
        string body,
        bool? generatedRequestBuilding)
    {
        var result = RunGenerator(BuildBodySource(body), generatedRequestBuilding);

        return
        [
            .. result.GeneratorDiagnostics
            .Concat(result.OutputCompilation.GetDiagnostics())
            .Where(IsGeneratedOutputError)
        ];
    }

    /// <summary>Runs the generator over an interface body snippet and returns the output compilation.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <param name="generatedRequestBuilding">Whether generated request construction is explicitly configured.</param>
    /// <returns>The generator result.</returns>
    public static GeneratorTestResult RunGeneratorForBody(
        string body,
        bool? generatedRequestBuilding) =>
        RunGeneratorForBody(body, generatedRequestBuilding, false);

    /// <summary>Runs the generator over an interface body snippet and returns the output compilation.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <param name="generatedRequestBuilding">Whether generated request construction is explicitly configured.</param>
    /// <param name="disableSourceGenerator">Whether source generation is disabled.</param>
    /// <returns>The generator result.</returns>
    public static GeneratorTestResult RunGeneratorForBody(
        string body,
        bool? generatedRequestBuilding,
        bool disableSourceGenerator) =>
        RunGenerator(BuildBodySource(body), generatedRequestBuilding, disableSourceGenerator);

    /// <summary>Runs the generator over the source and returns the output compilation.</summary>
    /// <param name="source">The source to compile and generate from.</param>
    /// <param name="generatedRequestBuilding">Whether generated request construction is explicitly configured.</param>
    /// <returns>The generator result.</returns>
    public static GeneratorTestResult RunGenerator(
        string source,
        bool? generatedRequestBuilding) =>
        RunGenerator(source, generatedRequestBuilding, false);

    /// <summary>Runs the generator over the source and returns the output compilation.</summary>
    /// <param name="source">The source to compile and generate from.</param>
    /// <param name="generatedRequestBuilding">Whether generated request construction is explicitly configured.</param>
    /// <param name="disableSourceGenerator">Whether source generation is disabled.</param>
    /// <returns>The generator result.</returns>
    public static GeneratorTestResult RunGenerator(
        string source,
        bool? generatedRequestBuilding,
        bool disableSourceGenerator)
    {
        var compilation = CreateLibrary(source);
        var generator = new InterfaceStubGeneratorV2();
        var driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            optionsProvider: new TestAnalyzerConfigOptionsProvider(
                generatedRequestBuilding,
                disableSourceGenerator));

        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var generatorDiagnostics);

        return new(driver, outputCompilation, generatorDiagnostics);
    }

    /// <summary>Emits a generated output compilation and loads it into a collectible assembly context.</summary>
    /// <param name="result">The generator result to emit.</param>
    /// <returns>The loaded assembly and load context.</returns>
    [RequiresUnreferencedCode("Loading an emitted test assembly from a stream requires runtime metadata for referenced types.")]
    public static (Assembly Assembly, CollectibleAssemblyLoadContext Context) EmitAndLoad(
        GeneratorTestResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        using var stream = new MemoryStream();
        var emitResult = result.OutputCompilation.Emit(stream);
        if (!emitResult.Success)
        {
            var errors = string.Join(
                Environment.NewLine,
                emitResult.Diagnostics
                    .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                    .Select(diagnostic => $"{diagnostic.Id}: {diagnostic.GetMessage()}"));
            throw new InvalidOperationException(
                $"Failed to emit generated compilation:{Environment.NewLine}{errors}");
        }

        stream.Position = 0;
        var context = new CollectibleAssemblyLoadContext();
        var assembly = context.LoadFromStream(stream);
        return (assembly, context);
    }

    /// <summary>Verifies generator output for type declarations within a namespace.</summary>
    /// <param name="declarations">The type declarations source.</param>
    /// <returns>A task representing the verification.</returns>
    public static Task VerifyForType(string declarations)
    {
        var source =
            $$"""
              using System;
              using System.Collections.Generic;
              using System.Linq;
              using System.Net.Http;
              using System.Text;
              using System.Threading;
              using System.Threading.Tasks;
              using Refit;

              namespace RefitGeneratorTest;

              {{declarations}}
              """;

        return VerifyGenerator(source);
    }

    /// <summary>Verifies generator output for top-level declarations.</summary>
    /// <param name="declarations">The declarations source.</param>
    /// <returns>A task representing the verification.</returns>
    public static Task VerifyForDeclaration(string declarations)
    {
        var source =
            $$"""
              using System;
              using System.Collections.Generic;
              using System.Linq;
              using System.Net.Http;
              using System.Text;
              using System.Threading;
              using System.Threading.Tasks;
              using Refit;

              {{declarations}}
              """;

        return VerifyGenerator(source);
    }

    /// <summary>Generates output for top-level declarations and returns the requested generated file.</summary>
    /// <param name="declarations">The declarations source.</param>
    /// <param name="hintName">The generated source hint name.</param>
    /// <param name="generatedRequestBuilding">Whether generated request construction is explicitly configured.</param>
    /// <returns>The generated source text.</returns>
    public static string GenerateForDeclaration(
        string declarations,
        string hintName,
        bool? generatedRequestBuilding)
    {
        var source =
            $$"""
              using System;
              using System.Collections.Generic;
              using System.Linq;
              using System.Net.Http;
              using System.Text;
              using System.Threading;
              using System.Threading.Tasks;
              using Refit;

              {{declarations}}
              """;

        return Generate(source, hintName, generatedRequestBuilding);
    }

    /// <summary>Creates a compilation from the given syntax trees with the required references.</summary>
    /// <param name="source">The syntax trees to compile.</param>
    /// <returns>The created compilation.</returns>
    public static CSharpCompilation CreateLibrary(params SyntaxTree[] source)
    {
        var referencePaths = new HashSet<string>(StringComparer.Ordinal);
        AddTrustedPlatformAssemblies(referencePaths);

        foreach (var assembly in GetAssemblyReferencesForCodegen())
        {
            if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            {
                referencePaths.Add(assembly.Location);
            }
        }

        var references = new List<MetadataReference>(referencePaths.Count + 1);
        foreach (var referencePath in referencePaths)
        {
            references.Add(MetadataReference.CreateFromFile(referencePath));
        }

        references.Add(_refitAssembly);
        return CSharpCompilation.Create(
            "compilation",
            source,
            references,
            new(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>Gets the assemblies referenced when compiling generated code.</summary>
    /// <returns>The distinct, non-dynamic assemblies to reference.</returns>
    private static Assembly[] GetAssemblyReferencesForCodegen() =>
        [
            .. AppDomain.CurrentDomain
                .GetAssemblies()
                .Concat(_importantAssemblies.Select(x => x.Assembly))
                .Distinct()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
        ];

    /// <summary>Creates a compilation by parsing the given source strings.</summary>
    /// <param name="source">The source strings to parse and compile.</param>
    /// <returns>The created compilation.</returns>
    private static CSharpCompilation CreateLibrary(params string[] source) =>
        CreateLibrary(source.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray());

    /// <summary>Adds runtime framework references used by Roslyn in-memory compilations.</summary>
    /// <param name="referencePaths">The reference path set to populate.</param>
    private static void AddTrustedPlatformAssemblies(HashSet<string> referencePaths)
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData(TrustedPlatformAssemblies);
        if (string.IsNullOrEmpty(trustedPlatformAssemblies))
        {
            return;
        }

        // CI hosts can type-forward BCL types, such as Uri, to implementation assemblies
        // that are not loaded yet. The trusted platform assembly list gives Roslyn the
        // complete runtime framework closure for generated-output compilation tests.
        foreach (var referencePath in trustedPlatformAssemblies.Split(Path.PathSeparator))
        {
            if (!string.IsNullOrEmpty(referencePath))
            {
                referencePaths.Add(referencePath);
            }
        }
    }

    /// <summary>Determines whether a diagnostic represents an error in generator output.</summary>
    /// <param name="diagnostic">The diagnostic to inspect.</param>
    /// <returns><see langword="true"/> when the diagnostic should fail generated-output compilation tests.</returns>
    private static bool IsGeneratedOutputError(Diagnostic diagnostic) =>
        diagnostic is { Severity: DiagnosticSeverity.Error }
        && diagnostic.Id != "CS5001";

    /// <summary>Builds a source file containing a generated-client interface body.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <returns>The complete source file.</returns>
    private static string BuildBodySource(string body) =>
        $$"""
          using System;
          using System.Collections.Generic;
          using System.Linq;
          using System.Net.Http;
          using System.Text;
          using System.Threading;
          using System.Threading.Tasks;
          using Refit;

          namespace RefitGeneratorTest;

          public interface IGeneratedClient
          {
          {{body}}
          }
          """;

    /// <summary>Runs the generator over the source and returns the verification result.</summary>
    /// <param name="source">The source to compile and generate from.</param>
    /// <param name="ignoreNonInterfaces">Whether to ignore non-interface generated results.</param>
    /// <returns>A task producing the verification result.</returns>
    private static Task<VerifyResult> VerifyGenerator(string source, bool ignoreNonInterfaces = true)
    {
        var compilation = CreateLibrary(source);

        var generator = new InterfaceStubGeneratorV2();
        var driver = CSharpGeneratorDriver.Create(generator);

        var ranDriver = driver.RunGenerators(compilation);
        var settings = new VerifySettings();
        if (ignoreNonInterfaces)
        {
            settings.IgnoreGeneratedResult(x =>
                x.HintName.Contains("PreserveAttribute.g.cs", StringComparison.Ordinal));
            settings.IgnoreGeneratedResult(x => x.HintName.Contains("Generated.g.cs", StringComparison.Ordinal));
        }

        var verify = Verify(ranDriver, settings);
        return verify.ToTask();
    }

    /// <summary>Runs the generator and returns a generated source by hint name.</summary>
    /// <param name="source">The source to compile and generate from.</param>
    /// <param name="hintName">The generated source hint name.</param>
    /// <param name="generatedRequestBuilding">Whether generated request construction is explicitly configured.</param>
    /// <returns>The generated source text.</returns>
    private static string Generate(
        string source,
        string hintName,
        bool? generatedRequestBuilding)
    {
        var compilation = CreateLibrary(source);
        var generator = new InterfaceStubGeneratorV2();
        var driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            optionsProvider: new TestAnalyzerConfigOptionsProvider(generatedRequestBuilding));

        var ranDriver = driver.RunGenerators(compilation);
        foreach (var syntaxTree in ranDriver.GetRunResult().GeneratedTrees)
        {
            if (Path.GetFileName(syntaxTree.FilePath) == hintName)
            {
                return syntaxTree.GetText().ToString();
            }
        }

        throw new InvalidOperationException($"Generated file '{hintName}' was not produced.");
    }

    /// <summary>Analyzer-config options used by source-generator tests.</summary>
    /// <param name="generatedRequestBuilding">Whether generated request construction is explicitly configured.</param>
    /// <param name="disableSourceGenerator">Whether source generation is disabled.</param>
    private sealed class TestAnalyzerConfigOptionsProvider(
        bool? generatedRequestBuilding,
        bool disableSourceGenerator = false)
        : AnalyzerConfigOptionsProvider
    {
        /// <inheritdoc/>
        public override AnalyzerConfigOptions GlobalOptions { get; } =
            new TestAnalyzerConfigOptions(generatedRequestBuilding, disableSourceGenerator);

        /// <inheritdoc/>
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) =>
            TestAnalyzerConfigOptions.Empty;

        /// <inheritdoc/>
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
            TestAnalyzerConfigOptions.Empty;
    }

    /// <summary>Analyzer-config options used by source-generator tests.</summary>
    /// <param name="generatedRequestBuilding">Whether generated request construction is explicitly configured.</param>
    /// <param name="disableSourceGenerator">Whether source generation is disabled.</param>
    private sealed class TestAnalyzerConfigOptions(
        bool? generatedRequestBuilding,
        bool disableSourceGenerator) : AnalyzerConfigOptions
    {
        /// <summary>Gets empty analyzer-config options.</summary>
        public static TestAnalyzerConfigOptions Empty { get; } = new(null, false);

        /// <inheritdoc/>
        public override bool TryGetValue(string key, out string value)
        {
            if (key == "build_property.RefitGeneratedRequestBuilding" && generatedRequestBuilding.HasValue)
            {
                value = generatedRequestBuilding.Value ? "true" : "false";
                return true;
            }

            if (key == "build_property.DisableRefitSourceGenerator" && disableSourceGenerator)
            {
                value = "true";
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}
