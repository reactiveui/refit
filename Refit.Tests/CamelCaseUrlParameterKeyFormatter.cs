using Xunit;

namespace Refit.Tests;

public class CamelCaselTestsRequest
{
    public string alreadyCamelCased { get; set; }
    public string NOTCAMELCased { get; set; }
}

public class CamelCaseUrlParameterKeyFormatterTests
{
    [Fact]
    public void Format_EmptyKey_ReturnsEmptyKey()
    {
        var urlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter();

        var output = urlParameterKeyFormatter.Format(string.Empty);
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public void FormatKey_Returns_ExpectedValue()
    {
        var urlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter();

        var refitSettings = new RefitSettings { UrlParameterKeyFormatter = urlParameterKeyFormatter };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.ComplexQueryObjectWithDictionary));

        var complexQuery = new CamelCaselTestsRequest
        {
            alreadyCamelCased = "value1",
            NOTCAMELCased = "value2"
        };

        var output = factory([complexQuery]);
        var uri = new Uri(new Uri("http://api"), output.RequestUri);

        Assert.Equal("/foo?alreadyCamelCased=value1&notcamelCased=value2", uri.PathAndQuery);
    }
}
