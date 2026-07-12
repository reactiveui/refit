// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Model describing a Refit method for the source generator.</summary>
/// <param name="Name">The name of the method.</param>
/// <param name="ReturnType">The fully qualified return type of the method.</param>
/// <param name="ContainingType">The fully qualified type that declares the method.</param>
/// <param name="DeclaredMethod">The declared method signature.</param>
/// <param name="ReturnTypeMetadata">Metadata describing the shape of the return type.</param>
/// <param name="Request">The parsed request metadata for Refit methods.</param>
/// <param name="Parameters">The method parameters.</param>
/// <param name="Constraints">The generic type constraints for the method.</param>
/// <param name="IsExplicitInterface">A value indicating whether the method is an explicit interface implementation.</param>
/// <param name="RequiresUnreferencedCode">Whether the interface method declares <c>[RequiresUnreferencedCode]</c>, which a
/// reflection-fallback implementation must mirror instead of suppressing IL2026.</param>
/// <param name="RequiresDynamicCode">Whether the interface method declares <c>[RequiresDynamicCode]</c>, which a
/// reflection-fallback implementation must mirror instead of suppressing IL3050.</param>
internal sealed record MethodModel(
    string Name,
    string ReturnType,
    string ContainingType,
    string DeclaredMethod,
    ReturnTypeInfo ReturnTypeMetadata,
    RequestModel Request,
    ImmutableEquatableArray<ParameterModel> Parameters,
    ImmutableEquatableArray<TypeConstraint> Constraints,
    bool IsExplicitInterface,
    bool RequiresUnreferencedCode,
    bool RequiresDynamicCode);
