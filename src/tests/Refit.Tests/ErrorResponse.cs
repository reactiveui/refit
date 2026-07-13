// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A deserialization fixture representing an API error payload.</summary>
public class ErrorResponse
{
    /// <summary>Gets or sets the collection of error messages.</summary>
    public string[]? Errors { get; init; }
}
