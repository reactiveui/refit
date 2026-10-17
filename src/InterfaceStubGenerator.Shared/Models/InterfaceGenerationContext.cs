// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Carries the parsing state for one interface.</summary>
/// <param name="Diagnostics">The diagnostic collector.</param>
/// <param name="Options">The consumer's generation options.</param>
/// <param name="Symbols">The resolved compilation symbols.</param>
/// <param name="Capabilities">The supported target APIs and syntax.</param>
/// <param name="Caches">The alias and type classification caches.</param>
/// <param name="PathPrefix">The interface route prefix.</param>
internal readonly record struct InterfaceGenerationContext(
    List<Diagnostic> Diagnostics,
    GenerationOptionsModel Options,
    GenerationSymbolsModel Symbols,
    GenerationCapabilitiesModel Capabilities,
    GenerationCachesModel Caches,
    string PathPrefix = "");
