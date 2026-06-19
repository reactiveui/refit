// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>A test object exercising query serialization of non-public getters.</summary>
public class Person
{
    /// <summary>Gets or sets the first name.</summary>
    [SuppressMessage("Design", "CA1044:Properties should not be write only", Justification = "Intentional private getters to verify Refit query serialization of objects with non-public getters.")]
    public string? FirstName { private get; set; }

    /// <summary>Gets or sets the last name.</summary>
    [SuppressMessage("Design", "CA1044:Properties should not be write only", Justification = "Intentional private getters to verify Refit query serialization of objects with non-public getters.")]
    public string? LastName { private get; set; }

    /// <summary>Gets the full name.</summary>
    public string FullName => $"{FirstName} {LastName}";
}
