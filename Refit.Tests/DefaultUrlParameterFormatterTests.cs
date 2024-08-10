using System.Globalization;
using System.Reflection;
using Xunit;

namespace Refit.Tests;

public class DefaultUrlParameterFormatterTests
{
    class DateTimeRequestWithQueryAttribute
    {
        [Query(Format = "yyyy")] public DateTime? DateTimeYear { get; set; }
    }

    class DateTimeRequestWithoutQueryAttribute
    {
        public DateTime DateTime { get; set; }
    }

    [Fact]
    public void NullParameterValue_ReturnsNull()
    {
        var parameters = new DateTimeRequestWithQueryAttribute
        {
            DateTimeYear = null
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();

        var output = urlParameterFormatter.Format(
            parameters.DateTimeYear,
            parameters.GetType().GetProperty(nameof(parameters.DateTimeYear))!,
            parameters.GetType());

        Assert.Null(output);
    }

    [Fact]
    public void NoFormatters_UseDefaultFormat()
    {
        var parameters = new DateTimeRequestWithoutQueryAttribute
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
        var parameters = new DateTimeRequestWithQueryAttribute
        {
            DateTimeYear = new DateTime(2023, 8, 21)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();

        var output = urlParameterFormatter.Format(
            parameters.DateTimeYear,
            parameters.GetType().GetProperty(nameof(parameters.DateTimeYear))!,
            parameters.GetType());

        Assert.Equal("2023", output);
    }

    [Fact]
    public void QueryAttributeAndGeneralFormat_UseQueryAttributeFormat()
    {
        var parameters = new DateTimeRequestWithQueryAttribute
        {
            DateTimeYear = new DateTime(2023, 8, 21)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy-MM-dd");

        var output = urlParameterFormatter.Format(
            parameters.DateTimeYear,
            parameters.GetType().GetProperty(nameof(parameters.DateTimeYear))!,
            parameters.GetType());

        Assert.Equal("2023", output);
    }

    [Fact]
    public void QueryAttributeAndSpecificFormat_UseQueryAttributeFormat()
    {
        var parameters = new DateTimeRequestWithQueryAttribute
        {
            DateTimeYear = new DateTime(2023, 8, 21)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTimeRequestWithQueryAttribute, DateTime>("yyyy-MM-dd");

        var output = urlParameterFormatter.Format(
            parameters.DateTimeYear,
            parameters.GetType().GetProperty(nameof(parameters.DateTimeYear))!,
            parameters.GetType());

        Assert.Equal("2023", output);
    }

    [Fact]
    public void AllFormats_UseQueryAttributeFormat()
    {
        var parameters = new DateTimeRequestWithQueryAttribute
        {
            DateTimeYear = new DateTime(2023, 8, 21)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy-MM-dd");
        urlParameterFormatter.AddFormat<DateTimeRequestWithoutQueryAttribute, DateTime>("yyyy-MM-dd");

        var output = urlParameterFormatter.Format(
            parameters.DateTimeYear,
            parameters.GetType().GetProperty(nameof(parameters.DateTimeYear))!,
            parameters.GetType());

        Assert.Equal("2023", output);
    }

    [Fact]
    public void GeneralFormatOnly_UseGeneralFormat()
    {
        var parameters = new DateTimeRequestWithoutQueryAttribute
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
        var parameters = new DateTimeRequestWithoutQueryAttribute
        {
            DateTime = new DateTime(2023, 8, 21)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTimeRequestWithoutQueryAttribute, DateTime>("yyyy");

        var output = urlParameterFormatter.Format(
            parameters.DateTime,
            parameters.GetType().GetProperty(nameof(parameters.DateTime))!,
            parameters.GetType());

        Assert.Equal("2023", output);
    }

    [Fact]
    public void GeneralAndSpecificFormats_UseSpecificFormat()
    {
        var parameters = new DateTimeRequestWithoutQueryAttribute
        {
            DateTime = new DateTime(2023, 8, 21)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy-MM-dd");
        urlParameterFormatter.AddFormat<DateTimeRequestWithoutQueryAttribute, DateTime>("yyyy");

        var output = urlParameterFormatter.Format(
            parameters.DateTime,
            parameters.GetType().GetProperty(nameof(parameters.DateTime))!,
            parameters.GetType());

        Assert.Equal("2023", output);
    }
}
