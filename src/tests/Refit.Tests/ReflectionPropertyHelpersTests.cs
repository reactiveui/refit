// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Refit.Tests;

/// <summary>Verifies <see cref="ReflectionPropertyHelpers"/> returns only the properties that can be read through a
/// public getter, which the reflection request builder relies on when flattening query objects.</summary>
public sealed class ReflectionPropertyHelpersTests
{
    /// <summary>Verifies a readable public property is kept while a non-public-getter property is skipped.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetReadablePublicInstancePropertiesKeepsPubliclyReadableProperties()
    {
        var names = ReflectionPropertyHelpers
            .GetReadablePublicInstanceProperties(typeof(PropertyShapes))
            .Select(static property => property.Name)
            .ToArray();

        await Assert.That(names).Contains("Readable");
        await Assert.That(names).DoesNotContain("NonPublicGetter");
    }

    /// <summary>Verifies a public property that cannot be read is excluded. <see cref="ProcessThread"/> exposes the
    /// public write-only <c>IdealProcessor</c> and <c>ProcessorAffinity</c> properties (their metadata is present on
    /// every platform), so reflecting over the type exercises the not-readable branch.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetReadablePublicInstancePropertiesExcludesWriteOnlyProperties()
    {
        var names = ReflectionPropertyHelpers
            .GetReadablePublicInstanceProperties(typeof(ProcessThread))
            .Select(static property => property.Name)
            .ToArray();

        await Assert.That(names).DoesNotContain("IdealProcessor");
        await Assert.That(names).DoesNotContain("ProcessorAffinity");
    }

    /// <summary>A type whose properties span the readable and non-public-getter shapes.</summary>
    public sealed class PropertyShapes
    {
        /// <summary>Gets or sets a readable public property that is returned.</summary>
        public string? Readable { get; set; }

        /// <summary>Gets or sets a property whose getter is non-public, so it is skipped.</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1044",
            Justification = "The property-filter test requires a public property with an internal getter to verify that visibility alone does not make a property readable.")]
        public string? NonPublicGetter { internal get; set; }
    }
}
