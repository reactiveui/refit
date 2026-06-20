// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Tests for <see cref="CamelCaseUrlParameterKeyFormatter"/>.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
public class CamelCaseUrlParameterKeyFormatterTests
{
    /// <summary>Verifies that an empty key is returned unchanged.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Format_EmptyKey_ReturnsEmptyKey()
    {
        var urlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter();

        var output = urlParameterKeyFormatter.Format(string.Empty);
        await Assert.That(output).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies the acronym casing rules stop before the first non-leading lowercase character.</summary>
    /// <param name="key">The key to format.</param>
    /// <param name="expected">The expected formatted key.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [Arguments("URLValue", "urlValue")]
    [Arguments("UrlValue", "urlValue")]
    [Arguments("URL", "url")]
    public async Task Format_AcronymKeys_ReturnsExpectedValue(string key, string expected)
    {
        var urlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter();

        var output = urlParameterKeyFormatter.Format(key);

        await Assert.That(output).IsEqualTo(expected);
    }

    /// <summary>Verifies that query keys are camelCased when building a request.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task FormatKey_Returns_ExpectedValue()
    {
        var urlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter();

        var refitSettings = new RefitSettings
        {
            UrlParameterKeyFormatter = urlParameterKeyFormatter
        };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.ComplexQueryObjectWithDictionary));

        var complexQuery = new CamelCaselTestsRequest
        {
            AlreadyCamelCased = "value1",
            NotcamelCased = "value2"
        };

        var output = factory([complexQuery]);
        await Assert.That(output.RequestUri).IsNotNull();
        var uri = new Uri(new("http://api"), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?alreadyCamelCased=value1&notcamelCased=value2");
    }
}
