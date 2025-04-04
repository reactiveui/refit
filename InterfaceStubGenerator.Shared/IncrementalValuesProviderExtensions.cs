#if ROSLYN_4
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Refit.Generator;

internal static class IncrementalValuesProviderExtensions
{
    /// <summary>
    /// Registers an output node into an <see cref="IncrementalGeneratorInitializationContext"/> to output a diagnostic.
    /// </summary>
    /// <param name="context">The input <see cref="IncrementalGeneratorInitializationContext"/> instance.</param>
    /// <param name="diagnostic">The input <see cref="IncrementalValuesProvider{TValues}"/> sequence of diagnostics.</param>
    public static void ReportDiagnostics(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<Diagnostic> diagnostic
    )
    {
        context.RegisterSourceOutput(
            diagnostic,
            static (context, diagnostic) => context.ReportDiagnostic(diagnostic)
        );
    }

    /// <summary>
    /// Registers an output node into an <see cref="IncrementalGeneratorInitializationContext"/> to output diagnostics.
    /// </summary>
    /// <param name="context">The input <see cref="IncrementalGeneratorInitializationContext"/> instance.</param>
    /// <param name="diagnostics">The input <see cref="IncrementalValuesProvider{TValues}"/> sequence of diagnostics.</param>
    public static void ReportDiagnostics(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<ImmutableEquatableArray<Diagnostic>> diagnostics
    )
    {
        context.RegisterSourceOutput(
            diagnostics,
            static (context, diagnostics) =>
            {
                foreach (var diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        );
    }

    /// <summary>
    /// Registers an implementation source output for the provided mappers.
    /// </summary>
    /// <param name="context">The context, on which the output is registered.</param>
    /// <param name="model">The interfaces stubs.</param>
    public static void EmitSource(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<InterfaceModel> model
    )
    {
        context.RegisterImplementationSourceOutput(
            model,
            static (spc, model) =>
            {
                var mapperText = Emitter.EmitInterface(model);
                spc.AddSource(model.FileName, mapperText);
            }
        );
    }
}
#endif
