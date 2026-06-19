// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;

namespace Refit.GeneratorTests.Incremental;

/// <summary>Captures the expected incremental run reasons for the generator's pipeline steps.</summary>
/// <param name="BuildRefitStep">The expected run reason for the build-Refit step.</param>
/// <param name="ReportDiagnosticsStep">The expected run reason for the report-diagnostics step.</param>
internal sealed record IncrementalGeneratorRunReasons(
    IncrementalStepRunReason BuildRefitStep,
    IncrementalStepRunReason ReportDiagnosticsStep)
{
    /// <summary>The expected reasons for a brand-new generator run.</summary>
    public static readonly IncrementalGeneratorRunReasons New =
        new(IncrementalStepRunReason.New, IncrementalStepRunReason.New);

    /// <summary>The expected reasons for a cached generator run.</summary>
    /// <remarks>The compilation step is always modified because a new compilation is passed each time.</remarks>
    public static readonly IncrementalGeneratorRunReasons Cached =
        new(IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);

    /// <summary>The expected reasons when both steps are modified.</summary>
    public static readonly IncrementalGeneratorRunReasons Modified = Cached with
    {
        ReportDiagnosticsStep = IncrementalStepRunReason.Modified,
        BuildRefitStep = IncrementalStepRunReason.Modified
    };

    /// <summary>The expected reasons when only the source changed.</summary>
    public static readonly IncrementalGeneratorRunReasons ModifiedSource = Cached with
    {
        ReportDiagnosticsStep = IncrementalStepRunReason.Unchanged,
        BuildRefitStep = IncrementalStepRunReason.Modified
    };
}
