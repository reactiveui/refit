// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.LiveObservable;

/// <summary>Exercises generated observable request execution.</summary>
internal interface IObservableApi
{
    /// <summary>Watches one resource.</summary>
    /// <param name="id">The resource identifier.</param>
    /// <param name="q">The query value.</param>
    /// <returns>A cold observable response.</returns>
    [Get("/items/{id}")]
    IObservable<string> Watch(string id, string q);
}
