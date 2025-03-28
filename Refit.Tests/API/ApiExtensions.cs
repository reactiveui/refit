// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET48
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

using PublicApiGenerator;

using VerifyXunit;

namespace Refit.Tests;

/// <summary>
/// A helper for doing API approvals.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ApiExtensions
{
    /// <summary>
    /// Checks to make sure the API is approved.
    /// </summary>
    /// <param name="assembly">The assembly that is being checked.</param>
    /// <param name="namespaces">The namespaces.</param>
    /// <param name="filePath">The caller file path.</param>
    /// <returns>
    /// A Task.
    /// </returns>
    public static async Task CheckApproval(this Assembly assembly, string[] namespaces, [CallerFilePath] string filePath = "")
    {
        var generatorOptions = new ApiGeneratorOptions { AllowNamespacePrefixes = namespaces };
        var apiText = assembly.GeneratePublicApi(generatorOptions);
        var result = await Verifier.Verify(apiText, null, filePath)
            .UniqueForRuntimeAndVersion()
            .ScrubEmptyLines()
            .ScrubLines(l =>
                l.StartsWith("[assembly: AssemblyVersion(", StringComparison.InvariantCulture) ||
                l.StartsWith("[assembly: AssemblyFileVersion(", StringComparison.InvariantCulture) ||
                l.StartsWith("[assembly: AssemblyInformationalVersion(", StringComparison.InvariantCulture) ||
                l.StartsWith("[assembly: System.Reflection.AssemblyMetadata(", StringComparison.InvariantCulture));
    }
}
#endif
