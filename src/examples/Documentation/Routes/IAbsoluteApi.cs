// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Builds a download request from a caller-supplied absolute URL.</summary>
internal interface IAbsoluteApi
{
    /// <summary>Uses the supplied URL instead of composing a path from the base address.</summary>
    /// <param name="absoluteUrl">The complete download URL that replaces base-address path composition.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Get("")]
    Task<HttpRequestMessage> DownloadAsync([Url] string absoluteUrl);

    /// <summary>Uses an absolute URI object instead of a URL string.</summary>
    /// <param name="absoluteUrl">The complete download URI.</param>
    /// <returns>The unsent request owned by the caller.</returns>
    [Get("/")]
    Task<HttpRequestMessage> DownloadUriAsync([Url] Uri absoluteUrl);
}
