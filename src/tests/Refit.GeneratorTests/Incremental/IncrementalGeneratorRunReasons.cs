// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;

namespace Refit.GeneratorTests.Incremental;

/// <summary>Captures the expected incremental run reasons for the generator's pipeline steps.</summary>
/// <param name="BuildRefitStep">The expected run reason for the build-Refit step.</param>
internal sealed record IncrementalGeneratorRunReasons(
    IncrementalStepRunReason BuildRefitStep)
{
    /// <summary>The expected reasons for a brand-new generator run.</summary>
    internal static readonly IncrementalGeneratorRunReasons New =
        new(IncrementalStepRunReason.New);

    /// <summary>The expected reasons for a cached generator run.</summary>
    internal static readonly IncrementalGeneratorRunReasons Cached =
        new(IncrementalStepRunReason.Cached);

    /// <summary>The expected reasons when both steps are modified.</summary>
    internal static readonly IncrementalGeneratorRunReasons Modified = Cached with
    {
        BuildRefitStep = IncrementalStepRunReason.Modified
    };

    /// <summary>The expected reasons when only the source changed.</summary>
    internal static readonly IncrementalGeneratorRunReasons ModifiedSource = Cached with
    {
        BuildRefitStep = IncrementalStepRunReason.Modified
    };
}
