// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Marks the parameter of a <see cref="PagedAttribute"/> method that carries the continuation: a cursor, an offset, a page
/// number or any other value the API takes to identify a page. The generated client sends the caller's argument with the
/// first request and the continuation read from each page with the next one.
/// </summary>
/// <remarks>
/// The parameter binds like any other parameter, so <see cref="AliasAsAttribute"/>, <see cref="HeaderAttribute"/> and
/// <see cref="QueryAttribute"/> decide whether it travels in the query string, a header or the path. A method that follows
/// links has no such parameter.
/// </remarks>
/// <example>
/// <code>
/// [Get("/{bucket}")]
/// [Paged(Next = nameof(ListBucketResult.NextContinuationToken))]
/// PagedEnumerable&lt;ListBucketResult, S3Object&gt; ListObjects(string bucket, [PageToken, AliasAs("continuation-token")] string? continuationToken = null);
/// </code>
/// </example>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class PageTokenAttribute : Attribute;
