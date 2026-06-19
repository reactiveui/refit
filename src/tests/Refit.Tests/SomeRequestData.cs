// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A test object exercising aliased request data.</summary>
public class SomeRequestData
{
    /// <summary>Gets or sets the aliased readable property.</summary>
    [AliasAs("rpn")]
    public int ReadablePropertyName { get; set; }
}
