// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Runtime.CompilerServices;
using VerifyTests.DiffPlex;

namespace Refit.GeneratorTests;

/// <summary>Initializes Verify settings for the generator snapshot tests.</summary>
public static class ModuleInitializer
{
    /// <summary>Configures the snapshot path and Verify source generator support.</summary>
    [ModuleInitializer]
    public static void Init()
    {
        DerivePathInfo(static (file, _, type, method) =>
            new(Path.Combine(Path.GetDirectoryName(file) ?? AppContext.BaseDirectory, "_snapshots"), type.Name, method.Name));

        // The generated GeneratedCodeAttribute stamps the generator assembly version, which is not stable
        // across builds and CI. Scrub the line so snapshots stay version-independent.
        VerifierSettings.ScrubLinesContaining("System.CodeDom.Compiler.GeneratedCodeAttribute");

        VerifySourceGenerators.Initialize();
        VerifyDiffPlex.Initialize(OutputType.Compact);
    }
}
