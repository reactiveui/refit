// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Frozen;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Refit.GeneratorTests;

/// <summary>
/// Tests the assembly references of the analyzers the Refit package ships.
/// <para>
/// The package ships the generator, analyzers and code fixes on their own, with none of their
/// dependencies, so every reference has to be satisfied by whatever host is running the compiler.
/// Visual Studio's host is .NET Framework and binds the <c>System.*</c> facades through redirects that
/// stop at the versions Roslyn itself carries; a reference above that throws <c>FileNotFoundException</c>
/// on load, which surfaces as CS8784 and no generated clients. A command-line build never reproduces it,
/// because the .NET host resolves facades from the shared framework whatever version is asked for -
/// which is why a transitive package bump can raise a reference here and pass CI while breaking the IDE.
/// </para>
/// </summary>
public class PackagedAnalyzerReferenceTests
{
    /// <summary>The facade versions the compiler host supplies, keyed by simple assembly name.</summary>
    private static readonly FrozenDictionary<string, Version> HostSuppliedFacades =
        new Dictionary<string, Version> { ["System.Memory"] = new(4, 0, 1, 2), ["System.Collections.Immutable"] = new(7, 0, 0, 0) }
            .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>Verifies every declared analyzer was built, so the reference check cannot pass vacuously.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EveryPackagedAnalyzerAssemblyIsBuilt()
    {
        var missing = RefitPackageLayout.GetPackagedAnalyzers()
            .Select(RefitPackageLayout.ResolveBuildOutput)
            .Where(static path => !File.Exists(path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        await Assert.That(missing).IsEmpty();
    }

    /// <summary>
    /// Verifies each packaged analyzer references only assemblies the compiler host supplies, at the
    /// versions it supplies them.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PackagedAnalyzersReferenceOnlyHostSuppliedAssemblyVersions()
    {
        var minimumCompilerVersion = Version.Parse(AnalyzerPackagingTests.ReadMinimumCompilerVersion());
        var roslynVersion = new Version(minimumCompilerVersion.Major, minimumCompilerVersion.Minor, 0, 0);
        var unsupported = new List<string>();

        foreach (var analyzer in RefitPackageLayout.GetPackagedAnalyzers())
        {
            var path = RefitPackageLayout.ResolveBuildOutput(analyzer);
            if (!File.Exists(path))
            {
                continue;
            }

            foreach (var (name, version) in ReadAssemblyReferences(path))
            {
                var supported = name switch
                {
                    "netstandard" => true,
                    _ when name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) => version == roslynVersion,
                    _ => HostSuppliedFacades.TryGetValue(name, out var supportedVersion) && version == supportedVersion,
                };

                if (!supported)
                {
                    unsupported.Add($"{analyzer.FileName} -> {name}, Version={version}");
                }
            }
        }

        await Assert.That(unsupported.Order(StringComparer.Ordinal).ToArray()).IsEmpty();
    }

    /// <summary>Reads the assembly references recorded in a managed assembly's metadata.</summary>
    /// <param name="path">The assembly path.</param>
    /// <returns>The referenced simple names and versions.</returns>
    private static List<(string Name, Version Version)> ReadAssemblyReferences(string path)
    {
        using var stream = File.OpenRead(path);
        using var portableExecutable = new PEReader(stream);
        var metadata = portableExecutable.GetMetadataReader();
        var references = new List<(string Name, Version Version)>();

        foreach (var handle in metadata.AssemblyReferences)
        {
            var reference = metadata.GetAssemblyReference(handle);
            references.Add((metadata.GetString(reference.Name), reference.Version));
        }

        return references;
    }
}
