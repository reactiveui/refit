// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Refit.Generator;

/// <summary>An incremental source generator that produces Refit interface stub implementations.</summary>
[Generator]
public class InterfaceStubGeneratorV2 : IIncrementalGenerator
{
    /// <summary>The MSBuild property prefix used by analyzer config options.</summary>
    private const string BuildPropertyPrefix = "build_property.";

    /// <summary>The option that disables all Refit source generation.</summary>
    private const string DisableRefitSourceGeneratorOption = "DisableRefitSourceGenerator";

    /// <summary>The option that enables direct generated request construction.</summary>
    private const string RefitGeneratedRequestBuildingOption = "RefitGeneratedRequestBuilding";

    /// <summary>The option that controls generated-code analyzer skip markers.</summary>
    private const string RefitEmitGeneratedCodeMarkersOption = "RefitEmitGeneratedCodeMarkers";

    /// <summary>The option that overrides the generated internal namespace prefix.</summary>
    private const string RefitInternalNamespaceOption = "RefitInternalNamespace";

    /// <summary>The Refit delete attribute metadata name.</summary>
    private const string DeleteAttributeMetadataName = "Refit.DeleteAttribute";

    /// <summary>The Refit get attribute metadata name.</summary>
    private const string GetAttributeMetadataName = "Refit.GetAttribute";

    /// <summary>The Refit head attribute metadata name.</summary>
    private const string HeadAttributeMetadataName = "Refit.HeadAttribute";

    /// <summary>The Refit options attribute metadata name.</summary>
    private const string OptionsAttributeMetadataName = "Refit.OptionsAttribute";

    /// <summary>The Refit patch attribute metadata name.</summary>
    private const string PatchAttributeMetadataName = "Refit.PatchAttribute";

    /// <summary>The Refit post attribute metadata name.</summary>
    private const string PostAttributeMetadataName = "Refit.PostAttribute";

    /// <summary>The Refit put attribute metadata name.</summary>
    private const string PutAttributeMetadataName = "Refit.PutAttribute";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var standardCandidateMethodsProvider = CreateStandardHttpMethodCandidateProvider(context);

        var customCandidateMethodsProvider = context.SyntaxProvider.CreateSyntaxProvider(
            static (syntax, _) => IsPotentialCustomHttpMethodSyntax(syntax),
            static (generatorContext, _) => (MethodDeclarationSyntax)generatorContext.Node);

        var candidateInterfacesProvider = context.SyntaxProvider.CreateSyntaxProvider(
            (syntax, _) =>
                syntax is InterfaceDeclarationSyntax { BaseList: not null },
            (generatorContext, _) => (InterfaceDeclarationSyntax)generatorContext.Node);

        var generatorOptions =
            context.AnalyzerConfigOptionsProvider.Select(
                static (analyzerConfigOptionsProvider, _) =>
                    CreateGeneratorOptions(analyzerConfigOptionsProvider.GlobalOptions));

        var candidateMethodsProvider = standardCandidateMethodsProvider
            .Combine(customCandidateMethodsProvider.Collect())
            .Select(static (combined, _) => CombineCandidateMethods(combined));

        var candidateSyntax = candidateMethodsProvider
            .Combine(candidateInterfacesProvider.Collect())
            .Select(static (combined, _) => CreateCandidateSyntax(combined));

        var syntaxAndOptions = candidateSyntax
            .Combine(generatorOptions)
            .Select(static (combined, _) => CreateSyntaxAndOptions(combined));

        var inputs = syntaxAndOptions
            .Combine(context.CompilationProvider)
            .Select(static (combined, _) => CreateGeneratorInputs(combined));

        var parseStep = inputs.Select(
            static (collectedValues, cancellationToken) =>
                collectedValues.DisableSourceGenerator
                    ? CreateDisabledGenerationResult()
                    : Parser.GenerateInterfaceStubs(
                        (CSharpCompilation)collectedValues.Compilation,
                        collectedValues.RefitInternalNamespace,
                        collectedValues.GeneratedRequestBuilding,
                        collectedValues.EmitGeneratedCodeMarkers,
                        collectedValues.CandidateMethods,
                        collectedValues.CandidateInterfaces,
                        cancellationToken));

