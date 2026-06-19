// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Model describing an interface property implemented by the generated stub.</summary>
/// <param name="Name">The property name.</param>
/// <param name="Type">The fully qualified property type.</param>
/// <param name="Annotation">A value indicating whether the property is nullable-annotated.</param>
/// <param name="ContainingType">The fully qualified interface that declares the property.</param>
/// <param name="RequestPropertyKey">The request-property key, or empty when the property is not request-bound.</param>
/// <param name="HasGetter">A value indicating whether the property has a getter.</param>
/// <param name="HasSetter">A value indicating whether the property has a setter.</param>
/// <param name="IsSatisfiedByGeneratedMember">A value indicating whether an existing generated member satisfies this interface property.</param>
/// <param name="IsExplicitInterface">A value indicating whether the property is an explicit interface implementation.</param>
internal sealed record InterfacePropertyModel(
    string Name,
    string Type,
    bool Annotation,
    string ContainingType,
    string RequestPropertyKey,
    bool HasGetter,
    bool HasSetter,
    bool IsSatisfiedByGeneratedMember,
    bool IsExplicitInterface);
