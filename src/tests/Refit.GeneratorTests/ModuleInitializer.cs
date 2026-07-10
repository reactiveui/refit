// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Runtime.CompilerServices;
using VerifyTests.DiffPlex;

namespace Refit.GeneratorTests;

/// <summary>Initializes Verify settings for the generator snapshot tests.</summary>
public static class ModuleInitializer
{
    /// <summary>Carriage return, which Verify rejects in a verified file.</summary>
    private const byte CarriageReturn = (byte)'\r';

    /// <summary>Configures the snapshot path and Verify source generator support.</summary>
    [ModuleInitializer]
    public static void Init()
    {
        NormalizeSnapshotLineEndings();

        DerivePathInfo(static (file, _, type, method) =>
            new(Path.Combine(Path.GetDirectoryName(file) ?? AppContext.BaseDirectory, "_snapshots"), type.Name, method.Name));

        // The generated GeneratedCodeAttribute stamps the generator assembly version, which is not stable
        // across builds and CI. Scrub the line so snapshots stay version-independent.
        VerifierSettings.ScrubLinesContaining("System.CodeDom.Compiler.GeneratedCodeAttribute");

        VerifySourceGenerators.Initialize();
        VerifyDiffPlex.Initialize(OutputType.Compact);
    }

    /// <summary>Rewrites verified snapshots to LF endings before Verify reads them.</summary>
    /// <param name="sourceFile">The path of this source file, supplied by the compiler.</param>
    /// <remarks>
    /// Verify throws if a verified file contains a carriage return, and offers no way to opt out.
    /// A Windows checkout rewrites the LF stored in git to CRLF, so normalize on disk here rather
    /// than depend on how the repository happens to be cloned. Bytes are rewritten in place so the
    /// byte-order mark is preserved.
    /// </remarks>
    private static void NormalizeSnapshotLineEndings([CallerFilePath] string sourceFile = "")
    {
        var directory = Path.Combine(Path.GetDirectoryName(sourceFile) ?? AppContext.BaseDirectory, "_snapshots");
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.verified.*"))
        {
            var bytes = File.ReadAllBytes(file);
            if (Array.IndexOf(bytes, CarriageReturn) < 0)
            {
                continue;
            }

            File.WriteAllBytes(file, [.. bytes.Where(static b => b != CarriageReturn)]);
        }
    }
}
