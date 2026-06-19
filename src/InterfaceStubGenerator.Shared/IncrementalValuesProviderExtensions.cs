// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if ROSLYN_4
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Extension methods for registering incremental generator outputs.</summary>
internal static class IncrementalValuesProviderExtensions
{
    /// <summary>Extensions for registering outputs on an <see cref="IncrementalGeneratorInitializationContext"/>.</summary>
    /// <param name="context">The generator initialization context to register outputs on.</param>
    extension(IncrementalGeneratorInitializationContext context)
    {
        /// <summary>Registers an output node into an <see cref="IncrementalGeneratorInitializationContext"/> to output a diagnostic.</summary>
        /// <param name="diagnostic">The input <see cref="IncrementalValuesProvider{TValues}"/> sequence of diagnostics.</param>
        public void ReportDiagnostics(
            IncrementalValuesProvider<Diagnostic> diagnostic) =>
            context.RegisterSourceOutput(
                diagnostic,
                static (context, diagnostic) => context.ReportDiagnostic(diagnostic));

        /// <summary>Registers an output node into an <see cref="IncrementalGeneratorInitializationContext"/> to output diagnostics.</summary>
        /// <param name="diagnostics">The input <see cref="IncrementalValuesProvider{TValues}"/> sequence of diagnostics.</param>
        public void ReportDiagnostics(
            IncrementalValueProvider<ImmutableEquatableArray<Diagnostic>> diagnostics) =>
            context.RegisterSourceOutput(
                diagnostics,
                static (context, diagnostics) =>
                {
                    foreach (var diagnostic in diagnostics)
                    {
                        context.ReportDiagnostic(diagnostic);
                    }
                });

        /// <summary>Registers an implementation source output for the provided mappers.</summary>
        /// <param name="model">The interfaces stubs.</param>
        public void EmitSource(
            IncrementalValuesProvider<InterfaceModel> model) =>
            context.RegisterImplementationSourceOutput(
                model,
                static (spc, model) =>
                {
                    var mapperText = Emitter.EmitInterface(model);
                    spc.AddSource(model.FileName, mapperText);
                });
    }
}
#endif
