// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>A model whose nested object is composed into <c>parent.child</c> form fields.</summary>
public class NestedFormObjectUploadModel
{
    /// <summary>Gets or sets the top-level title field.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the nested author whose properties compose under <c>Author.*</c>.</summary>
    public FormObjectUploadModel? Author { get; set; }
}
