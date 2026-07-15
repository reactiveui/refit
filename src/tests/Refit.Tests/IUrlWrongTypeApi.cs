// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>An invalid API whose <c>[Url]</c> parameter is neither a string nor a <see cref="Uri"/>.</summary>
public interface IUrlWrongTypeApi
{
    /// <summary>Declares a <c>[Url]</c> parameter of an unsupported type.</summary>
    /// <param name="url">The absolute request URL, declared with an unsupported type.</param>
    /// <returns>The response body.</returns>
    [Get("")]
    Task<string> Invalid([Url] int url);
}
