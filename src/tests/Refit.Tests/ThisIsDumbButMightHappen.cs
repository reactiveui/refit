// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Holds a compile-time constant consumed by a Refit attribute to exercise constant resolution.</summary>
public static class ThisIsDumbButMightHappen
{
    /// <summary>A constant string referenced from a Refit attribute argument.</summary>
    [SuppressMessage(
        "Design",
        "SST2311:Visible constants should be static readonly",
        Justification = "Must remain a const because it is consumed as a Refit attribute argument, which requires a compile-time constant.")]
    public const string PeopleDoWeirdStuff = "But we don't let them";
}
