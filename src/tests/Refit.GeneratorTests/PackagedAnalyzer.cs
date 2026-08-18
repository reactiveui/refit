// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>An analyzer assembly the Refit package ships.</summary>
/// <param name="PackagePath">The folder inside the package the assembly is placed in.</param>
/// <param name="SourcePath">The build output path, relative to Refit.csproj, still holding $(Configuration).</param>
/// <param name="FileName">The assembly file name.</param>
internal readonly record struct PackagedAnalyzer(string PackagePath, string SourcePath, string FileName);
