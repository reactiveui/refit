// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>The request-shaping attributes read from a single method parameter in one metadata pass, so the request
/// builder's construction classifies each parameter without re-enumerating its attribute records per lookup.</summary>
/// <param name="Query">The parameter's <see cref="QueryAttribute"/>, or null.</param>
/// <param name="Header">The parameter's <see cref="HeaderAttribute"/>, or null.</param>
/// <param name="HeaderCollection">The parameter's <see cref="HeaderCollectionAttribute"/>, or null.</param>
/// <param name="Property">The parameter's <see cref="PropertyAttribute"/>, or null.</param>
/// <param name="Authorize">The parameter's <see cref="AuthorizeAttribute"/>, or null.</param>
/// <param name="Body">The parameter's <see cref="BodyAttribute"/>, or null.</param>
/// <param name="Url">The parameter's <see cref="UrlAttribute"/>, or null.</param>
/// <param name="FormObject">The parameter's <see cref="FormObjectAttribute"/>, or null.</param>
internal readonly record struct ParameterAttributeSet(
    QueryAttribute? Query,
    HeaderAttribute? Header,
    HeaderCollectionAttribute? HeaderCollection,
    PropertyAttribute? Property,
    AuthorizeAttribute? Authorize,
    BodyAttribute? Body,
    UrlAttribute? Url,
    FormObjectAttribute? FormObject);
