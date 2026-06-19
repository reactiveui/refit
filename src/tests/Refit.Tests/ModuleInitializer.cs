// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using VerifyTests.DiffPlex;

namespace Refit.Tests;

/// <summary>Initializes Verify settings for the test assembly at module load.</summary>
internal static class ModuleInitializer
{
    /// <summary>Configures Verify snapshot paths and source generator support.</summary>
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries", Justification = "Required to initialize Verify settings for the test assembly.")]
    public static void Init()
    {
        DerivePathInfo((file, _, type, method) => new(Path.Combine(Path.GetDirectoryName(file) ?? string.Empty, "_snapshots"), type.Name, method.Name));

        VerifySourceGenerators.Initialize();
        VerifyDiffPlex.Initialize(OutputType.Compact);
    }
}
