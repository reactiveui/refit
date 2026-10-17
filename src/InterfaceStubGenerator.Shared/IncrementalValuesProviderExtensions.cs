// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Extension methods for registering incremental generator outputs.</summary>
internal static class IncrementalValuesProviderExtensions
{
    /// <summary>Extensions for registering outputs on an <see cref="IncrementalGeneratorInitializationContext"/>.</summary>
    /// <param name="context">The generator initialization context to register outputs on.</param>
    extension(
        IncrementalGeneratorInitializationContext context)
    {
        /// <summary>Registers an implementation source output for the provided mappers.</summary>
        /// <param name="model">The interfaces stubs.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EmitSource(
            in IncrementalValuesProvider<InterfaceModel> model) =>
            context.RegisterImplementationSourceOutput(
                model,
                static (spc, model) =>
                {
                    var mapperText = Emitter.EmitInterface(model);
                    spc.AddSource(model.FileName, mapperText);
                });
    }
}
