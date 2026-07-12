// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;

namespace Refit.Analyzers;

/// <summary>Diagnostic descriptors reported by Refit analyzers.</summary>
internal static class DiagnosticDescriptors
{
    /// <summary>Diagnostic for a Refit member missing a valid HTTP method attribute.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RS2008", Justification = "Diagnostic IDs are stable and intentionally not tracked in an analyzer release-tracking file.")]
    public static readonly DiagnosticDescriptor InvalidRefitMember =
        new(
            DiagnosticIds.InvalidRefitMember,
            "Refit types must have Refit HTTP method attributes",
            "Method {0}.{1} either has no Refit HTTP method attribute or you've used something other than a string literal for the 'path' argument",
            Category,
            DiagnosticSeverity.Warning,
            true);

    /// <summary>Diagnostic reported when a Refit route contains a backslash.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RS2008", Justification = "Diagnostic IDs are stable and intentionally not tracked in an analyzer release-tracking file.")]
    public static readonly DiagnosticDescriptor InvalidRouteBackslash =
        new(
            DiagnosticIds.InvalidRouteBackslash,
            "Refit routes should use forward slashes",
            "Method {0}.{1} has a route containing a backslash. Use '/' in Refit routes.",
            Category,
            DiagnosticSeverity.Warning,
            true);

    /// <summary>Diagnostic reported when a Refit method has more than one cancellation token.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RS2008", Justification = "Diagnostic IDs are stable and intentionally not tracked in an analyzer release-tracking file.")]
    public static readonly DiagnosticDescriptor MultipleCancellationTokens =
        new(
            DiagnosticIds.MultipleCancellationTokens,
            "Refit methods can only have one CancellationToken parameter",
            "Method {0}.{1} has more than one CancellationToken parameter",
            Category,
            DiagnosticSeverity.Warning,
            true);

    /// <summary>Diagnostic reported when a header collection parameter uses an unsupported type.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RS2008", Justification = "Diagnostic IDs are stable and intentionally not tracked in an analyzer release-tracking file.")]
    public static readonly DiagnosticDescriptor InvalidHeaderCollectionParameter =
        new(
            DiagnosticIds.InvalidHeaderCollectionParameter,
            "HeaderCollection parameters must be IDictionary<string, string>",
            "Parameter {0} on method {1}.{2} is marked with HeaderCollection but must be IDictionary<string, string>",
            Category,
            DiagnosticSeverity.Warning,
            true);

    /// <summary>Diagnostic reported when a Refit method cannot use generated request building and falls back to reflection.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RS2008", Justification = "Diagnostic IDs are stable and intentionally not tracked in an analyzer release-tracking file.")]
    public static readonly DiagnosticDescriptor GeneratedRequestBuildingFallback =
        new(
            DiagnosticIds.GeneratedRequestBuildingFallback,
            "Refit method is not compatible with generated-only client registration",
            "Method {0}.{1} uses request features the Refit source generator cannot build without reflection, so it falls back to the reflection request builder. "
            + "It will throw at runtime when resolved through AddRefitGeneratedClient (generated-only) or under NativeAOT. "
            + "Use RestService.For where reflection is acceptable, or change the method to use only features supported by generated request building.",
            Category,
            DiagnosticSeverity.Warning,
            true);

    /// <summary>Diagnostic reported when a Refit method declares more than one HeaderCollection parameter.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RS2008", Justification = "Diagnostic IDs are stable and intentionally not tracked in an analyzer release-tracking file.")]
    public static readonly DiagnosticDescriptor MultipleHeaderCollections =
        new(
            DiagnosticIds.MultipleHeaderCollections,
            "Refit methods can only have one HeaderCollection parameter",
            "Method {0}.{1} has more than one HeaderCollection parameter",
            Category,
            DiagnosticSeverity.Warning,
            true);

    /// <summary>Diagnostic reported when a Refit method declares more than one Authorize parameter.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RS2008", Justification = "Diagnostic IDs are stable and intentionally not tracked in an analyzer release-tracking file.")]
    public static readonly DiagnosticDescriptor MultipleAuthorizeParameters =
        new(
            DiagnosticIds.MultipleAuthorizeParameters,
            "Refit methods can only have one Authorize parameter",
            "Method {0}.{1} has more than one Authorize parameter",
            Category,
            DiagnosticSeverity.Warning,
            true);

    /// <summary>The diagnostic category for Refit analyzer diagnostics.</summary>
    private const string Category = "Refit";
}
