using Microsoft.CodeAnalysis;

namespace Refit.Generator;

internal static class DiagnosticDescriptors
{
#pragma warning disable RS2008 // Enable analyzer release tracking
    public static readonly DiagnosticDescriptor InvalidRefitMember =
        new(
            "RF001",
            "Refit types must have Refit HTTP method attributes",
            "Method {0}.{1} either has no Refit HTTP method attribute or you've used something other than a string literal for the 'path' argument",
            "Refit",
            DiagnosticSeverity.Warning,
            true
        );

    public static readonly DiagnosticDescriptor RefitNotReferenced =
        new(
            "RF002",
            "Refit must be referenced",
            "Refit is not referenced. Add a reference to Refit.",
            "Refit",
            DiagnosticSeverity.Error,
            true
        );
#pragma warning restore RS2008 // Enable analyzer release tracking
}
