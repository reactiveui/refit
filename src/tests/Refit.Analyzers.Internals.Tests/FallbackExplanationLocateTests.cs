// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Refit.Generator;

namespace Refit.Analyzers.Tests;

/// <summary>White-box tests for choosing the source location of a fallback diagnostic.</summary>
public sealed class FallbackExplanationLocateTests
{
    /// <summary>The core library file name used to resolve framework types.</summary>
    private const string CoreLibraryFileName = "System.Private.CoreLib.dll";

    /// <summary>Verifies a method-level fallback with no attribute is reported on the method.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MissingAttributeFallsBackToTheMethodLocation()
    {
        var (_, method) = CreateMethod();

        var location = FallbackExplanation.Locate(
            method,
            InlineFallback.ForMethod(InlineFallbackReason.UnreadableHttpMethod),
            null);

        await Assert.That(location).IsEqualTo(method.Locations[0]);
    }

    /// <summary>Verifies an attribute with no source syntax is reported on the method.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AttributeWithoutSyntaxFallsBackToTheMethodLocation()
    {
        var (compilation, method) = CreateMethod();
        var metadataAttribute = compilation.GetTypeByMetadataName("System.DateTime")!.GetAttributes()[0];

        var location = FallbackExplanation.Locate(
            method,
            InlineFallback.ForMethod(InlineFallbackReason.UnsupportedPathTemplate),
            metadataAttribute);

        await Assert.That(location).IsEqualTo(method.Locations[0]);
    }

    /// <summary>Verifies an attribute with source syntax is reported on the attribute itself.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AttributeWithSyntaxIsReportedOnTheAttribute()
    {
        var compilation = CSharpCompilation.Create(
            "LocateTests",
            [CSharpSyntaxTree.ParseText("interface I { [System.Obsolete] void M(); }")],
            [MetadataReference.CreateFromFile(FindCoreLibrary())]);
        var method = (IMethodSymbol)compilation.GetTypeByMetadataName("I")!.GetMembers("M")[0];
        var attribute = method.GetAttributes()[0];

        var location = FallbackExplanation.Locate(
            method,
            InlineFallback.ForMethod(InlineFallbackReason.UnreadableHttpMethod),
            attribute);

        var attributeSyntax = await attribute.ApplicationSyntaxReference!.GetSyntaxAsync();

        await Assert.That(location).IsEqualTo(attributeSyntax.GetLocation());
    }

    /// <summary>Compiles an interface with one method.</summary>
    /// <returns>The compilation and the method symbol.</returns>
    private static (CSharpCompilation Compilation, IMethodSymbol Method) CreateMethod()
    {
        var compilation = CSharpCompilation.Create(
            "LocateTests",
            [CSharpSyntaxTree.ParseText("interface I { void M(); }")],
            [MetadataReference.CreateFromFile(FindCoreLibrary())]);
        var method = (IMethodSymbol)compilation.GetTypeByMetadataName("I")!.GetMembers("M")[0];
        return (compilation, method);
    }

    /// <summary>Finds the core library the test runtime loaded.</summary>
    /// <returns>The core library's path.</returns>
    /// <exception cref="InvalidOperationException">The runtime does not list a core library.</exception>
    private static string FindCoreLibrary()
    {
        var trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        foreach (var path in trusted.Split(Path.PathSeparator))
        {
            if (path.EndsWith(CoreLibraryFileName, StringComparison.Ordinal))
            {
                return path;
            }
        }

        throw new InvalidOperationException("The core library was not found.");
    }
}
