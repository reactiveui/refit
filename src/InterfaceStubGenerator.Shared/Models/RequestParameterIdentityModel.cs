// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes a parameter's name, type and nullability.</summary>
/// <param name="Name">The parameter metadata name.</param>
/// <param name="Type">The fully-qualified parameter type.</param>
/// <param name="CanBeNull">Whether generated code must null-check the parameter before dereferencing.</param>
internal readonly record struct RequestParameterIdentityModel(
    string Name,
    string Type,
    bool CanBeNull);