        var diagnostics = parseStep
            .Select(static (x, _) => x.diagnostics.ToImmutableEquatableArray())
            .WithTrackingName(RefitGeneratorStepName.ReportDiagnostics);
        context.ReportDiagnostics(diagnostics);

        var contextModel = parseStep.Select(static (x, _) => x.contextGenerationSpec);
        var interfaceModels = contextModel
            .SelectMany(static (x, _) => x.Interfaces)
            .WithTrackingName(RefitGeneratorStepName.BuildRefit);
        context.EmitSource(interfaceModels);

        context.RegisterImplementationSourceOutput(
            contextModel,
            static (spc, model) => Emitter.EmitSharedCode(model, spc.AddSource));
    }

    /// <summary>Combines built-in and potential custom HTTP method candidates.</summary>
    /// <param name="combined">The built-in and custom candidate arrays.</param>
    /// <returns>The combined candidates.</returns>
    internal static ImmutableArray<MethodDeclarationSyntax> CombineCandidateMethodsForTesting(
        (ImmutableArray<MethodDeclarationSyntax> StandardMethods,
        ImmutableArray<MethodDeclarationSyntax> CustomMethods) combined) =>
        CombineCandidateMethods(combined);

    /// <summary>Determines whether an attribute syntax name is a built-in Refit HTTP method attribute name.</summary>
    /// <param name="name">The attribute name syntax.</param>
    /// <returns><see langword="true"/> when the name is one of Refit's built-in HTTP method attributes.</returns>
    internal static bool IsStandardHttpMethodAttributeNameForTesting(NameSyntax name) =>
        IsStandardHttpMethodAttributeName(name);

    /// <summary>Reads a global analyzer-config option by bare name or MSBuild build-property name.</summary>
    /// <param name="options">The analyzer-config options.</param>
    /// <param name="name">The option name without the build-property prefix.</param>
    /// <param name="value">The option value when found.</param>
    /// <returns><see langword="true"/> when an option value was found.</returns>
    internal static bool TryGetGlobalOption(
        AnalyzerConfigOptions options,
        string name,
        out string? value)
    {
        if (options.TryGetValue(BuildPropertyPrefix + name, out var buildPropertyValue))
        {
            value = buildPropertyValue;
            return true;
        }

        if (options.TryGetValue(name, out var analyzerConfigValue))
        {
            value = analyzerConfigValue;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Creates the provider for methods decorated with Refit's built-in HTTP method attributes.</summary>
    /// <param name="context">The generator initialization context.</param>
    /// <returns>The collected candidate methods.</returns>
    private static IncrementalValueProvider<ImmutableArray<MethodDeclarationSyntax>>
        CreateStandardHttpMethodCandidateProvider(IncrementalGeneratorInitializationContext context)
    {
        var deleteMethods = CreateHttpMethodCandidateProvider(context, DeleteAttributeMetadataName).Collect();
        var getMethods = CreateHttpMethodCandidateProvider(context, GetAttributeMetadataName).Collect();
        var headMethods = CreateHttpMethodCandidateProvider(context, HeadAttributeMetadataName).Collect();
        var optionsMethods = CreateHttpMethodCandidateProvider(context, OptionsAttributeMetadataName).Collect();
        var patchMethods = CreateHttpMethodCandidateProvider(context, PatchAttributeMetadataName).Collect();
        var postMethods = CreateHttpMethodCandidateProvider(context, PostAttributeMetadataName).Collect();
        var putMethods = CreateHttpMethodCandidateProvider(context, PutAttributeMetadataName).Collect();

        var standardMethods = deleteMethods
            .Combine(getMethods)
            .Select(static (combined, _) => new StandardHttpMethodCandidates
            {
                DeleteMethods = combined.Left,
                GetMethods = combined.Right
            });

        return standardMethods
            .Combine(headMethods)
            .Select(static (combined, _) => combined.Left with { HeadMethods = combined.Right })
            .Combine(optionsMethods)
            .Select(static (combined, _) => combined.Left with { OptionsMethods = combined.Right })
            .Combine(patchMethods)
            .Select(static (combined, _) => combined.Left with { PatchMethods = combined.Right })
            .Combine(postMethods)
            .Select(static (combined, _) => combined.Left with { PostMethods = combined.Right })
            .Combine(putMethods)
            .Select(static (combined, _) => CombineStandardHttpMethodCandidates(
                combined.Left with { PutMethods = combined.Right }));
    }

    /// <summary>Creates a provider for a single Refit HTTP method attribute metadata name.</summary>
    /// <param name="context">The generator initialization context.</param>
    /// <param name="metadataName">The fully qualified metadata name.</param>
    /// <returns>The matching method declarations.</returns>
    private static IncrementalValuesProvider<MethodDeclarationSyntax> CreateHttpMethodCandidateProvider(
        IncrementalGeneratorInitializationContext context,
        string metadataName) =>
        context.SyntaxProvider.ForAttributeWithMetadataName(
            metadataName,
            static (syntax, _) => syntax is MethodDeclarationSyntax { Parent: InterfaceDeclarationSyntax },
            static (generatorContext, _) => (MethodDeclarationSyntax)generatorContext.TargetNode);

    /// <summary>Combines standard HTTP method candidate arrays into one array.</summary>
    /// <param name="candidates">The candidate arrays.</param>
    /// <returns>The combined candidates.</returns>
    private static ImmutableArray<MethodDeclarationSyntax> CombineStandardHttpMethodCandidates(
        StandardHttpMethodCandidates candidates)
    {
#if ROSLYN_5
        return
        [
            ..candidates.DeleteMethods,
            ..candidates.GetMethods,
            ..candidates.HeadMethods,
            ..candidates.OptionsMethods,
            ..candidates.PatchMethods,
            ..candidates.PostMethods,
            ..candidates.PutMethods
        ];
#else
        var count =
            candidates.DeleteMethods.Length
            + candidates.GetMethods.Length
            + candidates.HeadMethods.Length
            + candidates.OptionsMethods.Length
            + candidates.PatchMethods.Length
            + candidates.PostMethods.Length
            + candidates.PutMethods.Length;
        var builder = ImmutableArray.CreateBuilder<MethodDeclarationSyntax>(count);
        builder.AddRange(candidates.DeleteMethods);
        builder.AddRange(candidates.GetMethods);
        builder.AddRange(candidates.HeadMethods);
        builder.AddRange(candidates.OptionsMethods);
        builder.AddRange(candidates.PatchMethods);
        builder.AddRange(candidates.PostMethods);
        builder.AddRange(candidates.PutMethods);
        return builder.MoveToImmutable();
#endif
    }

    /// <summary>Combines built-in and potential custom HTTP method candidates.</summary>
    /// <param name="combined">The built-in and custom candidate arrays.</param>
    /// <returns>The combined candidates.</returns>
    private static ImmutableArray<MethodDeclarationSyntax> CombineCandidateMethods(
        (ImmutableArray<MethodDeclarationSyntax> StandardMethods,
        ImmutableArray<MethodDeclarationSyntax> CustomMethods) combined)
    {
#if ROSLYN_5
        return [.. combined.StandardMethods, .. combined.CustomMethods];
#else
        if (combined.StandardMethods.Length == 0)
        {
            return combined.CustomMethods;
        }

        if (combined.CustomMethods.Length == 0)
        {
            return combined.StandardMethods;
        }

        var builder = ImmutableArray.CreateBuilder<MethodDeclarationSyntax>(
            combined.StandardMethods.Length + combined.CustomMethods.Length);
        builder.AddRange(combined.StandardMethods);
        builder.AddRange(combined.CustomMethods);
        return builder.MoveToImmutable();
#endif
    }

    /// <summary>Determines whether syntax might be a method using a custom Refit HTTP method attribute.</summary>
    /// <param name="syntax">The syntax node to inspect.</param>
    /// <returns><see langword="true"/> when the method should go through the compatibility fallback.</returns>
    private static bool IsPotentialCustomHttpMethodSyntax(SyntaxNode syntax)
    {
        if (syntax is not MethodDeclarationSyntax
            {
                Parent: InterfaceDeclarationSyntax,
                AttributeLists.Count: > 0
            } method)
        {
            return false;
        }

        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (IsStandardHttpMethodAttributeName(attribute.Name))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>Determines whether an attribute syntax name is a built-in Refit HTTP method attribute name.</summary>
    /// <param name="name">The attribute name syntax.</param>
    /// <returns><see langword="true"/> when the name is one of Refit's built-in HTTP method attributes.</returns>
    private static bool IsStandardHttpMethodAttributeName(NameSyntax name)
    {
        var identifier = GetRightmostIdentifier(name);
        const string attributeSuffix = "Attribute";
        if (identifier.EndsWith(attributeSuffix, StringComparison.Ordinal))
        {
            identifier = identifier[..^attributeSuffix.Length];
        }

        return identifier is "Delete" or "Get" or "Head" or "Options" or "Patch" or "Post" or "Put";
    }

    /// <summary>Gets the rightmost identifier text from an attribute name.</summary>
    /// <param name="name">The attribute name syntax.</param>
    /// <returns>The rightmost identifier text.</returns>
    private static string GetRightmostIdentifier(NameSyntax name) =>
        name switch
        {
            IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
            QualifiedNameSyntax qualifiedName => GetRightmostIdentifier(qualifiedName.Right),
            AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name.Identifier.ValueText,
            _ => string.Empty
        };

    /// <summary>Creates the collected candidate syntax input.</summary>
    /// <param name="combined">The combined method and interface candidate collections.</param>
    /// <returns>The named candidate syntax input.</returns>
    private static CandidateSyntax CreateCandidateSyntax(
        (ImmutableArray<MethodDeclarationSyntax> Methods, ImmutableArray<InterfaceDeclarationSyntax> Interfaces) combined) =>
        new(combined.Methods, combined.Interfaces);

    /// <summary>Creates the generator options input.</summary>
    /// <param name="options">The global analyzer-config options.</param>
    /// <returns>The named generator options input.</returns>
    private static GeneratorOptions CreateGeneratorOptions(AnalyzerConfigOptions options)
    {
        _ = TryGetGlobalOption(options, RefitInternalNamespaceOption, out var refitInternalNamespace);

        return new(
            refitInternalNamespace,
            GetBooleanOption(options, RefitGeneratedRequestBuildingOption, defaultValue: true),
            GetBooleanOption(options, RefitEmitGeneratedCodeMarkersOption, defaultValue: true),
            GetBooleanOption(options, DisableRefitSourceGeneratorOption, defaultValue: false));
    }

    /// <summary>Creates the combined syntax and options input.</summary>
    /// <param name="combined">The combined candidate syntax and generator options.</param>
    /// <returns>The named syntax-and-options input.</returns>
    private static SyntaxAndOptions CreateSyntaxAndOptions(
        (CandidateSyntax CandidateSyntax, GeneratorOptions Options) combined) =>
        new(combined.CandidateSyntax, combined.Options);

    /// <summary>Creates the final parser input.</summary>
    /// <param name="combined">The combined syntax/options input and compilation.</param>
    /// <returns>The named parser input.</returns>
    private static GeneratorInputs CreateGeneratorInputs(
        (SyntaxAndOptions SyntaxAndOptions, Compilation Compilation) combined) =>
        new(
            combined.SyntaxAndOptions.CandidateSyntax.CandidateMethods,
            combined.SyntaxAndOptions.CandidateSyntax.CandidateInterfaces,
            combined.SyntaxAndOptions.Options.RefitInternalNamespace,
            combined.SyntaxAndOptions.Options.GeneratedRequestBuilding,
            combined.SyntaxAndOptions.Options.EmitGeneratedCodeMarkers,
            combined.SyntaxAndOptions.Options.DisableSourceGenerator,
            combined.Compilation);

    /// <summary>Creates an empty result when generation is explicitly disabled.</summary>
    /// <returns>The disabled generation result.</returns>
    private static (List<Diagnostic> diagnostics, ContextGenerationModel contextGenerationSpec)
        CreateDisabledGenerationResult() =>
        new(
            [],
            new(
                string.Empty,
                string.Empty,
                false,
                true,
                ImmutableEquatableArrayFactory.Empty<InterfaceModel>()));

    /// <summary>Reads a boolean analyzer-config option.</summary>
    /// <param name="options">The analyzer-config options.</param>
    /// <param name="name">The option name without the build-property prefix.</param>
    /// <param name="defaultValue">The value to use when the option is not present or cannot be parsed.</param>
    /// <returns>The parsed option value.</returns>
    private static bool GetBooleanOption(
        AnalyzerConfigOptions options,
        string name,
        bool defaultValue) =>
        TryGetGlobalOption(options, name, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : defaultValue;

    /// <summary>The collected syntax candidates for one generator pass.</summary>
    /// <param name="CandidateMethods">The candidate method declarations.</param>
    /// <param name="CandidateInterfaces">The candidate interface declarations.</param>
    private readonly record struct CandidateSyntax(
        ImmutableArray<MethodDeclarationSyntax> CandidateMethods,
        ImmutableArray<InterfaceDeclarationSyntax> CandidateInterfaces);

    /// <summary>The method declarations found through Refit's built-in HTTP method attributes.</summary>
    private readonly record struct StandardHttpMethodCandidates
    {
        /// <summary>Gets the methods decorated with <c>DeleteAttribute</c>.</summary>
        public ImmutableArray<MethodDeclarationSyntax> DeleteMethods { get; init; }

        /// <summary>Gets the methods decorated with <c>GetAttribute</c>.</summary>
        public ImmutableArray<MethodDeclarationSyntax> GetMethods { get; init; }

        /// <summary>Gets the methods decorated with <c>HeadAttribute</c>.</summary>
        public ImmutableArray<MethodDeclarationSyntax> HeadMethods { get; init; }

        /// <summary>Gets the methods decorated with <c>OptionsAttribute</c>.</summary>
        public ImmutableArray<MethodDeclarationSyntax> OptionsMethods { get; init; }

        /// <summary>Gets the methods decorated with <c>PatchAttribute</c>.</summary>
        public ImmutableArray<MethodDeclarationSyntax> PatchMethods { get; init; }

        /// <summary>Gets the methods decorated with <c>PostAttribute</c>.</summary>
        public ImmutableArray<MethodDeclarationSyntax> PostMethods { get; init; }

        /// <summary>Gets the methods decorated with <c>PutAttribute</c>.</summary>
        public ImmutableArray<MethodDeclarationSyntax> PutMethods { get; init; }
    }

    /// <summary>The generator options visible through analyzer config.</summary>
    /// <param name="RefitInternalNamespace">The optional Refit internal namespace prefix.</param>
    /// <param name="GeneratedRequestBuilding">Whether inline request construction is enabled.</param>
    /// <param name="EmitGeneratedCodeMarkers">Whether generated files include generated-code analyzer skip markers.</param>
    /// <param name="DisableSourceGenerator">Whether source generation is disabled.</param>
    private readonly record struct GeneratorOptions(
        string? RefitInternalNamespace,
        bool GeneratedRequestBuilding,
        bool EmitGeneratedCodeMarkers,
        bool DisableSourceGenerator);

    /// <summary>The collected syntax candidates plus generator options.</summary>
    /// <param name="CandidateSyntax">The candidate syntax input.</param>
    /// <param name="Options">The generator options input.</param>
    private readonly record struct SyntaxAndOptions(
        CandidateSyntax CandidateSyntax,
        GeneratorOptions Options);

    /// <summary>The full input consumed by the parser.</summary>
    /// <param name="CandidateMethods">The candidate method declarations.</param>
    /// <param name="CandidateInterfaces">The candidate interface declarations.</param>
    /// <param name="RefitInternalNamespace">The optional Refit internal namespace prefix.</param>
    /// <param name="GeneratedRequestBuilding">Whether inline request construction is enabled.</param>
    /// <param name="EmitGeneratedCodeMarkers">Whether generated files include generated-code analyzer skip markers.</param>
    /// <param name="DisableSourceGenerator">Whether source generation is disabled.</param>
    /// <param name="Compilation">The current compilation.</param>
    private readonly record struct GeneratorInputs(
        ImmutableArray<MethodDeclarationSyntax> CandidateMethods,
        ImmutableArray<InterfaceDeclarationSyntax> CandidateInterfaces,
        string? RefitInternalNamespace,
        bool GeneratedRequestBuilding,
        bool EmitGeneratedCodeMarkers,
        bool DisableSourceGenerator,
        Compilation Compilation);
}
