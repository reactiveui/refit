// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Holds the tracking names used for the incremental generator pipeline steps.</summary>
internal static class RefitGeneratorStepName
{
    /// <summary>The tracking name for the diagnostics reporting step.</summary>
    internal const string ReportDiagnostics = "ReportDiagnostics";

    /// <summary>The tracking name for the Refit stub building step.</summary>
    internal const string BuildRefit = "BuildRefit";
}
