// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Refit.Documentation;

/// <summary>Checks custom and JSON-driven object flattening without sending HTTP requests.</summary>
internal static class Converters
{
    /// <summary>Sets the result limit used by the custom-converter expectation.</summary>
    private const int SearchLimit = 20;

    /// <summary>The second scalar value in the nested query collection.</summary>
    private const int SecondCode = 2;

    /// <summary>The relative path used by direct query construction.</summary>
    private const string QueryPath = "/people";

    /// <summary>The prefix passed to direct JSON flattening.</summary>
    private const string JsonPrefix = "person.";

    /// <summary>Provides generated metadata needed by the person query converter.</summary>
    private static readonly JsonSerializerOptions Options = new(SampleJsonContext.Default.Options) { TypeInfoResolver = SampleJsonContext.Default };

    /// <summary>Uses the generated person serializer for JSON-based query flattening.</summary>
    private static readonly RefitSettings Settings = new(new SystemTextJsonContentSerializer(Options));

    /// <summary>Reuses generated metadata for nested query envelopes.</summary>
    private static readonly JsonSerializerOptions NestedOptions = new(QueryJsonContext.Default.Options) { TypeInfoResolver = QueryJsonContext.Default };

    /// <summary>Builds requests and checks the exact query strings from both converters.</summary>
    /// <param name="host">The local handler and generated-metadata client configuration for these samples.</param>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        IConverterApi api = RestService.ForGenerated<IConverterApi>(host.Client, host.Settings);
        using HttpRequestMessage custom = await api.SearchAsync(new("Ada Lovelace", SearchLimit));
        Console.WriteLine(custom.RequestUri); // /people?q=Ada%20Lovelace&limit=20

        SampleCheck.Equal("/people?q=Ada%20Lovelace&limit=20", custom.RequestUri?.OriginalString);

        IConverterApi jsonApi = RestService.ForGenerated<IConverterApi>(host.Client, Settings);
        using HttpRequestMessage json = await jsonApi.PersonAsync(new(1, "Ada"));
        Console.WriteLine(json.RequestUri); // /people?person.id=1&person.name=Ada

        SampleCheck.Equal("/people?person.id=1&person.name=Ada", json.RequestUri?.OriginalString);
        CheckDirectConversion();
        CheckNestedConversion();
    }

    /// <summary>Checks nested objects, repeated values, null omission and serializer requirements.</summary>
    private static void CheckNestedConversion()
    {
        SystemTextJsonContentSerializer serializer = new(NestedOptions);
        RefitSettings settings = new(serializer) { CollectionFormat = CollectionFormat.Multi };
        SystemTextJsonQueryConverter<JsonQueryEnvelope> converter = new();
        JsonQueryEnvelope value = new(new(1, "Ada"), [1, SecondCode], null);
        GeneratedQueryStringBuilder builder = new(QueryPath);
        converter.Flatten(value, "filter.", ref builder, settings);
        SampleCheck.Equal("/people?filter.person.id=1&filter.person.name=Ada&filter.codes=1&filter.codes=2", builder.Build());

        RefitSettings incompatible = new(new ContentOnlySerializer(serializer));
        GeneratedQueryStringBuilder rejectedBuilder = new(QueryPath);
        bool rejected = false;
        try
        {
            converter.Flatten(value, string.Empty, ref rejectedBuilder, incompatible);
        }
        catch (NotSupportedException)
        {
            rejected = true;
        }

        SampleCheck.Equal(true, rejected);
        SampleCheck.Equal(QueryPath, rejectedBuilder.Build());
    }

    /// <summary>Calls the public JSON converter directly with generated metadata.</summary>
    private static void CheckDirectConversion()
    {
        QueryConverterAttribute attribute = new(typeof(SystemTextJsonQueryConverter<Person>));
        SampleCheck.Equal(typeof(SystemTextJsonQueryConverter<Person>), attribute.ConverterType);
        SystemTextJsonQueryConverter<Person> converter = new();
        GeneratedQueryStringBuilder builder = new(QueryPath);
        converter.Flatten(new(1, "Ada"), JsonPrefix, ref builder, Settings);
        string path = builder.Build();
        Console.WriteLine(path); // /people?person.id=1&person.name=Ada
        SampleCheck.Equal("/people?person.id=1&person.name=Ada", path);
        RefitSettings mapped = new(Settings.ContentSerializer);
        mapped.UrlParameterFormatterMap[typeof(int)] = new YesNoFormatter();
        GeneratedQueryStringBuilder unmappedBuilder = new(QueryPath);
        converter.Flatten(new(1, "Ada"), JsonPrefix, ref unmappedBuilder, mapped);
        SampleCheck.Equal(path, unmappedBuilder.Build());
        GeneratedQueryStringBuilder emptyBuilder = new(QueryPath);
        converter.Flatten(null!, JsonPrefix, ref emptyBuilder, Settings);
        SampleCheck.Equal(QueryPath, emptyBuilder.Build());
    }
}
