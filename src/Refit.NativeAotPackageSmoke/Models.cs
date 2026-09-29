// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.NativeAotPackageSmoke;

/// <summary>The person the smoke API reads, streams, uploads and pages through.</summary>
/// <param name="Id">The person's identifier.</param>
/// <param name="Name">The person's name.</param>
public sealed record Person(int Id, string Name);

/// <summary>The error body the smoke API returns for a rejected request.</summary>
/// <param name="Code">The machine-readable error code.</param>
/// <param name="Message">The human-readable message.</param>
public sealed record Failure(string Code, string Message);

/// <summary>The metadata part serialized as JSON inside a multipart upload.</summary>
/// <param name="Caption">The photo caption.</param>
/// <param name="Tags">The photo tags.</param>
public sealed record PhotoMetadata(string Caption, List<string> Tags);

/// <summary>One page of people with the cursor for the next page.</summary>
public sealed class PersonPage
{
    /// <summary>Gets or sets the people on this page.</summary>
    public List<Person> Items { get; set; } = [];

    /// <summary>Gets or sets the cursor for the next page, or null on the last page.</summary>
    public string? Next { get; set; }
}

/// <summary>A reply type the JSON context deliberately does not describe.</summary>
/// <param name="Value">An ignored value.</param>
public sealed record Undescribed(string Value);

/// <summary>A search the custom query converter flattens into query parameters.</summary>
/// <param name="Text">The search text.</param>
/// <param name="Limit">The most results to return.</param>
public sealed record PersonSearch(string Text, int Limit);
