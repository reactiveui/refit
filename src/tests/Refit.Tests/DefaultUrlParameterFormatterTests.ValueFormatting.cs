// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Tests pinning the scalar value rendering of <see cref="DefaultUrlParameterFormatter"/>: a string passes
/// through verbatim and a formattable value renders with the invariant culture.</summary>
public partial class DefaultUrlParameterFormatterTests
{
    /// <summary>Verifies a string value is returned verbatim and a formattable value renders invariantly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task FormatReturnsStringVerbatimAndRendersFormattableInvariantly()
    {
        const int sample = 1234;
        var formatter = new DefaultUrlParameterFormatter();

        await Assert.That(formatter.Format("widgets and gadgets", typeof(string), typeof(string)))
            .IsEqualTo("widgets and gadgets");
        await Assert.That(formatter.Format(sample, typeof(int), typeof(int)))
            .IsEqualTo("1234");
    }

    /// <summary>Verifies a non-formattable value falls back to its <see cref="object.ToString"/>, and a value whose
    /// <see cref="object.ToString"/> returns null renders as the empty string, matching composite formatting.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task FormatFallsBackToToStringForNonFormattableValues()
    {
        var formatter = new DefaultUrlParameterFormatter();

        await Assert.That(formatter.Format(new NonFormattableValue(), typeof(NonFormattableValue), typeof(NonFormattableValue)))
            .IsEqualTo("custom-text");
        await Assert.That(formatter.Format(new NullToStringValue(), typeof(NullToStringValue), typeof(NullToStringValue)))
            .IsEqualTo(string.Empty);
    }

    /// <summary>A non-formattable value whose <see cref="ToString"/> supplies the rendered text.</summary>
    private sealed class NonFormattableValue
    {
        /// <inheritdoc/>
        public override string ToString() => "custom-text";
    }

    /// <summary>A non-formattable value whose <see cref="ToString"/> returns null, exercising the empty fallback.</summary>
    private sealed class NullToStringValue
    {
        /// <summary>Gets the backing text, left null so <see cref="ToString"/> yields the empty-string fallback.</summary>
        public string? Text { get; init; }

        /// <inheritdoc/>
        public override string? ToString() => Text;
    }
}
