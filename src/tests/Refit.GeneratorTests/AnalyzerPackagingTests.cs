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
    /// <summary>The package path prefix that marks an item as a shipped analyzer.</summary>
    private const string AnalyzerPackagePathPrefix = "analyzers/";

    /// <summary>The property in refit.props holding the Roslyn version of the shipped analyzer slot.</summary>
    private const string MinimumCompilerVersionProperty = "_RefitMinimumCompilerVersion";

    /// <summary>Verifies the package declares exactly one analyzer slot.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PackageDeclaresExactlyOneRoslynAnalyzerSlot()
    {
        var slots = GetPackagedAnalyzers()
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
        var duplicated = GetPackagedAnalyzers()
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
        var slots = GetPackagedAnalyzers()
            .Select(static analyzer => analyzer.PackagePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase);

        var declaredMinimum = XDocument
            .Load(FindRepositoryFile("Refit/targets/refit.props"))
            .Descendants(MinimumCompilerVersionProperty)
            .Select(static element => element.Value.Trim())
            .Single();

        // Joined rather than compared element-wise so a stray extra slot shows up in the diff.
        await Assert.That(string.Join(", ", slots))
            .IsEqualTo($"analyzers/dotnet/roslyn{declaredMinimum}/cs");
    }

    /// <summary>Reads the analyzer items the Refit package ships.</summary>
    /// <returns>The packaged analyzer package path and assembly file name pairs.</returns>
    private static List<(string PackagePath, string FileName)> GetPackagedAnalyzers()
    {
        var analyzers = new List<(string PackagePath, string FileName)>();

        foreach (var element in XDocument.Load(FindRepositoryFile("Refit/Refit.csproj")).Descendants("None"))
        {
            var packagePath = Normalize(element.Attribute("PackagePath")?.Value);
            if (!packagePath.StartsWith(AnalyzerPackagePathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var include = Normalize(element.Attribute("Include")?.Value);
            analyzers.Add((packagePath, include[(include.LastIndexOf('/') + 1)..]));
        }

        return analyzers;
    }

    /// <summary>Normalizes an MSBuild path attribute to forward slashes.</summary>
    /// <param name="value">The raw attribute value.</param>
    /// <returns>The normalized value, or an empty string when the attribute is absent.</returns>
    private static string Normalize(string? value) =>
        value?.Replace('\\', '/') ?? string.Empty;

    /// <summary>Resolves a path under the repository source directory.</summary>
    /// <param name="relativePath">The path relative to the directory holding Refit.slnx.</param>
    /// <returns>The absolute path.</returns>
    /// <exception cref="InvalidOperationException">No Refit.slnx was found above the test binary.</exception>
    private static string FindRepositoryFile(string relativePath)
    {
        // Walked from the test binary rather than taken from [CallerFilePath], which deterministic
        // CI builds rewrite to a placeholder root.
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Refit.slnx");
            if (File.Exists(candidate))
            {
                return Path.Combine(directory.FullName, relativePath);
            }
        }

        throw new InvalidOperationException(
            $"Refit.slnx was not found above '{AppContext.BaseDirectory}', so '{relativePath}' could not be resolved.");
    }
}
