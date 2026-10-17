// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refit.Generator;

/// <summary>Extracts compiler-independent interface data inside syntax transforms.</summary>
internal static class InterfaceSemanticParsing
{
    /// <summary>Parses a syntax tree once through its first Refit method.</summary>
    /// <param name="semanticModel">The current semantic model.</param>
    /// <param name="method">The candidate method declaration.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>The tree's parsed interfaces, or an empty array for other candidates.</returns>
    internal static InterfaceModel[] ParseMethodInterface(
        SemanticModel semanticModel,
        MethodDeclarationSyntax method,
        CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetDeclaredSymbol(method, cancellationToken);
        var attribute = semanticModel.Compilation.GetTypeByMetadataName("Refit.HttpMethodAttribute");
        if (symbol is null || attribute is null || !Parser.IsRefitMethod(symbol, attribute))
        {
            return [];
        }

        return FindFirstRefitMethod(semanticModel, attribute, cancellationToken) is { } first && first.Span == method.Span
            ? ParseSyntaxTree(semanticModel, attribute, cancellationToken)
            : [];
    }

    /// <summary>Parses inherited interfaces in a tree without a directly declared Refit method.</summary>
    /// <param name="semanticModel">The current semantic model.</param>
    /// <param name="declaration">The candidate interface declaration.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>The tree's parsed interfaces, or an empty array for other candidates.</returns>
    internal static InterfaceModel[] ParseInheritedInterface(
        SemanticModel semanticModel,
        InterfaceDeclarationSyntax declaration,
        CancellationToken cancellationToken)
    {
        var attribute = semanticModel.Compilation.GetTypeByMetadataName("Refit.HttpMethodAttribute");
        if (attribute is null || FindFirstRefitMethod(semanticModel, attribute, cancellationToken) is not null)
        {
            return [];
        }

        foreach (var node in semanticModel.SyntaxTree.GetRoot(cancellationToken).DescendantNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node is not InterfaceDeclarationSyntax { BaseList: not null } candidate
                || semanticModel.GetDeclaredSymbol(candidate, cancellationToken) is not { } symbol
                || !HasRefitMethods(symbol, attribute))
            {
                continue;
            }

            return candidate.Span == declaration.Span
                ? ParseSyntaxTree(semanticModel, attribute, cancellationToken)
                : [];
        }

