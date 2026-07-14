// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A test object exercising query serialization of non-public getters.</summary>
public class Person
{
    /// <summary>Gets or sets the first name.</summary>
    public string? FirstName { private get; set; }

    /// <summary>Gets or sets the last name.</summary>
    public string? LastName { private get; set; }

    /// <summary>Gets the full name.</summary>
    public string FullName => $"{FirstName} {LastName}";
}
