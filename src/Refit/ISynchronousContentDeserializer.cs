// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>
/// An optional capability for an <see cref="IHttpContentSerializer"/> that can deserialize an already-buffered
/// response body string synchronously. Refit uses this for <see cref="ApiException.GetContentAs{T}"/> and
/// <see cref="ApiException.TryGetContentAs{T}"/>, which let callers peek at an error body from inside an
/// exception filter (<c>catch (ApiException ex) when (ex.TryGetContentAs&lt;Error&gt;(out var error))</c>),
/// where awaiting is not allowed by the CLR.
/// </summary>
public interface ISynchronousContentDeserializer
{
    /// <summary>Deserializes <paramref name="content"/> synchronously into an object of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type to deserialize the content to.</typeparam>
    /// <param name="content">The already-buffered content string.</param>
    /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Type parameter intentionally specified explicitly by callers.")]
    T? DeserializeFromString<T>(string content);
}
