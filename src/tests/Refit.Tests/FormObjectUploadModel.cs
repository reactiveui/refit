// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>A model flattened into individual multipart form fields by <see cref="FormObjectAttribute"/>.</summary>
public class FormObjectUploadModel
{
    /// <summary>Gets or sets the display name, aliased to a snake-cased form field.</summary>
    [AliasAs("full_name")]
    public string? Name { get; set; }

    /// <summary>Gets or sets the age, whose field name comes from the key formatter.</summary>
    public int Age { get; set; }

    /// <summary>Gets the roles rendered as a single delimited field per the collection format.</summary>
    public string[]? Roles { get; init; }
}
