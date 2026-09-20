// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Declares how a method that returns <see cref="PagedEnumerable{TPage, TItem}"/> reads the items and the continuation from
/// each page. The source generator emits the request and the paging loop, so the method is called like any other and returns
/// the lazy sequence.
/// </summary>
/// <remarks>
/// Members of the page type are named as strings, and the generator resolves them at compile time into direct property
/// accesses, so no reflection is involved and a misspelled name is a build error. A dotted name such as
/// <c>Content.Documents</c> reads through nested members, which is how the members of an <see cref="ApiResponse{T}"/> page
/// are reached. A method that returns a token-based sequence marks one parameter with <see cref="PageTokenAttribute"/> and
/// names exactly one of <see cref="Next"/>, <see cref="NextHeader"/> or <see cref="Total"/>. A method with no such parameter
/// follows links, names <see cref="Next"/> or <see cref="NextHeader"/>, and states which origins a link may point at with
/// one of <see cref="Origins"/>, <see cref="SameOrigin"/> or <see cref="AnyOrigin"/>. The method cannot declare a
/// cancellation token, because enumeration supplies it, and cannot be generic.
/// </remarks>
/// <example>
/// <code>
/// [Get("/{bucket}")]
/// [Paged(Next = nameof(ListBucketResult.NextContinuationToken))]
/// PagedEnumerable&lt;ListBucketResult, S3Object&gt; ListObjects(string bucket, [PageToken, AliasAs("continuation-token")] string? continuationToken = null);
/// </code>
/// </example>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PagedAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the member of the page that holds the items. When omitted, the page type must have exactly one member
    /// that is a sequence of the item type, and the generator uses it.
    /// </summary>
    public string? Items { get; set; }

    /// <summary>
    /// Gets or sets the member of the page that holds the continuation: the cursor, the next offset, or the next link. A
    /// <see langword="null"/> or empty value ends the sequence, so the member must be nullable.
    /// </summary>
    public string? Next { get; set; }

    /// <summary>
    /// Gets or sets the response header that holds the continuation, for APIs that return it outside the body. The page
    /// type must be an <see cref="ApiResponse{T}"/> or an <see cref="IApiResponse"/>. The <c>Link</c> header is read for
    /// its <c>rel="next"</c> relation; any other header is read as the whole value.
    /// </summary>
    public string? NextHeader { get; set; }

    /// <summary>
    /// Gets or sets the member of the page that holds the total number of items, for offset paging. The sequence ends when
    /// the offset reaches it, and the token parameter must be an <see cref="int"/>.
    /// </summary>
    public string? Total { get; set; }

    /// <summary>Gets or sets the origins a next link may point at, such as <c>https://api.github.com</c>.</summary>
    public string[]? Origins { get; set; }

    /// <summary>Gets or sets a value indicating whether a next link may only point at the origin of the client's base address.</summary>
    public bool SameOrigin { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a next link may point at any http or https origin. The client's credentials
    /// are sent to whatever origin the server names, so prefer <see cref="Origins"/> or <see cref="SameOrigin"/>.
    /// </summary>
    public bool AnyOrigin { get; set; }
}
