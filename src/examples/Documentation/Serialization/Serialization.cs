// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Refit.Documentation;

/// <summary>Checks generated metadata, synchronous and streaming serialization and missing-type failures.</summary>
internal static class Serialization
{
    /// <summary>Resolves only models registered with the shared generated JSON context.</summary>
    private static readonly JsonSerializerOptions Options = new(SampleJsonContext.Default.Options) { TypeInfoResolver = SampleJsonContext.Default, };

    /// <summary>Reuses immutable generated-metadata serializer configuration across the demonstrations.</summary>
    private static readonly SystemTextJsonContentSerializer Serializer = new(Options);

    /// <summary>Checks person serialization paths, combined contexts and rejection of missing metadata.</summary>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    internal static async Task RunAsync()
    {
        SystemTextJsonContentSerializer serializer = Serializer;
        using HttpContent content = serializer.ToHttpContent(new Person(1, "Ada"));
        Person? person = await serializer.FromHttpContentAsync<Person>(content);
        Console.WriteLine(person?.Name); // Ada

        SampleCheck.Equal("Ada", person?.Name);
        SystemTextJsonContentSerializer synchronous = Serializer;
        using HttpContent buffered = synchronous.ToHttpContentSynchronous(new Person(1, "Ada"));
        using HttpContent streamed = synchronous.ToStreamingHttpContent(new Person(1, "Ada"));
        Person? fromText = Serializer.DeserializeFromString<Person>(await buffered.ReadAsStringAsync());
        Person? fromStream = await Serializer.FromHttpContentAsync<Person>(streamed);
        Console.WriteLine(fromText?.Name); // Ada
        Console.WriteLine(fromStream?.Name); // Ada

        SampleCheck.Equal("Ada", fromText?.Name);
        SampleCheck.Equal("Ada", fromStream?.Name);

        JsonTypeInfo<Person> personInfo = SampleJsonContext.Default.Person;
        string json = JsonSerializer.Serialize(new(1, "Ada"), personInfo);
        Person? restored = JsonSerializer.Deserialize(json, personInfo);
        Console.WriteLine(restored?.Name); // Ada

        SampleCheck.Equal("Ada", restored?.Name);
        SampleCheck.Equal(Options, Serializer.SerializerOptions);
        await RunDefaultsAsync();
        VerifyFieldNames();
        await RunFastPathAsync();
        await RunCombinedContextsAsync();
        VerifyMissingMetadata();
    }

    /// <summary>Checks fresh defaults and configuring the default constructor for generated metadata.</summary>
    /// <returns>Completion of the serializer default checks.</returns>
    private static async Task RunDefaultsAsync()
    {
        SystemTextJsonContentSerializer defaults = new();
        JsonSerializerOptions defaultOptions = defaults.SerializerOptions;
        defaultOptions.TypeInfoResolver = SampleJsonContext.Default;
        JsonSerializerOptions separateDefaults = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
        using HttpContent content = defaults.ToHttpContent(new Person(1, "Ada"));
        Person? person = await defaults.FromHttpContentAsync<Person>(content, CancellationToken.None);
        Console.WriteLine(person!.Name); // Ada
        SampleCheck.Equal("Ada", person.Name);
        SampleCheck.Equal(false, ReferenceEquals(defaultOptions, separateDefaults));
        SampleCheck.Equal(true, defaultOptions.PropertyNameCaseInsensitive);
        SampleCheck.Equal(false, defaultOptions.AllowDuplicateProperties);
        SampleCheck.Equal(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString, defaultOptions.NumberHandling);
        SampleCheck.Equal("displayName", defaultOptions.PropertyNamingPolicy!.ConvertName("DisplayName"));
    }

    /// <summary>Checks explicit JSON property names without applying the camel-case naming policy.</summary>
    private static void VerifyFieldNames()
    {
        JsonNamedValue model = new();
        System.Reflection.PropertyInfo explicitName = model.GetType().GetProperty(nameof(JsonNamedValue.Value))!;
        System.Reflection.PropertyInfo policyOnly = typeof(Person).GetProperty(nameof(Person.Name))!;
        string? name = Serializer.GetFieldNameForProperty(explicitName);
        string? absent = Serializer.GetFieldNameForProperty(policyOnly);
        Console.WriteLine(name); // wire-name
        Console.WriteLine(absent is null); // True
        SampleCheck.Equal("wire-name", name);
        SampleCheck.Equal(true, absent is null);
        SampleCheck.Equal("wire-name", GetCapabilityName(Serializer, explicitName));
    }

    /// <summary>Checks the reflected naming hook through the base serializer capability.</summary>
    /// <typeparam name="TSerializer">The content serializer capability selected by the caller.</typeparam>
    /// <param name="serializer">The serializer whose naming contract is called.</param>
    /// <param name="property">The known property to inspect.</param>
    /// <returns>The explicit field name, or null.</returns>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static string? GetCapabilityName<TSerializer>(TSerializer serializer, System.Reflection.PropertyInfo property)
        where TSerializer : IHttpContentSerializer => serializer.GetFieldNameForProperty(property);

    /// <summary>Checks that fast-path options work after a generated resolver is supplied.</summary>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    private static async Task RunFastPathAsync()
    {
        JsonSerializerOptions fastOptions = SystemTextJsonContentSerializer.GetFastPathJsonSerializerOptions();
        fastOptions.TypeInfoResolver = SampleJsonContext.Default;
        SystemTextJsonContentSerializer fastSerializer = new(fastOptions);
        using HttpContent fastContent = fastSerializer.ToHttpContentSynchronous(new Person(1, "Ada"));
        Person? fromFastContent = await fastSerializer.FromHttpContentAsync<Person>(fastContent);
        Console.WriteLine(fromFastContent?.Name); // Ada

        SampleCheck.Equal("Ada", fromFastContent?.Name);
        SampleCheck.Equal(0, fastOptions.Converters.Count);
    }

    /// <summary>Checks that combining generated contexts enables the separately registered page model.</summary>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    private static async Task RunCombinedContextsAsync()
    {
        IJsonTypeInfoResolver resolver = JsonTypeInfoResolver.Combine(SampleJsonContext.Default, PageJsonContext.Default);
        JsonSerializerOptions combinedOptions = new(SampleJsonContext.Default.Options) { TypeInfoResolver = resolver };
        SystemTextJsonContentSerializer combinedSerializer = new(combinedOptions);
        using HttpContent pageContent = combinedSerializer.ToHttpContent(new Page<Person>([new(1, "Ada")]));
        Page<Person>? page = await combinedSerializer.FromHttpContentAsync<Page<Person>>(pageContent);
        Console.WriteLine(page?.Items[0].Name); // Ada

        SampleCheck.Equal("Ada", page?.Items[0].Name);
    }

    /// <summary>Checks that the person-only resolver rejects an unregistered closed page type.</summary>
    /// <exception cref="InvalidOperationException">The serializer unexpectedly accepts the unregistered page model.</exception>
    private static void VerifyMissingMetadata()
    {
        try
        {
            // Options resolves only SampleJsonContext's roots. Page<Person> belongs to PageJsonContext.
            using HttpContent missingContent = Serializer.ToHttpContent(new Page<Person>([]));
            throw new InvalidOperationException("Missing JSON metadata should fail.");
        }
        catch (NotSupportedException)
        {
            Console.WriteLine("Register Page<Person> or combine the generated contexts.");
        }
    }
}
