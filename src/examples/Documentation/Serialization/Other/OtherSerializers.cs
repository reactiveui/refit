// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Refit.Documentation;

/// <summary>Verifies Newtonsoft.Json and XML content serialization against Refit source.</summary>
internal static class OtherSerializers
{
    /// <summary>Runs both serializers' public method examples.</summary>
    /// <returns>Completion after both serializers pass their checks.</returns>
    internal static async Task RunAsync()
    {
        await ShowNewtonsoftAsync();
        await ShowXmlAsync();
        await ShowXmlOverridesAsync();
    }

    /// <summary>Checks default and custom JSON serialization with an HTTP content round trip.</summary>
    /// <returns>Completion after the JSON content checks.</returns>
    private static async Task ShowNewtonsoftAsync()
    {
        NewtonsoftJsonContentSerializer defaults = new();
        JsonSerializerSettings jsonSettings = new() { NullValueHandling = NullValueHandling.Ignore, TypeNameHandling = TypeNameHandling.None };
        NewtonsoftJsonContentSerializer serializer = new(jsonSettings);
        RefitSettings settings = new(serializer);

        SampleCheck.Equal(serializer, settings.ContentSerializer);
        using HttpContent defaultContent = defaults.ToHttpContent(new WirePerson { Id = 1, Name = "Ada" });
        SampleCheck.Equal("Ada", (await defaults.FromHttpContentAsync<WirePerson>(defaultContent))?.Name);
        using HttpContent omittedNull = serializer.ToHttpContent(new WirePerson { Id = 1, Name = null! });
        SampleCheck.Equal("{\"Id\":1}", await omittedNull.ReadAsStringAsync());

        using HttpContent content = serializer.ToHttpContent(new WirePerson { Id = 1, Name = "Ada" });
        Console.WriteLine(content.Headers.ContentType?.MediaType); // application/json
        WirePerson? person = await serializer.FromHttpContentAsync<WirePerson>(content, CancellationToken.None);
        Console.WriteLine(person?.Name); // Ada

        SampleCheck.Equal("Ada", person?.Name);
        SampleCheck.Equal("application/json", content.Headers.ContentType?.MediaType);
        ShowNewtonsoftSynchronous(serializer);
    }

    /// <summary>Checks buffered JSON parsing and explicit property names.</summary>
    /// <param name="serializer">The configured JSON serializer.</param>
    private static void ShowNewtonsoftSynchronous(NewtonsoftJsonContentSerializer serializer)
    {
        WirePerson? person = serializer.DeserializeFromString<WirePerson>("""{"Id":1,"display_name":"Ada"}""");
        string? name = serializer.GetFieldNameForProperty(typeof(WirePerson).GetProperty(nameof(WirePerson.Name))!);
        Console.WriteLine(person?.Name); // Ada
        Console.WriteLine(name); // display_name

        SampleCheck.Equal("Ada", person?.Name);
        SampleCheck.Equal("display_name", name);
    }

    /// <summary>Checks XML configuration and an HTTP content round trip.</summary>
    /// <returns>Completion after the XML content checks.</returns>
    private static async Task ShowXmlAsync()
    {
        XmlReaderWriterSettings defaults = new();
        XmlReaderSettings reader = new() { IgnoreComments = true };
        XmlWriterSettings writer = new() { Encoding = Encoding.UTF8, Indent = true };
        XmlReaderWriterSettings withReader = new(reader);
        XmlReaderWriterSettings withWriter = new(writer);
        XmlReaderWriterSettings both = new(reader, writer);
        both.ReaderSettings = reader;
        both.WriterSettings = writer;
        XmlContentSerializerSettings xmlSettings = new()
        {
            XmlDefaultNamespace = null,
            XmlReaderWriterSettings = both,
            XmlNamespaces = new([new(string.Empty, string.Empty)]),
            XmlAttributeOverrides = new(),
        };
        XmlContentSerializer serializer = new(xmlSettings);
        RefitSettings settings = new(serializer);
        XmlContentSerializer defaultSerializer = new();

        SampleCheck.Equal(serializer, settings.ContentSerializer);
        SampleCheck.Equal(true, defaults.ReaderSettings.Async);
        SampleCheck.Equal(reader, withReader.ReaderSettings);
        SampleCheck.Equal(writer, withWriter.WriterSettings);
        SampleCheck.Equal(DtdProcessing.Prohibit, both.ReaderSettings.DtdProcessing);
        using HttpContent defaultContent = defaultSerializer.ToHttpContent(new WirePerson());
        SampleCheck.Equal(string.Empty, (await defaultSerializer.FromHttpContentAsync<WirePerson>(defaultContent))?.Name);

        using HttpContent content = serializer.ToHttpContent(new WirePerson { Id = 1, Name = "Ada" });
        Console.WriteLine(content.Headers.ContentType?.MediaType); // application/xml
        WirePerson? person = await serializer.FromHttpContentAsync<WirePerson>(content, CancellationToken.None);
        Console.WriteLine(person?.Name); // Ada

        SampleCheck.Equal("Ada", person?.Name);
        SampleCheck.Equal("application/xml", content.Headers.ContentType?.MediaType);
        SampleCheck.Equal(writer.Encoding.WebName, content.Headers.ContentType?.CharSet);
        ShowXmlSynchronous(serializer);
    }

