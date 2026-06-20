// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Analyzers;

/// <summary>Diagnostic identifiers reported by Refit analyzers.</summary>
internal static class DiagnosticIds
{
    /// <summary>Diagnostic for a Refit member missing a valid HTTP method attribute.</summary>
    public const string InvalidRefitMember = "RF001";

    /// <summary>Diagnostic reported when a Refit route contains a backslash.</summary>
    public const string InvalidRouteBackslash = "RF003";

    /// <summary>Diagnostic reported when a Refit method has more than one cancellation token.</summary>
    public const string MultipleCancellationTokens = "RF004";

    /// <summary>Diagnostic reported when a header collection parameter uses an unsupported type.</summary>
    public const string InvalidHeaderCollectionParameter = "RF005";
}
