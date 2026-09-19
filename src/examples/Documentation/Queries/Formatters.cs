// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;

namespace Refit.Documentation;

/// <summary>Checks key casing, format precedence and a boolean-specific formatter.</summary>
internal static class Formatters
{
    /// <summary>Provides the mixed-case property name used to compare key formatters.</summary>
    private const string Key = nameof(SearchFilter.PageSize);

    /// <summary>The container-specific and form date format.</summary>
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>The date used to compare general and specific rules.</summary>
    private const string DayText = "2026-09-17";

    /// <summary>Builds requests and verifies key transformations and formatted query values.</summary>
    /// <param name="host">The local handler and generated-metadata client configuration for these samples.</param>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        IUrlParameterKeyFormatter[] names =
        [
            new DefaultUrlParameterKeyFormatter(),
            new CamelCaseUrlParameterKeyFormatter(),
            new SnakeCaseUrlParameterKeyFormatter(),
            new KebabCaseUrlParameterKeyFormatter(),
        ];
        foreach (IUrlParameterKeyFormatter formatter in names)
        {
            Console.WriteLine(formatter.Format(Key)); // PageSize, pageSize, page_size, page-size
        }

        SampleCheck.Equal(Key, names[0].Format(Key));
        SampleCheck.Equal("pageSize", RefitSettings.CamelCase().UrlParameterKeyFormatter.Format(Key));
        SampleCheck.Equal("page_size", RefitSettings.SnakeCase().UrlParameterKeyFormatter.Format(Key));
        SampleCheck.Equal("page-size", RefitSettings.KebabCase().UrlParameterKeyFormatter.Format(Key));

        DefaultUrlParameterFormatter values = new();
        values.AddFormat<DateTime>("yyyy-MM");
        values.AddFormat<DateFilter, DateTime>(DateFormat);
        RefitSettings settings = new(host.Settings.ContentSerializer) { UrlParameterFormatter = values };
        IFormatterApi api = RestService.ForGenerated<IFormatterApi>(host.Client, settings);
        DateTime day = DateTime.ParseExact(DayText, DateFormat, CultureInfo.InvariantCulture);
        using HttpRequestMessage dates = await api.DatesAsync(day, new() { Started = day, End = day });
        Console.WriteLine(dates.RequestUri); // /reports?day=2026-09&Started=2026-09-17&End=2026

        SampleCheck.Equal("/reports?day=2026-09&Started=2026-09-17&End=2026", dates.RequestUri?.OriginalString);

        settings.UrlParameterFormatterMap[typeof(bool)] = new YesNoFormatter();
        using HttpRequestMessage active = await api.ActiveAsync(true);
        Console.WriteLine(active.RequestUri); // /reports?active=yes

        SampleCheck.Equal("/reports?active=yes", active.RequestUri?.OriginalString);
        CheckDirectFormats(values, day);
        CheckFormatGuards(values);
    }

    /// <summary>Checks the public formatting contracts without constructing a request.</summary>
    /// <param name="values">The formatter with general and container-specific date rules.</param>
    /// <param name="day">The date used by both URL and form examples.</param>
    private static void CheckDirectFormats(DefaultUrlParameterFormatter values, DateTime day)
    {
        string? general = values.Format(day, typeof(DateTime), typeof(DateTime));
        string? contained = values.Format(day, typeof(DateTime), typeof(DateFilter));
        DefaultFormUrlEncodedParameterFormatter formValues = new();
        string? formDate = formValues.Format(day, DateFormat);
        string? omitted = formValues.Format(null, null);
        Console.WriteLine(general); // 2026-09
        Console.WriteLine(contained); // 2026-09-17
        Console.WriteLine(formDate); // 2026-09-17
        Console.WriteLine(omitted is null); // True
        SampleCheck.Equal("2026-09", general);
        SampleCheck.Equal(DayText, contained);
        SampleCheck.Equal(DayText, formDate);
        SampleCheck.Equal(null, omitted);
        SampleCheck.Equal(null, values.Format(null, typeof(DateTime), typeof(DateTime)));
        SampleCheck.Equal("unchanged", formValues.Format("unchanged", DateFormat));
        SampleCheck.Equal("1", formValues.Format(1, " "));
    }

    /// <summary>Checks argument validation and duplicate registrations for both AddFormat overloads.</summary>
    /// <param name="values">The formatter containing both date registrations.</param>
    private static void CheckFormatGuards(DefaultUrlParameterFormatter values)
    {
        bool duplicateGeneral = false;
        try
        {
            values.AddFormat<DateTime>(DateFormat);
        }
        catch (ArgumentException)
        {
            duplicateGeneral = true;
        }

        bool duplicateScoped = false;
        try
        {
            values.AddFormat<DateFilter, DateTime>(DateFormat);
        }
        catch (ArgumentException)
        {
            duplicateScoped = true;
        }

        bool providerRequired = false;
        try
        {
            _ = values.Format(null, null!, typeof(DateTime));
        }
        catch (ArgumentNullException)
        {
            providerRequired = true;
        }

        SampleCheck.Equal(true, duplicateGeneral);
        SampleCheck.Equal(true, duplicateScoped);
        SampleCheck.Equal(true, providerRequired);
    }
}
