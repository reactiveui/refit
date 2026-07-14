// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Tests for composing flattened query-object keys via <see cref="GeneratedRequestRunner"/>.</summary>
public partial class GeneratedRequestRunnerTests
{
    /// <summary>The CLR property name used by the query-key composition tests.</summary>
    private const string QueryKeyClrName = "SortOrder";

    /// <summary>Verifies the pristine default key formatter is detected so generated code can use constant keys.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UsesDefaultUrlParameterKeyFormattingIsTrueForPristineDefault() =>
        await Assert.That(GeneratedRequestRunner.UsesDefaultUrlParameterKeyFormatting(new RefitSettings())).IsTrue();

    /// <summary>Verifies a custom key formatter disables the constant-key fast path.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UsesDefaultUrlParameterKeyFormattingIsFalseForCustomFormatter()
    {
        var settings = new RefitSettings { UrlParameterKeyFormatter = new UpperCaseKeyFormatter() };

        await Assert.That(GeneratedRequestRunner.UsesDefaultUrlParameterKeyFormatting(settings)).IsFalse();
    }

    /// <summary>Verifies a subclass of the default key formatter is not treated as pristine, since it may override Format.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UsesDefaultUrlParameterKeyFormattingIsFalseForDerivedDefaultFormatter()
    {
        var settings = new RefitSettings { UrlParameterKeyFormatter = new DerivedKeyFormatter() };

        await Assert.That(GeneratedRequestRunner.UsesDefaultUrlParameterKeyFormatting(settings)).IsFalse();
    }

    /// <summary>Verifies the CLR property name is the key when no alias, prefix, or custom formatter applies.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildQueryKeyUsesClrNameByDefault() =>
        await Assert.That(GeneratedRequestRunner.BuildQueryKey(new RefitSettings(), QueryKeyClrName, null, null))
            .IsEqualTo(QueryKeyClrName);

    /// <summary>Verifies the CLR property name passes through a custom key formatter.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildQueryKeyAppliesCustomKeyFormatter()
    {
        var settings = new RefitSettings { UrlParameterKeyFormatter = new UpperCaseKeyFormatter() };

        await Assert.That(GeneratedRequestRunner.BuildQueryKey(settings, QueryKeyClrName, null, null))
            .IsEqualTo("SORTORDER");
    }

    /// <summary>Verifies an alias bypasses the key formatter entirely, matching the reflection request builder.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildQueryKeyAliasBypassesKeyFormatter()
    {
        var settings = new RefitSettings { UrlParameterKeyFormatter = new UpperCaseKeyFormatter() };

        await Assert.That(GeneratedRequestRunner.BuildQueryKey(settings, QueryKeyClrName, "sort", null))
            .IsEqualTo("sort");
    }

    /// <summary>Verifies the compile-time prefix segment is prepended to the resolved name.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildQueryKeyPrependsPrefixSegment() =>
        await Assert.That(GeneratedRequestRunner.BuildQueryKey(new RefitSettings(), "Zip", null, "Addr."))
            .IsEqualTo("Addr.Zip");

    /// <summary>Verifies the prefix segment is prepended to an alias as well as to a formatted CLR name.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildQueryKeyPrependsPrefixSegmentToAlias() =>
        await Assert.That(GeneratedRequestRunner.BuildQueryKey(new RefitSettings(), "Zip", "postcode", "Addr-"))
            .IsEqualTo("Addr-postcode");

    /// <summary>A key formatter that upper-cases keys, used to prove the formatter is consulted.</summary>
    private sealed class UpperCaseKeyFormatter : IUrlParameterKeyFormatter
    {
        /// <summary>Formats the specified key.</summary>
        /// <param name="key">The key.</param>
        /// <returns>The upper-cased key.</returns>
        public string Format(string key) => key.ToUpperInvariant();
    }

    /// <summary>A subclass of the default key formatter, which must not be treated as the pristine default.</summary>
    private sealed class DerivedKeyFormatter : DefaultUrlParameterKeyFormatter
    {
        /// <summary>Formats the specified key.</summary>
        /// <param name="key">The key.</param>
        /// <returns>The reversed key.</returns>
        public override string Format(string key) => new([.. key.Reverse()]);
    }
}
