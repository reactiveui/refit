// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Parsed request-binding metadata for one method parameter.</summary>
/// <param name="Name">The parameter metadata name.</param>
/// <param name="Type">The fully-qualified parameter type.</param>
/// <param name="Kind">The generated request binding kind.</param>
/// <param name="CanBeNull">Whether generated code must null-check the parameter before dereferencing.</param>
/// <param name="HeaderName">The request header name, when this is a header parameter.</param>
/// <param name="PropertyKey">The request property key, when this is a property parameter.</param>
/// <param name="BodySerializationMethod">The Refit body serialization method name, when this is a body parameter.</param>
/// <param name="BodyBufferMode">The body buffering mode, when this is a body parameter.</param>
internal sealed record RequestParameterModel(
    string Name,
    string Type,
    RequestParameterKind Kind,
    bool CanBeNull,
    string HeaderName,
    string PropertyKey,
    string BodySerializationMethod,
    BodyBufferMode BodyBufferMode);
