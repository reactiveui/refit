// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Refit fixture interface exercising request-builder caching across method shapes.</summary>
public interface IGeneralRequests
{
    /// <summary>Sends a POST request with no parameters.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
    Task Empty();

    /// <summary>Sends a POST request with a single string parameter.</summary>
    /// <param name="id">The identifier value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
    Task SingleParameter(string id);

    /// <summary>Sends a POST request with two string parameters.</summary>
    /// <param name="id">The identifier value.</param>
    /// <param name="name">The name value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
    Task MultiParameter(string id, string name);

    /// <summary>Sends a POST request with two string parameters and one generic parameter.</summary>
    /// <typeparam name="TValue">The type of the generic parameter.</typeparam>
    /// <param name="id">The identifier value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="generic">The generic value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
    Task SingleGenericMultiParameter<TValue>(string id, string name, TValue generic);
}
