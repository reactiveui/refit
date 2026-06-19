// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Fixture whose header collection has an unsupported value type, used to verify Refit rejects it.</summary>
public interface IHeaderCollectionWrongType
{
    /// <summary>Endpoint declaring a header collection with an object value type.</summary>
    /// <param name="collection">Header collection with an invalid value type.</param>
    /// <returns>The response body.</returns>
    [Get("/")]
    Task<string> GetValue([HeaderCollection] IDictionary<string, object> collection);
}
