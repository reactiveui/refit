// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Tests for <see cref="DefaultUrlParameterFormatter"/> date, collection and enum formatting behaviour.</summary>
public partial class DefaultUrlParameterFormatterTests
{
    /// <summary>A general date format registered for the DateTime formatter tests.</summary>
    private const string GeneralDateFormat = "yyyy-MM-dd";

    /// <summary>The base address used when building request URIs.</summary>
    private const string BaseUrl = "http://api";

    /// <summary>Flags enum used to exercise combined and undefined enum formatting.</summary>
    [Flags]
    private enum SampleFlags
    {
        /// <summary>No flags set.</summary>
        None = 0,

        /// <summary>The first flag.</summary>
        First = 1,

        /// <summary>The second flag.</summary>
        Second = 2
    }

    /// <summary>Verifies a null parameter value formats to null.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NullParameterValue_ReturnsNull()
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

        await Assert.That(output).IsNull();
    }

    /// <summary>Verifies the default date format is used when no formatters are registered.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NoFormatters_UseDefaultFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTime = new DateTime(2023, 8, 21, 0, 0, 0, DateTimeKind.Unspecified)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();

        var output = urlParameterFormatter.Format(
            parameters.DateTime,
            parameters.GetType().GetProperty(nameof(parameters.DateTime))!,
            parameters.GetType());

        await Assert.That(output).IsEqualTo("08/21/2023 00:00:00");
    }

    /// <summary>Verifies a Query attribute format takes precedence when it is the only format.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryAttributeFormatOnly_UseQueryAttributeFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTimeWithAttributeFormatYear = new DateTime(2023, 8, 21, 0, 0, 0, DateTimeKind.Unspecified)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();

        var output = urlParameterFormatter.Format(
            parameters.DateTimeWithAttributeFormatYear,
            parameters.GetType().GetProperty(nameof(parameters.DateTimeWithAttributeFormatYear))!,
            parameters.GetType());

        await Assert.That(output).IsEqualTo("2023");
    }

    /// <summary>Verifies the Query attribute format wins over a registered general format.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryAttributeAndGeneralFormat_UseQueryAttributeFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTimeWithAttributeFormatYear = new DateTime(2023, 8, 21, 0, 0, 0, DateTimeKind.Unspecified)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>(GeneralDateFormat);

        var output = urlParameterFormatter.Format(
            parameters.DateTimeWithAttributeFormatYear,
            parameters.GetType().GetProperty(nameof(parameters.DateTimeWithAttributeFormatYear))!,
            parameters.GetType());

        await Assert.That(output).IsEqualTo("2023");
    }

    /// <summary>Verifies the Query attribute format wins over a registered type-specific format.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryAttributeAndSpecificFormat_UseQueryAttributeFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTimeWithAttributeFormatYear = new DateTime(2023, 8, 21, 0, 0, 0, DateTimeKind.Unspecified)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DefaultUrlParameterFormatterTestRequest, DateTime>(GeneralDateFormat);

        var output = urlParameterFormatter.Format(
            parameters.DateTimeWithAttributeFormatYear,
            parameters.GetType().GetProperty(nameof(parameters.DateTimeWithAttributeFormatYear))!,
            parameters.GetType());

        await Assert.That(output).IsEqualTo("2023");
    }

    /// <summary>Verifies the Query attribute format wins over both general and specific formats.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AllFormats_UseQueryAttributeFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTimeWithAttributeFormatYear = new DateTime(2023, 8, 21, 0, 0, 0, DateTimeKind.Unspecified)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>(GeneralDateFormat);
        urlParameterFormatter.AddFormat<DefaultUrlParameterFormatterTestRequest, DateTime>(GeneralDateFormat);

        var output = urlParameterFormatter.Format(
            parameters.DateTimeWithAttributeFormatYear,
            parameters.GetType().GetProperty(nameof(parameters.DateTimeWithAttributeFormatYear))!,
            parameters.GetType());

        await Assert.That(output).IsEqualTo("2023");
    }

    /// <summary>Verifies a registered general format is applied when no attribute format exists.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GeneralFormatOnly_UseGeneralFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTime = new DateTime(2023, 8, 21, 0, 0, 0, DateTimeKind.Unspecified)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy");

        var output = urlParameterFormatter.Format(
            parameters.DateTime,
            parameters.GetType().GetProperty(nameof(parameters.DateTime))!,
            parameters.GetType());

        await Assert.That(output).IsEqualTo("2023");
    }

    /// <summary>Verifies a registered type-specific format is applied when no attribute format exists.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SpecificFormatOnly_UseSpecificFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTime = new DateTime(2023, 8, 21, 0, 0, 0, DateTimeKind.Unspecified)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DefaultUrlParameterFormatterTestRequest, DateTime>("yyyy");

        var output = urlParameterFormatter.Format(
            parameters.DateTime,
            parameters.GetType().GetProperty(nameof(parameters.DateTime))!,
            parameters.GetType());

        await Assert.That(output).IsEqualTo("2023");
    }

    /// <summary>Verifies a type-specific format wins over a general format.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GeneralAndSpecificFormats_UseSpecificFormat()
    {
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTime = new DateTime(2023, 8, 21, 0, 0, 0, DateTimeKind.Unspecified)
        };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>(GeneralDateFormat);
        urlParameterFormatter.AddFormat<DefaultUrlParameterFormatterTestRequest, DateTime>("yyyy");

        var output = urlParameterFormatter.Format(
            parameters.DateTime,
            parameters.GetType().GetProperty(nameof(parameters.DateTime))!,
            parameters.GetType());

        await Assert.That(output).IsEqualTo("2023");
    }

    /// <summary>Verifies a request with a plain DateTime query parameter produces the expected query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RequestWithPlainDateTimeQueryParameter_ProducesCorrectQueryString()
    {
        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy");

        var refitSettings = new RefitSettings { UrlParameterFormatter = urlParameterFormatter };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.PostWithComplexTypeQuery));

        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTime = new DateTime(2023, 8, 21, 0, 0, 0, DateTimeKind.Unspecified),
        };

        var output = await factory([parameters]);
        var uri = new Uri(new(BaseUrl), output.RequestUri!);

        await Assert.That(uri.Query).IsEqualTo(
            "?DateTime=2023");
    }

    /// <summary>Verifies a request with a DateTime collection query parameter produces the expected query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RequestWithDateTimeCollectionQueryParameter_ProducesCorrectQueryString()
    {
        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy");

        var refitSettings = new RefitSettings { UrlParameterFormatter = urlParameterFormatter };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.PostWithComplexTypeQuery));

        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTimeCollection =
            [
                new(2023, 8, 21, 0, 0, 0, DateTimeKind.Unspecified),
                new(2024, 8, 21, 0, 0, 0, DateTimeKind.Unspecified)
            ],
        };

        var output = await factory([parameters]);
        var uri = new Uri(new(BaseUrl), output.RequestUri!);

        await Assert.That(uri.Query).IsEqualTo(
            "?DateTimeCollection=2023%2C2024");
    }

    /// <summary>Verifies a request with a DateTime-valued dictionary query parameter produces the expected query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RequestWithDateTimeDictionaryQueryParameter_ProducesCorrectQueryString()
    {
        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy");

        var refitSettings = new RefitSettings { UrlParameterFormatter = urlParameterFormatter };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.PostWithComplexTypeQuery));

        const int secondEntryKey = 2;
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTimeDictionary = new Dictionary<int, DateTime>
            {
                { 1, new(2023, 8, 21, 0, 0, 0, DateTimeKind.Unspecified) },
                { secondEntryKey, new(2024, 8, 21, 0, 0, 0, DateTimeKind.Unspecified) },
            },
        };

        var output = await factory([parameters]);
        var uri = new Uri(new(BaseUrl), output.RequestUri!);

        await Assert.That(uri.Query).IsEqualTo(
            "?DateTimeDictionary.1=2023&DateTimeDictionary.2=2024");
    }

    /// <summary>Verifies a request with a DateTime-keyed dictionary query parameter produces the expected query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RequestWithDateTimeKeyedDictionaryQueryParameter_ProducesCorrectQueryString()
    {
        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        urlParameterFormatter.AddFormat<DateTime>("yyyy");

        var refitSettings = new RefitSettings { UrlParameterFormatter = urlParameterFormatter };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.PostWithComplexTypeQuery));

        const int secondEntryValue = 2;
        var parameters = new DefaultUrlParameterFormatterTestRequest
        {
            DateTimeKeyedDictionary = new Dictionary<DateTime, int>
            {
                { new(2023, 8, 21, 0, 0, 0, DateTimeKind.Unspecified), 1 },
                { new(2024, 8, 21, 0, 0, 0, DateTimeKind.Unspecified), secondEntryValue },
            },
        };

        var output = await factory([parameters]);
        var uri = new Uri(new(BaseUrl), output.RequestUri!);

        await Assert.That(uri.Query).IsEqualTo(
            "?DateTimeKeyedDictionary.2023=1&DateTimeKeyedDictionary.2024=2");
    }

    /// <summary>Verifies a combined flags enum value formats to its named flags without throwing.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CombinedFlagsEnumValue_DoesNotThrow()
    {
        var parameters = new FlagsRequest { Flags = SampleFlags.First | SampleFlags.Second };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();

        var output = urlParameterFormatter.Format(
            parameters.Flags,
            parameters.GetType().GetProperty(nameof(parameters.Flags))!,
            parameters.GetType());

        await Assert.That(output).IsEqualTo("First, Second");
    }

    /// <summary>Verifies an undefined flags enum value formats to its numeric value without throwing.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UndefinedEnumValue_DoesNotThrow()
    {
        var parameters = new FlagsRequest { Flags = (SampleFlags)(-1) };

        var urlParameterFormatter = new DefaultUrlParameterFormatter();

        var output = urlParameterFormatter.Format(
            parameters.Flags,
            parameters.GetType().GetProperty(nameof(parameters.Flags))!,
            parameters.GetType());

        await Assert.That(output).IsEqualTo("-1");
    }

    /// <summary>Request fixture exposing DateTime-shaped query parameters used by the formatter tests.</summary>
    private sealed class DefaultUrlParameterFormatterTestRequest
    {
        /// <summary>Gets or sets a DateTime carrying a Query attribute format of "yyyy".</summary>
        [Query(Format = "yyyy")]
        public DateTime? DateTimeWithAttributeFormatYear { get; set; }

        /// <summary>Gets or sets a plain nullable DateTime.</summary>
        public DateTime? DateTime { get; set; }

        /// <summary>Gets or sets a collection of DateTime values.</summary>
        public IEnumerable<DateTime>? DateTimeCollection { get; set; }

        /// <summary>Gets or sets a dictionary of DateTime values keyed by integer.</summary>
        public IDictionary<int, DateTime> DateTimeDictionary { get; init; } = new Dictionary<int, DateTime>();

        /// <summary>Gets or sets a dictionary of integer values keyed by DateTime.</summary>
        public IDictionary<DateTime, int> DateTimeKeyedDictionary { get; init; } = new Dictionary<DateTime, int>();
    }

    /// <summary>Request fixture exposing a flags enum query parameter.</summary>
    private sealed class FlagsRequest
    {
        /// <summary>Gets or sets the flags enum value.</summary>
        public SampleFlags Flags { get; set; }
    }
}
