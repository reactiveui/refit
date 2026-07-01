// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Tests for <see cref="SnakeCaseUrlParameterKeyFormatter"/>.</summary>
public class SnakeCaseUrlParameterKeyFormatterTests
{
    /// <summary>Verifies that an empty key is returned unchanged.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Format_EmptyKey_ReturnsEmptyKey()
    {
        var formatter = new SnakeCaseUrlParameterKeyFormatter();

        var output = formatter.Format(string.Empty);

        await Assert.That(output).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies that keys are converted to snake_case.</summary>
    /// <param name="key">The key to format.</param>
    /// <param name="expected">The expected formatted key.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [Arguments("MyProperty", "my_property")]
    [Arguments("myProperty", "my_property")]
    [Arguments("URLValue", "url_value")]
    [Arguments("UrlValue", "url_value")]
    [Arguments("URL", "url")]
    [Arguments("Address1Zip", "address1_zip")]
    public async Task Format_Keys_ReturnsSnakeCase(string key, string expected)
    {
        var formatter = new SnakeCaseUrlParameterKeyFormatter();

        var output = formatter.Format(key);

        await Assert.That(output).IsEqualTo(expected);
    }

    /// <summary>Verifies that complex query object keys are snake_cased when building a request.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task FormatKey_Returns_ExpectedValue()
    {
        var refitSettings = new RefitSettings
        {
            UrlParameterKeyFormatter = new SnakeCaseUrlParameterKeyFormatter()
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

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?already_camel_cased=value1&notcamel_cased=value2");
    }
}
