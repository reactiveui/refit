// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>An API with additive query naming.</summary>
public interface IQueryAttributeNameApi
{
    /// <summary>Builds a scalar query.</summary>
    /// <param name="value">The query value.</param>
    /// <returns>The request.</returns>
    [Get("/query")]
    Task<HttpRequestMessage> Scalar([Query(Name = "client_id")] string value);

    /// <summary>Builds a query whose AliasAs takes precedence.</summary>
    /// <param name="value">The query value.</param>
    /// <returns>The request.</returns>
    [Get("/query")]
    Task<HttpRequestMessage> Alias([Query(Name = "ignored")] [AliasAs("chosen")] string value);

    /// <summary>Builds a repeated collection query.</summary>
    /// <param name="values">The query values.</param>
    /// <returns>The request.</returns>
    [Get("/query")]
    Task<HttpRequestMessage> Collection([Query(CollectionFormat.Multi, Name = "ids")] int[] values);

    /// <summary>Builds an object query.</summary>
    /// <param name="value">The query object.</param>
    /// <returns>The request.</returns>
    [Get("/query")]
    Task<HttpRequestMessage> Object([Query] QueryAttributeNameTests.NamedQueryData value);

    /// <summary>Builds a form body.</summary>
    /// <param name="value">The form object.</param>
    /// <returns>The request.</returns>
    [Post("/form")]
    Task<HttpRequestMessage> Form([Body(BodySerializationMethod.UrlEncoded)] QueryAttributeNameTests.NamedQueryData value);

    /// <summary>Builds a path without treating Query.Name as a path alias.</summary>
    /// <param name="path">The path value.</param>
    /// <param name="value">The query value.</param>
    /// <returns>The request.</returns>
    [Get("/{path}")]
    Task<HttpRequestMessage> Path([Query(Name = "not_path")] string path, [Query(Name = "filter")] string value);
}
