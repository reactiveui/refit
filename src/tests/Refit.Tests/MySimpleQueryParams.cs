// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Simple query parameter object used to exercise dynamic query parameter expansion.</summary>
public sealed record MySimpleQueryParams
{
    /// <summary>Gets or sets the first name query value.</summary>
    public required string FirstName { get; init; }

    /// <summary>Gets or sets the last name query value, aliased to <c>lName</c>.</summary>
    [AliasAs("lName")]
    public required string LastName { get; init; }
}
