// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Xml.Linq;

namespace Refit.GeneratorTests;

/// <summary>
/// Tests the analyzer layout of the Refit package.
/// <para>
/// Only the .NET SDK narrows a package that ships several <c>analyzers/dotnet/roslyn&lt;version&gt;/cs</c>
/// slots down to one: its <c>ResolvePackageAssets</c> task picks the highest slot at or below
/// <c>$(CompilerApiVersion)</c>. A legacy non-SDK <c>.csproj</c> never runs that task, so it receives every
/// slot at once, each generator emits the same types, and the consumer's build fails on duplicate members.
/// Shipping a single slot is what keeps those consumers working, so it is asserted here rather than left
/// to a packaging convention that a later cleanup can quietly undo.
/// </para>
/// </summary>
public class AnalyzerPackagingTests
{
    /// <summary>The property in refit.props holding the Roslyn version of the shipped analyzer slot.</summary>
    private const string MinimumCompilerVersionProperty = "_RefitMinimumCompilerVersion";

    /// <summary>Verifies the package declares exactly one analyzer slot.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PackageDeclaresExactlyOneRoslynAnalyzerSlot()
    {
        var slots = RefitPackageLayout.GetPackagedAnalyzers()
            .Select(static analyzer => analyzer.PackagePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await Assert.That(slots).IsNotEmpty();
        await Assert.That(slots.Length).IsEqualTo(1);
    }

    /// <summary>Verifies no analyzer assembly is shipped more than once.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PackagedAnalyzerAssembliesAreShippedOnce()
    {
        var duplicated = RefitPackageLayout.GetPackagedAnalyzers()
            .GroupBy(static analyzer => analyzer.FileName, StringComparer.OrdinalIgnoreCase)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await Assert.That(duplicated).IsEmpty();
    }

    /// <summary>
    /// Verifies the compiler floor enforced by refit.props matches the slot actually shipped. A slot newer
    /// than the host compiler is dropped silently, so the two have to move together or consumers below the
    /// floor get no generated clients and no explanation.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MinimumCompilerVersionMatchesThePackagedAnalyzerSlot()
    {
        var slots = RefitPackageLayout.GetPackagedAnalyzers()
            .Select(static analyzer => analyzer.PackagePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase);

        // Joined rather than compared element-wise so a stray extra slot shows up in the diff.
        await Assert.That(string.Join(", ", slots))
            .IsEqualTo($"analyzers/dotnet/roslyn{ReadMinimumCompilerVersion()}/cs");
    }

    /// <summary>Reads the Roslyn version of the shipped analyzer slot from refit.props.</summary>
    /// <returns>The declared version, such as <c>4.8</c>.</returns>
    internal static string ReadMinimumCompilerVersion() =>
        XDocument
            .Load(RefitPackageLayout.FindRepositoryFile("Refit/targets/refit.props"))
            .Descendants(MinimumCompilerVersionProperty)
            .Select(static element => element.Value.Trim())
            .Single();
}
