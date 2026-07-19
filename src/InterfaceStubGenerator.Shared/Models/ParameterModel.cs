// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Describes a method parameter for the source generator.</summary>
/// <param name="MetadataName">The parameter's metadata name.</param>
/// <param name="Type">The parameter's type name.</param>
/// <param name="Annotation">A value indicating whether the parameter is nullable-annotated.</param>
/// <param name="IsGeneric">A value indicating whether the parameter type is a generic type parameter.</param>
internal readonly record struct ParameterModel(
    string MetadataName,
    string Type,
    bool Annotation,
    bool IsGeneric);
