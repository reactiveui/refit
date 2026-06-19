// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET48
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

using PublicApiGenerator;

using VerifyTUnit;

namespace Refit.Tests;

/// <summary>A helper for doing API approvals.</summary>
[ExcludeFromCodeCoverage]
public static class ApiExtensions
{
    /// <summary>API approval helpers on <see cref="System.Reflection.Assembly"/>.</summary>
    /// <param name="assembly">The assembly whose public API is verified.</param>
    extension(Assembly assembly)
    {
        /// <summary>Checks to make sure the API is approved.</summary>
        /// <param name="namespaces">The namespaces.</param>
        /// <param name="filePath">The caller file path.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CheckApproval(string[] namespaces, [CallerFilePath] string filePath = "")
        {
            var generatorOptions = new ApiGeneratorOptions { AllowNamespacePrefixes = namespaces };
            var apiText = assembly.GeneratePublicApi(generatorOptions);
            await Verifier.Verify(apiText, null, filePath)
                .UniqueForRuntimeAndVersion()
                .ScrubEmptyLines()
                .ScrubLines(l =>
                    l.StartsWith("[assembly: AssemblyVersion(", StringComparison.InvariantCulture) ||
                    l.StartsWith("[assembly: AssemblyFileVersion(", StringComparison.InvariantCulture) ||
                    l.StartsWith("[assembly: AssemblyInformationalVersion(", StringComparison.InvariantCulture) ||
                    l.StartsWith("[assembly: System.Reflection.AssemblyMetadata(", StringComparison.InvariantCulture));
        }
    }
}
#endif
