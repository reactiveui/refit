// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes RequestGenerationModel metadata.</summary>
/// <param name="CanGenerateInline">Whether this method is eligible for generated request construction.</param>
/// <param name="AdapterTypeExpression">The closed <c>IReturnTypeAdapter</c> type expression when the return type is surfaced by an adapter, so the
/// generated method emits an <c>Adapt</c> call; otherwise <see langword="null"/>. The result-type fields already carry the adapter's wrapped result
/// type.</param>
/// <param name="Fallback">why the request is not generated inline, and which parameter caused it; <see cref="InlineFallback.None"/> when <see
/// cref="CanGenerateInline"/> is set. The generator only reads <see cref="CanGenerateInline"/>; the RF006 analyzer turns this into its explanation, so both
/// always come from the same classification.</param>
internal readonly record struct RequestGenerationModel(
    bool CanGenerateInline,
    string? AdapterTypeExpression,
    InlineFallback Fallback);
