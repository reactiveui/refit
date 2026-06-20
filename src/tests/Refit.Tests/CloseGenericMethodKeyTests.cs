// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Tests for the reflection method-info cache key used by generic request dispatch.</summary>
public sealed class CloseGenericMethodKeyTests
{
    /// <summary>Verifies closed generic method keys compare method definitions and type arguments.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ComparesMethodAndTypes()
    {
        var openMethod = typeof(IGenericMethodKeyFixture).GetMethod(nameof(IGenericMethodKeyFixture.GenericMethod))!;
        var otherOpenMethod = typeof(IGenericMethodKeyFixture).GetMethod(nameof(IGenericMethodKeyFixture.OtherGenericMethod))!;
        var value = new CloseGenericMethodKey(openMethod, [typeof(string), typeof(int)]);
        var same = new CloseGenericMethodKey(openMethod, [typeof(string), typeof(int)]);
        var differentMethod = new CloseGenericMethodKey(otherOpenMethod, [typeof(string), typeof(int)]);
        var differentLength = new CloseGenericMethodKey(openMethod, [typeof(string)]);
        var differentType = new CloseGenericMethodKey(openMethod, [typeof(string), typeof(long)]);

        await Assert.That(value.Equals(same)).IsTrue();
        await Assert.That(value.Equals((object)same)).IsTrue();
        await Assert.That(value.Equals("not-key")).IsFalse();
        await Assert.That(value.GetHashCode()).IsEqualTo(same.GetHashCode());
        await Assert.That(value.Equals(differentMethod)).IsFalse();
        await Assert.That(value.Equals(differentLength)).IsFalse();
        await Assert.That(value.Equals(differentType)).IsFalse();
    }
}
