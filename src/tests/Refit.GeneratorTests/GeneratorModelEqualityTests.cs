// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Verifies raw model arrays retain structural incremental-cache equality.</summary>
public class GeneratorModelEqualityTests
{
    /// <summary>The request header compared in nested model tests.</summary>
    private const string HeaderName = "Accept";

    /// <summary>Verifies equal sequences in different arrays have matching comparisons and hashes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Equals_SameSequence_MatchesHash()
    {
        string[] left = ["a", "b"];
        string[] right = ["a", "b"];
        var comparer = ArraySequenceComparer<string>.Instance;

        await Assert.That(comparer.Equals(left, right)).IsTrue();
        await Assert.That(comparer.GetHashCode(left)).IsEqualTo(comparer.GetHashCode(right));
        await Assert.That(comparer.Equals(left, left)).IsTrue();
    }

    /// <summary>Verifies changed elements, lengths and optional-array presence invalidate equality.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Equals_DifferentSequence_IsFalse()
    {
        string[] values = ["a", "b"];
        var comparer = ArraySequenceComparer<string>.Instance;

        await Assert.That(comparer.Equals(values, ["a", "c"])).IsFalse();
        await Assert.That(comparer.Equals(values, ["a"])).IsFalse();
        await Assert.That(comparer.Equals(values, null)).IsFalse();
        await Assert.That(comparer.Equals(null, values)).IsFalse();
        await Assert.That(comparer.Equals(null, [])).IsFalse();
        await Assert.That(comparer.Equals(null, null)).IsTrue();
    }

    /// <summary>Verifies default required arrays compare and hash like empty arrays.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequiredArrays_DefaultAndEmpty_AreEqual()
    {
        await Assert.That(GeneratorModelEquality.SequenceEquals<string>(null, [])).IsTrue();
        await Assert.That(GeneratorModelEquality.SequenceEquals<string>([], null)).IsTrue();
        await Assert.That(GeneratorModelEquality.SequenceEquals(null, ["value"])).IsFalse();
        await Assert.That(GeneratorModelEquality.GetSequenceHashCode<string>(null)).IsEqualTo(GeneratorModelEquality.GetSequenceHashCode<string>([]));
        await Assert.That(GeneratorModelEquality.GetHashCode<string?>(null)).IsEqualTo(0);
    }

    /// <summary>Verifies nested request bindings compare header contents instead of array identities.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestBindings_CompareNestedContents()
    {
        var left = new RequestBindingsModel([new(HeaderName, "application/json")], []);
        var right = new RequestBindingsModel([new(HeaderName, "application/json")], []);
        var changed = new RequestBindingsModel([new(HeaderName, "text/plain")], []);

        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
        await Assert.That(left.Equals(changed)).IsFalse();
        await Assert.That(default(RequestBindingsModel).Equals(new([], []))).IsTrue();
    }
}
