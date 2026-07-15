// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Marks a parameter as supplying the complete, absolute request URI, dispatching the call to that URL (which may be a
/// different host) and bypassing the client's <c>BaseAddress</c>. This is Refit's equivalent of Retrofit's <c>@Url</c>.
/// </summary>
/// <remarks>
/// The parameter must be a <see cref="string"/> or a <see cref="System.Uri"/> whose value is an absolute URI; a
/// relative or otherwise invalid value throws an <see cref="ArgumentException"/> when the request is built. When a
/// method has a <c>[Url]</c> parameter its HTTP method attribute path template must be empty, because <c>[Url]</c>
/// already provides the full URL; any <c>[Query]</c> parameters are still appended to that URL's query string.
/// </remarks>
/// <example>
/// <code>
/// interface IFileApi
/// {
///   [Get("")]
///   Task&lt;Stream&gt; DownloadAsync([Url] string absoluteUrl, [Query] string token);
/// }
/// </code>
/// Calling <c>api.DownloadAsync("https://cdn.example.com/file.bin", "abc")</c> dispatches a GET to
/// <c>https://cdn.example.com/file.bin?token=abc</c> regardless of the client's base address.
/// </example>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class UrlAttribute : Attribute;
