// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Content;

/// <summary>Returns a custom deferred shape recognized at compile time.</summary>
internal interface IAdapterApi
{
    /// <summary>Prepares a person request for the return adapter.</summary>
    /// <returns>A deferred request that the caller invokes once.</returns>
    [Get("/content/person")]
    PersonCall<Person> Read();
}
