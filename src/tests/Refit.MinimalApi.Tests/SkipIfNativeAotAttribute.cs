// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Refit.MinimalApi.Tests;

/// <summary>Skips tests that intentionally exercise APIs unsupported by Native AOT.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class SkipIfNativeAotAttribute : SkipAttribute
{
    /// <summary>Initializes a new instance of the <see cref="SkipIfNativeAotAttribute"/> class.</summary>
    public SkipIfNativeAotAttribute()
        : base("Reflection-based Minimal API mapping uses runtime JSON metadata and is not Native AOT-safe.")
    {
    }

    /// <inheritdoc/>
    public override Task<bool> ShouldSkip(TestRegisteredContext context) =>
#if NET
        Task.FromResult(!RuntimeFeature.IsDynamicCodeSupported);
#else
        Task.FromResult(false);
#endif
}
