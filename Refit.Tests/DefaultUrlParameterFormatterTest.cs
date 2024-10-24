using System.Globalization;
using System.Reflection;
using Xunit;

namespace Refit.Tests;

public class DefaultUrlParameterFormatterTests
{
    class DefaultUrlParameterFormatterTestRequest
    {
        [Query(Format = "yyyy")] public DateTime? DateTimeWithAttributeFormatYear { get; set; }

        public DateTime? DateTime { get; set; }

        public IEnumerable<DateTime> DateTimeCollection { get; set; }

        public IDictionary<int, DateTime> DateTimeDictionary { get; set; }

        public IDictionary<DateTime, int> DateTimeKeyedDictionary { get; set; }
    }

    [Fact]
    public void NullParameterValue_ReturnsNull()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTime = null
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();

        var output = urlParameterFormatter.Format(
            parameters.DateTime,
            parameters.GetType().GetProperty(nameof(parameters.DateTime))!,
            parameters.GetType());

        Assert.Null(output);
    }

    [Fact]
    public void NoFormatters_UseDefaultFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTime = new DateTime(2023, 8, 21)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();

        var output = urlParameterFormatter.Format(
            parameters.DateTime,
            parameters.GetType().GetProperty(nameof(parameters.DateTime))!,
            parameters.GetType());

        Assert.Equal("08/21/2023 00:00:00", output);
    }

    [Fact]
    public void QueryAttributeFormatOnly_UseQueryAttributeFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTimeWithAttributeFormatYear = new DateTime(2023, 8, 21)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();

        var output = urlParameterFormatter.Format(
            parameters.DateTimeWithAttributeFormatYear,
            parameters.GetType().GetProperty(nameof(parameters.DateTimeWithAttributeFormatYear))!,
            parameters.GetType());

        Assert.Equal("2023", output);
    }

    [Fact]
    public void QueryAttributeAndGeneralFormat_UseQueryAttributeFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTimeWithAttributeFormatYear = new DateTime(2023, 8, 21)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy-MM-dd");

        var output = urlParameterFormatter.Format(
            parameters.DateTimeWithAttributeFormatYear,
            parameters.GetType().GetProperty(nameof(parameters.DateTimeWithAttributeFormatYear))!,
            parameters.GetType());

        Assert.Equal("2023", output);
    }

    [Fact]
    public void QueryAttributeAndSpecificFormat_UseQueryAttributeFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTimeWithAttributeFormatYear = new DateTime(2023, 8, 21)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DefaultUrlParameterFormatterTestRequest, DateTime>("yyyy-MM-dd");

        var output = urlParameterFormatter.Format(
            parameters.DateTimeWithAttributeFormatYear,
            parameters.GetType().GetProperty(nameof(parameters.DateTimeWithAttributeFormatYear))!,
            parameters.GetType());

        Assert.Equal("2023", output);
    }

    [Fact]
    public void AllFormats_UseQueryAttributeFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTimeWithAttributeFormatYear = new DateTime(2023, 8, 21)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy-MM-dd");
        urlParameterFormatter.AddFormat<DefaultUrlParameterFormatterTestRequest, DateTime>("yyyy-MM-dd");

        var output = urlParameterFormatter.Format(
            parameters.DateTimeWithAttributeFormatYear,
            parameters.GetType().GetProperty(nameof(parameters.DateTimeWithAttributeFormatYear))!,
            parameters.GetType());

        Assert.Equal("2023", output);
    }

    [Fact]
    public void GeneralFormatOnly_UseGeneralFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTime = new DateTime(2023, 8, 21)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy");

        var output = urlParameterFormatter.Format(
            parameters.DateTime,
            parameters.GetType().GetProperty(nameof(parameters.DateTime))!,
            parameters.GetType());

        Assert.Equal("2023", output);
    }

    [Fact]
    public void SpecificFormatOnly_UseSpecificFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTime = new DateTime(2023, 8, 21)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DefaultUrlParameterFormatterTestRequest, DateTime>("yyyy");

        var output = urlParameterFormatter.Format(
            parameters.DateTime,
            parameters.GetType().GetProperty(nameof(parameters.DateTime))!,
            parameters.GetType());

        Assert.Equal("2023", output);
    }

    [Fact]
    public void GeneralAndSpecificFormats_UseSpecificFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTime = new DateTime(2023, 8, 21)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy-MM-dd");
        urlParameterFormatter.AddFormat<DefaultUrlParameterFormatterTestRequest, DateTime>("yyyy");

        var output = urlParameterFormatter.Format(
            parameters.DateTime,
            parameters.GetType().GetProperty(nameof(parameters.DateTime))!,
            parameters.GetType());

        Assert.Equal("2023", output);
    }

    [Fact]
    public void RequestWithPlainDateTimeQueryParameter_ProducesCorrectQueryString()
    {
        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy");

        var refitSettings = new RefitSettings { UrlParameterFormatter = urlParameterFormatter };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.PostWithComplexTypeQuery)
        );

        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTime = new DateTime(2023, 8, 21),
        };

        var output = factory([parameters]);
        var uri = new Uri(new Uri("http://api"), output.RequestUri);

        Assert.Equal(
            "?DateTime=2023",
            uri.Query
        );
    }

    [Fact]
    public void RequestWithDateTimeCollectionQueryParameter_ProducesCorrectQueryString()
    {
        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy");

        var refitSettings = new RefitSettings { UrlParameterFormatter = urlParameterFormatter };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.PostWithComplexTypeQuery)
        );

        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTimeCollection = [new DateTime(2023, 8, 21), new DateTime(2024, 8, 21)],
        };

        var output = factory([parameters]);
        var uri = new Uri(new Uri("http://api"), output.RequestUri);

        Assert.Equal(
            "?DateTimeCollection=2023%2C2024",
            uri.Query
        );
    }

    [Fact]
    public void RequestWithDateTimeDictionaryQueryParameter_ProducesCorrectQueryString()
    {
        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy");

        var refitSettings = new RefitSettings { UrlParameterFormatter = urlParameterFormatter };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.PostWithComplexTypeQuery)
        );

        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTimeDictionary = new Dictionary<int, DateTime>
            {
                { 1, new DateTime(2023, 8, 21) },
                { 2, new DateTime(2024, 8, 21) },
            },
        };

        var output = factory([parameters]);
        var uri = new Uri(new Uri("http://api"), output.RequestUri);

        Assert.Equal(
            "?DateTimeDictionary.1=2023&DateTimeDictionary.2=2024",
            uri.Query
        );
    }

    [Fact]
    public void RequestWithDateTimeKeyedDictionaryQueryParameter_ProducesCorrectQueryString()
    {
        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy");

        var refitSettings = new RefitSettings { UrlParameterFormatter = urlParameterFormatter };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.PostWithComplexTypeQuery)
        );

        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTimeKeyedDictionary = new Dictionary<DateTime, int>
            {
                { new DateTime(2023, 8, 21), 1 },
                { new DateTime(2024, 8, 21), 2 },
            },
        };

        var output = factory([parameters]);
        var uri = new Uri(new Uri("http://api"), output.RequestUri);

        Assert.Equal(
            "?DateTimeKeyedDictionary.2023=1&DateTimeKeyedDictionary.2024=2",
            uri.Query
        );
    }
}