    /// <summary>Checks nonempty namespace mappings and attribute overrides in the actual XML payload.</summary>
    /// <returns>Completion after the overridden XML round trip.</returns>
    private static async Task ShowXmlOverridesAsync()
    {
        const string serviceNamespace = "urn:people";
        XmlAttributeOverrides overrides = new();
        overrides.Add(typeof(WirePerson), new XmlAttributes { XmlRoot = new("contact") { Namespace = serviceNamespace } });
        XmlAttributes name = new();
        _ = name.XmlElements.Add(new("label") { Namespace = serviceNamespace });
        overrides.Add(typeof(WirePerson), nameof(WirePerson.Name), name);
        XmlContentSerializerSettings settings = new() { XmlDefaultNamespace = serviceNamespace, XmlAttributeOverrides = overrides, XmlNamespaces = new([new("p", serviceNamespace)]) };
        XmlContentSerializer serializer = new(settings);
        using HttpContent content = serializer.ToHttpContent(new WirePerson { Id = 1, Name = "Ada" });
        XmlDocument document = new();
        document.LoadXml(await content.ReadAsStringAsync());
        SampleCheck.Equal("contact", document.DocumentElement?.LocalName);
        SampleCheck.Equal(serviceNamespace, document.DocumentElement?.NamespaceURI);
        SampleCheck.Equal("p", document.DocumentElement?.Prefix);
        SampleCheck.Equal("Ada", document.GetElementsByTagName("label", serviceNamespace)[0]?.InnerText);
        SampleCheck.Equal("Ada", (await serializer.FromHttpContentAsync<WirePerson>(content))?.Name);

        XmlContentSerializer namespaceReader = new(new() { XmlDefaultNamespace = serviceNamespace });
        const string namespacedXml = "<person xmlns=\"urn:people\"><Id>1</Id><display_name>Ada</display_name></person>";
        WirePerson? person = namespaceReader.DeserializeFromString<WirePerson>(namespacedXml);
        SampleCheck.Equal("Ada", person?.Name);
        using HttpContent readFirst = namespaceReader.ToHttpContent(new WirePerson { Id = 1, Name = "Ada" });
        document.LoadXml(await readFirst.ReadAsStringAsync());
        SampleCheck.Equal(serviceNamespace, document.DocumentElement?.NamespaceURI);

        XmlContentSerializer namespaceWriter = new(new() { XmlDefaultNamespace = serviceNamespace });
        using HttpContent writeFirst = namespaceWriter.ToHttpContent(new WirePerson { Id = 1, Name = "Ada" });
        document.LoadXml(await writeFirst.ReadAsStringAsync());
        SampleCheck.Equal(string.Empty, document.DocumentElement?.NamespaceURI);
        bool rejected = false;
        try
        {
            _ = namespaceWriter.DeserializeFromString<WirePerson>(namespacedXml);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        SampleCheck.Equal(true, rejected);
    }

    /// <summary>Checks buffered XML parsing and explicit element names.</summary>
    /// <param name="serializer">The configured XML serializer.</param>
    private static void ShowXmlSynchronous(XmlContentSerializer serializer)
    {
        WirePerson? person = serializer.DeserializeFromString<WirePerson>("<person><Id>1</Id><display_name>Ada</display_name></person>");
        string? name = serializer.GetFieldNameForProperty(typeof(WirePerson).GetProperty(nameof(WirePerson.Name))!);
        Console.WriteLine(person?.Name); // Ada
        Console.WriteLine(name); // display_name
        SampleCheck.Equal("Ada", person?.Name);
        SampleCheck.Equal("display_name", name);
    }
}