        return [];
    }

    /// <summary>Combines parsed candidates, excluding nulls and duplicate interface declarations.</summary>
    /// <param name="models">The candidates collected so far.</param>
    /// <param name="first">The next candidate collection.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <param name="second">An optional additional candidate collection.</param>
    /// <returns>The distinct parsed interfaces in discovery order.</returns>
    internal static InterfaceModel[] CombineInterfaceModels(
        InterfaceModel[] models,
        ImmutableArray<InterfaceModel[]> first,
        CancellationToken cancellationToken,
        ImmutableArray<InterfaceModel[]> second = default)
    {
        List<InterfaceModel> combined = [.. models];
        AddInterfaceModels(combined, first, cancellationToken);
        if (!second.IsDefault)
        {
            AddInterfaceModels(combined, second, cancellationToken);
        }

        return [.. combined];
    }

    /// <summary>Applies consumer options to semantic values without consulting the compilation.</summary>
    /// <param name="models">The parsed interfaces.</param>
    /// <param name="options">The consumer options.</param>
    /// <param name="assemblyName">The consumer assembly name.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>The final cacheable source and diagnostic inputs.</returns>
    internal static ContextGenerationModel ApplyGeneratorOptions(
        InterfaceModel[] models,
        InterfaceStubGeneratorV2.GeneratorOptions options,
        string? assemblyName,
        CancellationToken cancellationToken)
    {
        if (options.DisableSourceGenerator)
        {
            return new(string.Empty, string.Empty, string.Empty, false, true, []);
        }

        var internalNamespace = GeneratedTypeNaming.BuildRefitInternalNamespace(options.RefitInternalNamespace, assemblyName);
        var preserveAttribute = $"global::{internalNamespace}.PreserveAttribute";
        var interfaces = new InterfaceModel[models.Length];
        Dictionary<string, int> keyCount = [with(StringComparer.OrdinalIgnoreCase)];
        for (var i = 0; i < models.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var model = models[i];
            var keyName = model.FileName[..^".g.cs".Length];
            while (keyCount.TryGetValue(keyName, out var value))
            {
                keyName = $"{keyName}{value + 1}";
            }

            keyCount[keyName] = 0;
            interfaces[i] = model with
            {
                FileName = $"{keyName}.g.cs",
                Options = model.Options with
                {
                    PreserveAttributeDisplayName = preserveAttribute,
                    GeneratedRequestBuilding = options.GeneratedRequestBuilding,
                    EmitGeneratedCodeMarkers = options.EmitGeneratedCodeMarkers
                },
                Members = options.GeneratedRequestBuilding
                    ? model.Members
                    : model.Members with
                    {
                        RefitMethods = WithoutGeneratedRequests(model.Members.RefitMethods),
                        DerivedRefitMethods = WithoutGeneratedRequests(model.Members.DerivedRefitMethods)
                    }
            };
        }

        return new(
            internalNamespace,
            interfaces.Length == 0 ? string.Empty : preserveAttribute,
            GeneratedTypeNaming.BuildGeneratedContainerName(assemblyName),
            options.GeneratedRequestBuilding,
            options.EmitGeneratedCodeMarkers,
            interfaces);
    }

    /// <summary>Finds the first directly declared Refit method in a syntax tree.</summary>
    /// <param name="semanticModel">The syntax transform's semantic model.</param>
    /// <param name="attribute">The Refit HTTP method base attribute.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>The first Refit method, or null if the tree declares none.</returns>
    private static MethodDeclarationSyntax? FindFirstRefitMethod(
        SemanticModel semanticModel,
        INamedTypeSymbol attribute,
        CancellationToken cancellationToken)
    {
        foreach (var node in semanticModel.SyntaxTree.GetRoot(cancellationToken).DescendantNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node is not MethodDeclarationSyntax { Parent: InterfaceDeclarationSyntax, AttributeLists.Count: > 0 } method
                || semanticModel.GetDeclaredSymbol(method, cancellationToken) is not { } symbol
                || !Parser.IsRefitMethod(symbol, attribute)
                || !IsDiscoverableMethod(method, symbol))
            {
                continue;
            }

            return method;
        }

        return null;
    }

    /// <summary>Checks whether a method is reachable by a built-in or custom discovery provider.</summary>
    /// <param name="method">The candidate method syntax.</param>
    /// <param name="symbol">The candidate method symbol.</param>
    /// <returns>Whether a discovery provider can select the method.</returns>
    private static bool IsDiscoverableMethod(MethodDeclarationSyntax method, IMethodSymbol symbol)
    {
        if (InterfaceStubGeneratorV2.IsPotentialCustomHttpMethodSyntax(method))
        {
            return true;
        }

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass is
                {
                    ContainingType: null,
                    ContainingNamespace: { Name: "Refit", ContainingNamespace.IsGlobalNamespace: true }
                } type
                && HttpMethodAttributeParsing.MapKnownHttpVerb(type.Name) is not null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether an interface declares or inherits any Refit methods.</summary>
    /// <param name="symbol">The interface to inspect.</param>
    /// <param name="attribute">The Refit HTTP method base attribute.</param>
    /// <returns>Whether the interface has a Refit method.</returns>
    private static bool HasRefitMethods(INamedTypeSymbol symbol, INamedTypeSymbol attribute)
    {
        foreach (var member in symbol.GetMembers())
        {
            if (member is IMethodSymbol method && Parser.IsRefitMethod(method, attribute))
            {
                return true;
            }
        }

        return Parser.HasDerivedRefitMethods(symbol, attribute);
    }

    /// <summary>Parses all interfaces in a tree with shared, transform-local symbol caches.</summary>
    /// <param name="semanticModel">The syntax transform's semantic model.</param>
    /// <param name="attribute">The Refit HTTP method base attribute.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>The compiler-independent interface values.</returns>
    private static InterfaceModel[] ParseSyntaxTree(
        SemanticModel semanticModel,
        INamedTypeSymbol attribute,
        CancellationToken cancellationToken)
    {
        var compilation = (CSharpCompilation)semanticModel.Compilation;
        var internalNamespace = GeneratedTypeNaming.BuildRefitInternalNamespace(null, compilation.AssemblyName);
        var context = Parser.CreateGenerationContext(
            compilation,
            [],
            internalNamespace,
            attribute,
            true,
            true,
            cancellationToken);
        List<InterfaceModel> models = [];
        HashSet<ISymbol> seen = [with(SymbolEqualityComparer.Default)];
        foreach (var node in semanticModel.SyntaxTree.GetRoot(cancellationToken).DescendantNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node is InterfaceDeclarationSyntax declaration
                && semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is { } symbol
                && seen.Add(symbol)
                && HasRefitMethods(symbol, attribute))
            {
                models.Add(ParseInterface(semanticModel, symbol, context, cancellationToken));
            }
        }

        return [.. models];
    }

    /// <summary>Copies an interface's semantic information into compiler-independent values.</summary>
    /// <param name="semanticModel">The syntax transform's semantic model.</param>
    /// <param name="symbol">The interface to parse.</param>
    /// <param name="context">The transform-local parsing context.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>The compiler-independent interface value.</returns>
    private static InterfaceModel ParseInterface(
        SemanticModel semanticModel,
        INamedTypeSymbol symbol,
        in InterfaceGenerationContext context,
        CancellationToken cancellationToken)
    {
        var compilation = (CSharpCompilation)semanticModel.Compilation;
        List<IMethodSymbol> methods = [];
        var compilationNullableEnabled = compilation.Options.NullableContextOptions == NullableContextOptions.Enable;
        var nullableEnabled = compilationNullableEnabled;
        foreach (var member in symbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (member is not IMethodSymbol method || !Parser.IsRefitMethod(method, context.Symbols.HttpMethodBaseAttributeSymbol))
            {
                continue;
            }

            methods.Add(method);
            if (!compilationNullableEnabled && method.DeclaringSyntaxReferences is { IsEmpty: false } references)
            {
                nullableEnabled = IsNullableEnabled(references[0], semanticModel);
            }
        }

        if (methods.Count == 0)
        {
            nullableEnabled = IsNullableEnabled(symbol.DeclaringSyntaxReferences[0], semanticModel);
        }

        return Parser.ProcessInterface($"{symbol.Name}.g.cs", symbol, methods, nullableEnabled, context);
    }

    /// <summary>Reads the nullable context from the declaration's own syntax tree.</summary>
    /// <param name="reference">The declaration reference.</param>
    /// <param name="semanticModel">The syntax transform's semantic model.</param>
    /// <returns>Whether nullable annotations and warnings are enabled.</returns>
    private static bool IsNullableEnabled(SyntaxReference reference, SemanticModel semanticModel)
    {
        var model = reference.SyntaxTree == semanticModel.SyntaxTree
            ? semanticModel
            : semanticModel.Compilation.GetSemanticModel(reference.SyntaxTree);
        return model.GetNullableContext(reference.Span.Start) == NullableContext.Enabled;
    }

    /// <summary>Adds distinct non-null interfaces in discovery order.</summary>
    /// <param name="models">The combined interfaces.</param>
    /// <param name="candidates">The candidates to add.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    private static void AddInterfaceModels(
        List<InterfaceModel> models,
        ImmutableArray<InterfaceModel[]> candidates,
        CancellationToken cancellationToken)
    {
        foreach (var batch in candidates)
        {
            foreach (var candidate in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!ContainsInterface(models, candidate.Names.InterfaceDisplayName))
                {
                    models.Add(candidate);
                }
            }
        }
    }

    /// <summary>Determines whether an interface was collected by another discovery provider.</summary>
    /// <param name="models">The interfaces collected so far.</param>
    /// <param name="name">The qualified interface name.</param>
    /// <returns>Whether the interface is present.</returns>
    private static bool ContainsInterface(List<InterfaceModel> models, string name)
    {
        foreach (var model in models)
        {
            if (model.Names.InterfaceDisplayName == name)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Removes inline request metadata when reflection request building is selected.</summary>
    /// <param name="methods">The parsed Refit methods.</param>
    /// <returns>The method signatures with empty request metadata.</returns>
    private static MethodModel[] WithoutGeneratedRequests(MethodModel[] methods)
    {
        var models = new MethodModel[methods.Length];
        for (var i = 0; i < models.Length; i++)
        {
            models[i] = methods[i] with { Request = RequestModel.Empty };
        }

        return models;
    }
}
