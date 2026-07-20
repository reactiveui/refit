// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>An API whose route is bound to a property of an object argument that is also flattened into the query string,
/// exercising the reflection builder's path-bound-property skip when flattening the same object.</summary>
public interface IReflectionObjectPathApi
{
    /// <summary>Binds the object's identifier to a path segment while flattening its remaining properties into the query.</summary>
    /// <param name="request">The request object supplying both the path segment and the query values.</param>
    /// <returns>The response body.</returns>
    [Get("/users/{request.Id}/detail")]
    Task<string> Detail(ReflectionCachingQueryModel request);
}
