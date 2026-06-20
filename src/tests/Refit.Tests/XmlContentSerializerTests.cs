// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Refit.Tests;

/// <summary>Tests for the <see cref="XmlContentSerializer"/>.</summary>
public class XmlContentSerializerTests
{
    /// <summary>Verifies the produced content uses the application/xml media type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MediaTypeShouldBeApplicationXmlAsync()
    {
        var dto = BuildDto();
        var sut = new XmlContentSerializer();

        var content = sut.ToHttpContent(dto);

        await Assert.That(content.Headers.ContentType?.MediaType).IsEqualTo("application/xml");
    }

    /// <summary>Verifies a DTO serializes to the expected XML element values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldSerializeToXml()
    {
        var dto = BuildDto();
        var sut = new XmlContentSerializer();

        var content = sut.ToHttpContent(dto);
        var document = new XmlDocument();
        document.LoadXml(await content.ReadAsStringAsync());

        var root =
            document[nameof(Dto)] ?? throw new InvalidOperationException("Root element was not found");
        await Assert.That(
            XmlConvert.ToDateTime(
                root[nameof(Dto.CreatedOn)]!.InnerText,
                XmlDateTimeSerializationMode.Utc)).IsEqualTo(dto.CreatedOn);
        await Assert.That(root[nameof(Dto.Identifier)]!.InnerText).IsEqualTo(dto.Identifier);
        await Assert.That(root[nameof(Dto.Name)]!.InnerText).IsEqualTo(dto.Name);
    }

    /// <summary>Verifies attribute overrides rename the serialized root element.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldSerializeToXmlUsingAttributeOverrides()
    {
        const string overridenRootElementName = "dto-ex";

        var dto = BuildDto();
        var serializerSettings = new XmlContentSerializerSettings();
        var attributes = new XmlAttributes
        {
            XmlRoot = new(overridenRootElementName)
        };
        serializerSettings.XmlAttributeOverrides.Add(dto.GetType(), attributes);
        var sut = new XmlContentSerializer(serializerSettings);

        var content = sut.ToHttpContent(dto);
        var document = new XmlDocument();
        document.LoadXml(await content.ReadAsStringAsync());

        await Assert.That(document.DocumentElement?.Name).IsEqualTo(overridenRootElementName);
    }

    /// <summary>Verifies namespace overrides apply the configured prefix.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldSerializeToXmlUsingNamespaceOverrides()
    {
        const string prefix = "google";

        var dto = BuildDto();
        var serializerSettings = new XmlContentSerializerSettings
        {
            XmlNamespaces = new()
        };
        serializerSettings.XmlNamespaces.Add(prefix, "https://google.com");
        var sut = new XmlContentSerializer(serializerSettings);

        var content = sut.ToHttpContent(dto);
        var document = new XmlDocument();
        document.LoadXml(await content.ReadAsStringAsync());

        await Assert.That(document["Dto"]?["Name", "https://google.com"]?.Prefix).IsEqualTo(prefix);
    }

    /// <summary>Verifies a DTO can be deserialized from XML content.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldDeserializeFromXmlAsync()
    {
        var serializerSettings = new XmlContentSerializerSettings
        {
            XmlNamespaces = new()
        };
        var sut = new XmlContentSerializer(serializerSettings);

        var dto = await sut.FromHttpContentAsync<Dto>(
            new StringContent("<Dto><Identifier>123</Identifier></Dto>"));

        await Assert.That(dto?.Identifier).IsEqualTo("123");
    }

    /// <summary>Verifies the declared XML encoding matches the configured writer settings.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task XmlEncodingShouldMatchWriterSettingAsync()
    {
        var encoding = Encoding.UTF32;
        var serializerSettings = new XmlContentSerializerSettings
        {
            XmlReaderWriterSettings = new()
            {
                WriterSettings = new() { Encoding = encoding }
            }
        };
        var sut = new XmlContentSerializer(serializerSettings);

        var dto = BuildDto();
        var content = sut.ToHttpContent(dto);
        var xml = XDocument.Parse(await content.ReadAsStringAsync());
        var documentEncoding = xml.Declaration?.Encoding;
        await Assert.That(documentEncoding).IsEqualTo(encoding.WebName);
    }

    /// <summary>Builds a populated <see cref="Dto"/> instance for the tests.</summary>
    /// <returns>A new <see cref="Dto"/>.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S6566:Prefer using \"DateTimeOffset\" instead of \"DateTime\"",
        Justification = "Test intentionally exercises DateTime XML round-trip via XmlConvert.ToDateTime.")]
    private static Dto BuildDto() =>
        new()
        {
            CreatedOn = DateTime.UtcNow,
            Identifier = Guid.NewGuid().ToString(),
            Name = "Test Dto Object"
        };

    /// <summary>A simple data transfer object used to exercise XML serialization.</summary>
    public class Dto
    {
        /// <summary>Gets or sets the creation timestamp.</summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>Gets or sets the identifier.</summary>
        public string? Identifier { get; set; }

        /// <summary>Gets or sets the name, serialized into the Google namespace.</summary>
        [XmlElement(Namespace = "https://google.com")]
        public string? Name { get; set; }
    }
}
