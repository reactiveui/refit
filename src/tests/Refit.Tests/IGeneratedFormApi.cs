// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit API posting a strongly-typed URL-encoded form body.</summary>
public interface IGeneratedFormApi
{
    /// <summary>Posts a URL-encoded form body.</summary>
    /// <param name="form">The form payload.</param>
    /// <returns>The response string.</returns>
    [Post("/form")]
    Task<string> PostForm([Body(BodySerializationMethod.UrlEncoded)] GeneratedFormData form);
}
