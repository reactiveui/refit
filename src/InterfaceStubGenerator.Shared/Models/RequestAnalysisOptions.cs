// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Carries the analyzer's request validation options and reporting callback.</summary>
/// <param name="GeneratedRequestBuilding">Whether the consumer enables generated requests.</param>
/// <param name="ReportDiagnostic">The analyzer's diagnostic reporting callback.</param>
/// <param name="CancellationToken">The analysis cancellation token.</param>
internal readonly record struct RequestAnalysisOptions(
    bool GeneratedRequestBuilding,
    Action<Diagnostic> ReportDiagnostic,
    CancellationToken CancellationToken);
