// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Refit.Generator;

/// <summary>An incremental source generator that produces Refit interface stub implementations.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
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
    public void Initialize(
        IncrementalGeneratorInitializationContext context)
    {
        var standardModels = CreateStandardHttpMethodCandidateProvider(context);

        var customCandidateMethodsProvider = context.SyntaxProvider.CreateSyntaxProvider(
            static (syntax, _) => IsPotentialCustomHttpMethodSyntax(syntax),
            static (generatorContext, cancellationToken) => InterfaceSemanticParsing.ParseMethodInterface(
                generatorContext.SemanticModel,
                (MethodDeclarationSyntax)generatorContext.Node,
                cancellationToken))
            .WithComparer(ArraySequenceComparer<InterfaceModel>.Instance)
            .WithTrackingName("ExtractCustomInterfaces");

        var candidateInterfacesProvider = context.SyntaxProvider.CreateSyntaxProvider(
            static (syntax, _) =>
                syntax is InterfaceDeclarationSyntax { BaseList: not null },
            static (generatorContext, cancellationToken) => InterfaceSemanticParsing.ParseInheritedInterface(
                generatorContext.SemanticModel,
                (InterfaceDeclarationSyntax)generatorContext.Node,
                cancellationToken))
            .WithComparer(ArraySequenceComparer<InterfaceModel>.Instance)
            .WithTrackingName("ExtractInheritedInterfaces");

        var generatorOptions =
            context.AnalyzerConfigOptionsProvider.Select(static (analyzerConfigOptionsProvider, _) =>
                CreateGeneratorOptions(analyzerConfigOptionsProvider.GlobalOptions));

        var candidates = standardModels
            .Combine(customCandidateMethodsProvider.Collect())
            .Select(static (combined, cancellationToken) => InterfaceSemanticParsing.CombineInterfaceModels(combined.Left, combined.Right, cancellationToken))
            .Combine(candidateInterfacesProvider.Collect())
            .Select(static (combined, cancellationToken) => InterfaceSemanticParsing.CombineInterfaceModels(combined.Left, combined.Right, cancellationToken))
            .WithComparer(ArraySequenceComparer<InterfaceModel>.Instance)
            .WithTrackingName("ParserInputs");

        // Resolve compilation-wide facts before combining them with cached interface data.
        var compilationFacts = context.CompilationProvider.Select(static (compilation, _) =>
            compilation.AssemblyName);

        var parseStep = candidates
            .Combine(generatorOptions)
            .Combine(compilationFacts)
            .Select(static (combined, cancellationToken) => InterfaceSemanticParsing.ApplyGeneratorOptions(
                combined.Left.Left,
                combined.Left.Right,
                combined.Right,
                cancellationToken))
            .WithTrackingName("ParseInterfaces");

        var contextModel = parseStep;
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ImmutableArray<MethodDeclarationSyntax> CombineCandidateMethodsForTesting(
        (ImmutableArray<MethodDeclarationSyntax> StandardMethods,
            ImmutableArray<MethodDeclarationSyntax> CustomMethods) combined) =>
        CombineCandidateMethods(combined);

    /// <summary>Determines whether an attribute syntax name is a built-in Refit HTTP method attribute name.</summary>
    /// <param name="name">The attribute name syntax.</param>
    /// <returns><see langword="true"/> when the name is one of Refit's built-in HTTP method attributes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    internal static IncrementalValueProvider<InterfaceModel[]>
        CreateStandardHttpMethodCandidateProvider(
            in IncrementalGeneratorInitializationContext context)
    {
        var deleteMethods = CreateHttpMethodCandidateProvider(context, DeleteAttributeMetadataName).Collect();
        var getMethods = CreateHttpMethodCandidateProvider(context, GetAttributeMetadataName).Collect();
        var headMethods = CreateHttpMethodCandidateProvider(context, HeadAttributeMetadataName).Collect();
        var optionsMethods = CreateHttpMethodCandidateProvider(context, OptionsAttributeMetadataName).Collect();
        var patchMethods = CreateHttpMethodCandidateProvider(context, PatchAttributeMetadataName).Collect();
        var postMethods = CreateHttpMethodCandidateProvider(context, PostAttributeMetadataName).Collect();
        var putMethods = CreateHttpMethodCandidateProvider(context, PutAttributeMetadataName).Collect();

        return deleteMethods
            .Combine(getMethods)
            .Select(static (combined, cancellationToken) => InterfaceSemanticParsing.CombineInterfaceModels([], combined.Left, cancellationToken, combined.Right))
            .Combine(headMethods)
            .Select(static (combined, cancellationToken) => InterfaceSemanticParsing.CombineInterfaceModels(combined.Left, combined.Right, cancellationToken))
            .Combine(optionsMethods)
            .Select(static (combined, cancellationToken) => InterfaceSemanticParsing.CombineInterfaceModels(combined.Left, combined.Right, cancellationToken))
            .Combine(patchMethods)
            .Select(static (combined, cancellationToken) => InterfaceSemanticParsing.CombineInterfaceModels(combined.Left, combined.Right, cancellationToken))
            .Combine(postMethods)
            .Select(static (combined, cancellationToken) => InterfaceSemanticParsing.CombineInterfaceModels(combined.Left, combined.Right, cancellationToken))
            .Combine(putMethods)
            .Select(static (combined, cancellationToken) => InterfaceSemanticParsing.CombineInterfaceModels(combined.Left, combined.Right, cancellationToken));
    }

    /// <summary>Creates a provider for a single Refit HTTP method attribute metadata name.</summary>
    /// <param name="context">The generator initialization context.</param>
    /// <param name="metadataName">The fully qualified metadata name.</param>
    /// <returns>The matching method declarations.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IncrementalValuesProvider<InterfaceModel[]> CreateHttpMethodCandidateProvider(
        in IncrementalGeneratorInitializationContext context,
        string metadataName) =>
        context.SyntaxProvider.ForAttributeWithMetadataName(
            metadataName,
            static (syntax, _) => IsInterfaceMethodDeclaration(syntax),
            static (generatorContext, cancellationToken) => InterfaceSemanticParsing.ParseMethodInterface(
                generatorContext.SemanticModel,
                (MethodDeclarationSyntax)generatorContext.TargetNode,
                cancellationToken))
            .WithComparer(ArraySequenceComparer<InterfaceModel>.Instance)
            .WithTrackingName($"Extract{metadataName}");

    /// <summary>Determines whether an attributed syntax node is a method declared directly on an interface.</summary>
    /// <param name="syntax">The candidate syntax node carrying a Refit HTTP method attribute.</param>
    /// <returns><see langword="true"/> when the node is an interface method declaration.</returns>
    [ExcludeFromCodeCoverage]
    internal static bool IsInterfaceMethodDeclaration(SyntaxNode syntax) =>
        syntax is MethodDeclarationSyntax { Parent: InterfaceDeclarationSyntax };

    /// <summary>Combines standard HTTP method candidate arrays into one array.</summary>
    /// <param name="candidates">The candidate arrays.</param>
    /// <returns>The combined candidates.</returns>
    internal static ImmutableArray<MethodDeclarationSyntax> CombineStandardHttpMethodCandidates(
        in StandardHttpMethodCandidates candidates)
    {
        // Built through an exact-capacity builder on both Roslyn versions: MoveToImmutable hands
        // over the array without copying, so this matches what a collection expression produces.
        var builder = ImmutableArray.CreateBuilder<MethodDeclarationSyntax>(
            candidates.DeleteMethods.Length
            + candidates.GetMethods.Length
            + candidates.HeadMethods.Length
            + candidates.OptionsMethods.Length
            + candidates.PatchMethods.Length
            + candidates.PostMethods.Length
            + candidates.PutMethods.Length);
        builder.AddRange(candidates.DeleteMethods);
        builder.AddRange(candidates.GetMethods);
        builder.AddRange(candidates.HeadMethods);
        builder.AddRange(candidates.OptionsMethods);
        builder.AddRange(candidates.PatchMethods);
        builder.AddRange(candidates.PostMethods);
        builder.AddRange(candidates.PutMethods);
        return builder.MoveToImmutable();
    }

    /// <summary>Combines built-in and potential custom HTTP method candidates.</summary>
    /// <param name="combined">The built-in and custom candidate arrays.</param>
    /// <returns>The combined candidates.</returns>
    internal static ImmutableArray<MethodDeclarationSyntax> CombineCandidateMethods(
        (ImmutableArray<MethodDeclarationSyntax> StandardMethods,
            ImmutableArray<MethodDeclarationSyntax> CustomMethods) combined)
    {
        // Either side being empty makes the other array the answer outright, so the common
        // single-attribute interface allocates nothing. The guards hold on both Roslyn versions.
        if (combined.StandardMethods.IsEmpty || combined.CustomMethods.IsEmpty)
        {
            return combined.StandardMethods.IsEmpty ? combined.CustomMethods : combined.StandardMethods;
        }

        // MoveToImmutable hands the array over without copying, so this matches what a collection
        // expression produces. A collection expression cannot be used: the generator compiles against
        // System.Collections.Immutable 7.0.0, which predates [CollectionBuilder] on ImmutableArray<T>.
        var builder = ImmutableArray.CreateBuilder<MethodDeclarationSyntax>(
            combined.StandardMethods.Length + combined.CustomMethods.Length);
        builder.AddRange(combined.StandardMethods);
        builder.AddRange(combined.CustomMethods);
        return builder.MoveToImmutable();
    }

    /// <summary>Determines whether syntax might be a method using a custom Refit HTTP method attribute.</summary>
    /// <param name="syntax">The syntax node to inspect.</param>
    /// <returns><see langword="true"/> when the method should go through the compatibility fallback.</returns>
    internal static bool IsPotentialCustomHttpMethodSyntax(
        SyntaxNode syntax)
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
    internal static bool IsStandardHttpMethodAttributeName(
        NameSyntax name)
    {
        var identifier = GetRightmostIdentifier(name);
        const string attributeSuffix = "Attribute";
        if (!identifier.EndsWith(attributeSuffix, StringComparison.Ordinal))
        {
            identifier += attributeSuffix;
        }

        return HttpMethodAttributeParsing.MapKnownHttpVerb(identifier) is not null;
    }

    /// <summary>Gets the rightmost identifier text from an attribute name.</summary>
    /// <param name="name">The attribute name syntax.</param>
    /// <returns>The rightmost identifier text.</returns>
    internal static string GetRightmostIdentifier(NameSyntax name) =>
        name switch
        {
            IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
            QualifiedNameSyntax qualifiedName => GetRightmostIdentifier(qualifiedName.Right),
            AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name.Identifier.ValueText,
            _ => string.Empty
        };

    /// <summary>Creates the generator options input.</summary>
    /// <param name="options">The global analyzer-config options.</param>
    /// <returns>The named generator options input.</returns>
    internal static GeneratorOptions CreateGeneratorOptions(
        AnalyzerConfigOptions options)
    {
        _ = TryGetGlobalOption(options, RefitInternalNamespaceOption, out var refitInternalNamespace);

        return new(
            refitInternalNamespace,
            GetBooleanOption(options, RefitGeneratedRequestBuildingOption, defaultValue: true),
            GetBooleanOption(options, RefitEmitGeneratedCodeMarkersOption, defaultValue: true),
            GetBooleanOption(options, DisableRefitSourceGeneratorOption, defaultValue: false));
    }

    /// <summary>Reads a boolean analyzer-config option.</summary>
    /// <param name="options">The analyzer-config options.</param>
    /// <param name="name">The option name without the build-property prefix.</param>
    /// <param name="defaultValue">The value to use when the option is not present or cannot be parsed.</param>
    /// <returns>The parsed option value.</returns>
    internal static bool GetBooleanOption(
        AnalyzerConfigOptions options,
        string name,
        bool defaultValue) =>
        TryGetGlobalOption(options, name, out var value) && bool.TryParse(
            value,
            out var parsed)
            ? parsed
            : defaultValue;

    /// <summary>The method declarations found through Refit's built-in HTTP method attributes.</summary>
    /// <param name="DeleteMethods">The methods decorated with <c>DeleteAttribute</c>.</param>
    /// <param name="GetMethods">The methods decorated with <c>GetAttribute</c>.</param>
    /// <param name="HeadMethods">The methods decorated with <c>HeadAttribute</c>.</param>
    /// <param name="OptionsMethods">The methods decorated with <c>OptionsAttribute</c>.</param>
    /// <param name="PatchMethods">The methods decorated with <c>PatchAttribute</c>.</param>
    /// <param name="PostMethods">The methods decorated with <c>PostAttribute</c>.</param>
    /// <param name="PutMethods">The methods decorated with <c>PutAttribute</c>.</param>
    internal readonly record struct StandardHttpMethodCandidates(
        ImmutableArray<MethodDeclarationSyntax> DeleteMethods,
        ImmutableArray<MethodDeclarationSyntax> GetMethods,
        ImmutableArray<MethodDeclarationSyntax> HeadMethods = default,
        ImmutableArray<MethodDeclarationSyntax> OptionsMethods = default,
        ImmutableArray<MethodDeclarationSyntax> PatchMethods = default,
        ImmutableArray<MethodDeclarationSyntax> PostMethods = default,
        ImmutableArray<MethodDeclarationSyntax> PutMethods = default);

    /// <summary>The generator options visible through analyzer config.</summary>
    /// <param name="RefitInternalNamespace">The optional Refit internal namespace prefix.</param>
    /// <param name="GeneratedRequestBuilding">Whether inline request construction is enabled.</param>
    /// <param name="EmitGeneratedCodeMarkers">Whether generated files include generated-code analyzer skip markers.</param>
    /// <param name="DisableSourceGenerator">Whether source generation is disabled.</param>
    internal readonly record struct GeneratorOptions(
        string? RefitInternalNamespace,
        bool GeneratedRequestBuilding,
        bool EmitGeneratedCodeMarkers,
        bool DisableSourceGenerator);
}
