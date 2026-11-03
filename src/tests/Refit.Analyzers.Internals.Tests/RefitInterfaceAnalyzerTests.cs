// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Analyzers.Tests;

/// <summary>White-box tests for Refit interface analyzer implementation details.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public sealed class RefitInterfaceAnalyzerTests
{
    /// <summary>Verifies HTTP path extraction handles missing attribute data.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetHttpPathReturnsEmptyForMissingAttribute() =>
        await Assert.That(RefitInterfaceAnalyzer.GetHttpPath(null)).IsEqualTo(string.Empty);

    /// <summary>Verifies HTTP path extraction reads a string first argument and ignores other attribute shapes.</summary>
    /// <param name="declaration">The attributed declaration to compile.</param>
    /// <param name="expected">The path the analyzer should read.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("[System.Obsolete(\"/path\")] class C { }", "/path")]
    [Arguments("[System.Serializable] class C { }", "")]
    [Arguments("[System.AttributeUsage(System.AttributeTargets.All)] class C : System.Attribute { }", "")]
    public async Task GetHttpPathReadsOnlyAStringFirstArgument(string declaration, string expected)
    {
        var compilation = CSharpCompilation.Create(
            "PathTests",
            [CSharpSyntaxTree.ParseText(declaration)],
            [MetadataReference.CreateFromFile(FindCoreLibrary())]);
        var symbol = compilation.GetTypeByMetadataName("C")!;

        await Assert.That(RefitInterfaceAnalyzer.GetHttpPath(symbol.GetAttributes()[0])).IsEqualTo(expected);
    }

    /// <summary>Finds the core library the test runtime loaded.</summary>
    /// <returns>The core library's path.</returns>
    /// <exception cref="InvalidOperationException">The runtime does not list a core library.</exception>
    private static string FindCoreLibrary()
    {
        var trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        foreach (var path in trusted.Split(Path.PathSeparator))
        {
            if (path.EndsWith("System.Private.CoreLib.dll", StringComparison.Ordinal))
            {
                return path;
            }
        }

        throw new InvalidOperationException("The core library was not found.");
    }
}
