// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Supplies aliased, repeated and explicitly null form fields.</summary>
internal sealed class ContactForm
{
    /// <summary>Gets the value sent under the form field name 'name'.</summary>
    [AliasAs("name")]
    public string FullName { get; init; } = "Ada Lovelace";

    /// <summary>Gets the values sent as separate Tags form fields.</summary>
    [Query(CollectionFormat.Multi)]
    public string[] Tags { get; init; } = ["math", "code"];

    /// <summary>Gets the optional note sent as an empty field when null.</summary>
    [Query(SerializeNull = true)]
    public string? Note { get; init; }
}
