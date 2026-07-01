// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Tests for <see cref="KebabCaseUrlParameterKeyFormatter"/>.</summary>
public class KebabCaseUrlParameterKeyFormatterTests
{
    /// <summary>Verifies that an empty key is returned unchanged.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Format_EmptyKey_ReturnsEmptyKey()
    {
        var formatter = new KebabCaseUrlParameterKeyFormatter();

        var output = formatter.Format(string.Empty);

        await Assert.That(output).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies that keys are converted to kebab-case.</summary>
    /// <param name="key">The key to format.</param>
    /// <param name="expected">The expected formatted key.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [Arguments("MyProperty", "my-property")]
    [Arguments("myProperty", "my-property")]
    [Arguments("URLValue", "url-value")]
    [Arguments("UrlValue", "url-value")]
    [Arguments("URL", "url")]
    [Arguments("Address1Zip", "address1-zip")]
    public async Task Format_Keys_ReturnsKebabCase(string key, string expected)
    {
        var formatter = new KebabCaseUrlParameterKeyFormatter();

        var output = formatter.Format(key);

        await Assert.That(output).IsEqualTo(expected);
    }

    /// <summary>Verifies that complex query object keys are kebab-cased when building a request.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task FormatKey_Returns_ExpectedValue()
    {
        var refitSettings = new RefitSettings
        {
            UrlParameterKeyFormatter = new KebabCaseUrlParameterKeyFormatter()
        };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.ComplexQueryObjectWithDictionary));

        var complexQuery = new CamelCaselTestsRequest
        {
            AlreadyCamelCased = "value1",
            NotcamelCased = "value2"
        };

        var output = await factory([complexQuery]);
        await Assert.That(output.RequestUri).IsNotNull();
        var uri = new Uri(new("http://api"), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?already-camel-cased=value1&notcamel-cased=value2");
    }
}
