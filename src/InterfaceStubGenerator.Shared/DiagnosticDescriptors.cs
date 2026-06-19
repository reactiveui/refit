// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Diagnostic descriptors reported by the Refit source generator.</summary>
internal static class DiagnosticDescriptors
{
    /// <summary>Diagnostic for a Refit member missing a valid HTTP method attribute.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RS2008", Justification = "Diagnostic IDs are stable and intentionally not tracked in an analyzer release-tracking file.")]
    public static readonly DiagnosticDescriptor InvalidRefitMember =
        new(
            "RF001",
            "Refit types must have Refit HTTP method attributes",
            "Method {0}.{1} either has no Refit HTTP method attribute or you've used something other than a string literal for the 'path' argument",
            "Refit",
            DiagnosticSeverity.Warning,
            true);

    /// <summary>Diagnostic reported when the Refit assembly is not referenced.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RS2008", Justification = "Diagnostic IDs are stable and intentionally not tracked in an analyzer release-tracking file.")]
    public static readonly DiagnosticDescriptor RefitNotReferenced =
        new(
            "RF002",
            "Refit must be referenced",
            "Refit is not referenced. Add a reference to Refit.",
            "Refit",
            DiagnosticSeverity.Error,
            true);
}
