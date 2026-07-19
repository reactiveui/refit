// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Direct unit tests for path-template placeholder scanning, matching, and the value-equatable placeholder set.</summary>
public static partial class GeneratorComponentTests
{
    /// <summary>Tests for <c>PathParameterLocations</c>, the <c>{placeholder}</c> scanner, and dotted-placeholder matching.</summary>
    public class PathParameterLocationTests
    {
        /// <summary>Verifies the placeholder scanner skips a brace closed by a segment separator and resumes at the next brace.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task Scanner_SkipsBraceTerminatedBySlash_AndResumesAtNextBrace()
        {
            // The first '{' is terminated by '/' before any '}', so it is not a placeholder; the scanner must loop on
            // and pick up the well-formed '{b}' that follows.
            var locations = Parser.ExtractPathParameterPlaceholderNames("{a/{b}");
            var count = locations.Occurrences.Length;
            var firstName = count > 0 ? locations.Occurrences[0].Name : null;

            await Assert.That(count).IsEqualTo(1);
            await Assert.That(firstName).IsEqualTo("b");
        }

        /// <summary>Verifies placeholder-set equality is by backing occurrence identity, and the empty set hashes to zero.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task Equality_ComparesBackingOccurrenceIdentity()
        {
            var first = Parser.ExtractPathParameterPlaceholderNames("/a/{id}");
            var alias = first;
            var second = Parser.ExtractPathParameterPlaceholderNames("/a/{id}");
            var empty = Parser.ExtractPathParameterPlaceholderNames("/a/b");

            // A copy shares the backing array and compares equal; two independent parses of the same template do not.
            await Assert.That(first == alias).IsTrue();
            await Assert.That(first != alias).IsFalse();
            await Assert.That(first == second).IsFalse();
            await Assert.That(first != second).IsTrue();

            // A template with no placeholder yields the shared empty (defaulted) value.
            await Assert.That(empty == Parser.PathParameterLocations.Empty).IsTrue();

            // The object-typed override matches an equal value and rejects unrelated objects and a distinct set.
            await Assert.That(first.Equals((object)first)).IsTrue();
            await Assert.That(first.Equals((object)second)).IsFalse();
            await Assert.That(first.Equals("not a placeholder set")).IsFalse();

            // A populated set hashes from its backing array; the empty set hashes to zero.
            await Assert.That(first.GetHashCode()).IsEqualTo(first.GetHashCode());
            await Assert.That(empty.GetHashCode()).IsEqualTo(0);
        }

        /// <summary>Verifies round-trip (<c>{**name}</c>) matching resolves the matching name and rejects every near-miss shape.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task RoundTripMatching_MatchesDoubleStarPrefixAndRejectsNearMisses()
        {
            // "**value" is the round-trip form of "value"; "xxvalue" (wrong prefix char), "*yvalue" (only one star), and
            // "**walue" (same length and prefix, different name) are each near-misses that must not match.
            var locations = Parser.ExtractPathParameterPlaceholderNames("/x/{**value}/{xxvalue}/{*yvalue}/{**walue}");

            await Assert.That(locations.HasRoundTrip).IsTrue();

            var matched = locations.TryGetRoundTripLocations("value", out var matchedRanges);
            var matchedCount = matchedRanges.Count;

            // A name whose round-trip form is a different length matches nothing, so the walk falls through to no result.
            var unmatched = locations.TryGetRoundTripLocations("missing", out var missingRanges);
            var missingCount = missingRanges.Count;

            await Assert.That(matched).IsTrue();
            await Assert.That(matchedCount).IsEqualTo(1);
            await Assert.That(unmatched).IsFalse();
            await Assert.That(missingCount).IsEqualTo(0);
        }

        /// <summary>Verifies dotted-placeholder detection matches only a <c>{param.Prop}</c> whose head is the given name.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task DottedPlaceholderDetection_MatchesOnlyTheOwningParameterName()
        {
            var dotted = Parser.ExtractPathParameterPlaceholderNames("/x/{obj.Id}");

            await Assert.That(dotted.HasDotted).IsTrue();

            // The owning name matches; every other shape falls through the loop and returns false.
            await Assert.That(Parser.HasDottedPlaceholderFor(dotted, "obj")).IsTrue();

            // Head is a prefix but the boundary character is not '.'.
            await Assert.That(Parser.HasDottedPlaceholderFor(dotted, "ob")).IsFalse();

            // The '.' boundary aligns but the head text differs.
            await Assert.That(Parser.HasDottedPlaceholderFor(dotted, "abc")).IsFalse();

            // The candidate name is longer than the placeholder, so it cannot be its head.
            await Assert.That(Parser.HasDottedPlaceholderFor(dotted, "obj.Id.Extra")).IsFalse();

            // A template without a dotted placeholder short-circuits before scanning.
            var plain = Parser.ExtractPathParameterPlaceholderNames("/x/{id}");
            await Assert.That(Parser.HasDottedPlaceholderFor(plain, "id")).IsFalse();
        }
    }
}
