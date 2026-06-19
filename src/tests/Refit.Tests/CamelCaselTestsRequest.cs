// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Query object fixture exercising <see cref="CamelCaseUrlParameterKeyFormatter"/> key formatting.</summary>
public class CamelCaselTestsRequest
{
    /// <summary>Gets or sets a value whose property name is already camelCased once the leading letter is lowered.</summary>
    public string? AlreadyCamelCased { get; set; }

    /// <summary>Gets or sets a value whose property name is lowered to "notcamelCased" by the formatter.</summary>
    public string? NotcamelCased { get; set; }
}
