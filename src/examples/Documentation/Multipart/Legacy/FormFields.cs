// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Supplies text fields flattened by the reflection multipart builder.</summary>
internal sealed class FormFields
{
    /// <summary>Gets the title emitted under its explicit caption alias.</summary>
    [AliasAs("caption")]
    public string Title { get; init; } = "Annual report";

    /// <summary>Gets the values emitted as separate Tags text fields.</summary>
    [Query(CollectionFormat.Multi)]
    public string[] Tags { get; init; } = ["math", "code"];

    /// <summary>Gets the optional note emitted as an empty text field when null.</summary>
    [Query(SerializeNull = true)]
    public string? Note { get; init; }
}
