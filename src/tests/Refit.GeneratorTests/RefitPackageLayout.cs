// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Xml.Linq;

namespace Refit.GeneratorTests;

/// <summary>Reads the analyzer layout of the Refit package from Refit.csproj.</summary>
internal static class RefitPackageLayout
{
    /// <summary>The package path prefix that marks an item as a shipped analyzer.</summary>
    private const string AnalyzerPackagePathPrefix = "analyzers/";

    /// <summary>Reads the analyzer items the Refit package ships.</summary>
    /// <returns>The packaged analyzers, in declaration order.</returns>
    internal static List<PackagedAnalyzer> GetPackagedAnalyzers()
    {
        var analyzers = new List<PackagedAnalyzer>();

        foreach (var element in XDocument.Load(FindRepositoryFile("Refit/Refit.csproj")).Descendants("None"))
        {
            var packagePath = Normalize(element.Attribute("PackagePath")?.Value);
            if (!packagePath.StartsWith(AnalyzerPackagePathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var include = Normalize(element.Attribute("Include")?.Value);
            analyzers.Add(new(packagePath, include, include[(include.LastIndexOf('/') + 1)..]));
        }

        return analyzers;
    }

    /// <summary>Resolves the build output an analyzer is packed from.</summary>
    /// <param name="analyzer">The packaged analyzer.</param>
    /// <returns>The absolute path, which may not exist when the component has not been built.</returns>
    internal static string ResolveBuildOutput(PackagedAnalyzer analyzer)
    {
        // The test binary sits in bin/<Configuration>/<tfm>, and the analyzers are packed from the
        // matching bin/<Configuration>/netstandard2.0 of their own projects.
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var relative = analyzer.SourcePath.Replace("$(Configuration)", configuration, StringComparison.Ordinal);

        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindRepositoryFile("Refit/Refit.csproj"))!, relative));
    }

    /// <summary>Resolves a path under the repository source directory.</summary>
    /// <param name="relativePath">The path relative to the directory holding Refit.slnx.</param>
    /// <returns>The absolute path.</returns>
    /// <exception cref="InvalidOperationException">No Refit.slnx was found above the test binary.</exception>
    internal static string FindRepositoryFile(string relativePath)
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

    /// <summary>Normalizes an MSBuild path attribute to forward slashes.</summary>
    /// <param name="value">The raw attribute value.</param>
    /// <returns>The normalized value, or an empty string when the attribute is absent.</returns>
    private static string Normalize(string? value) =>
        value?.Replace('\\', '/') ?? string.Empty;
}
