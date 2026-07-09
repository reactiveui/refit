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

        await Assert.That(document[nameof(Dto)]?["Name", "https://google.com"]?.Prefix).IsEqualTo(prefix);
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

    /// <summary>Verifies the synchronous DeserializeFromString reads a DTO from XML (#1591).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DeserializeFromStringReadsDto()
    {
        var sut = new XmlContentSerializer(new XmlContentSerializerSettings { XmlNamespaces = new() });

        var dto = sut.DeserializeFromString<Dto>("<Dto><Identifier>123</Identifier></Dto>");

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

    /// <summary>Verifies constructor, serialization, and field-name guard branches.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GuardsAndXmlFieldNamesShouldWork()
    {
        var sut = new XmlContentSerializer();

        await Assert.That(() => new XmlContentSerializer(null!)).ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => sut.ToHttpContent<Dto>(null!)).ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => sut.GetFieldNameForProperty(null!)).ThrowsExactly<ArgumentNullException>();
        await Assert.That(sut.GetFieldNameForProperty(typeof(XmlFieldNameDto).GetProperty(nameof(XmlFieldNameDto.Element))!))
            .IsEqualTo("element-name");
        await Assert.That(sut.GetFieldNameForProperty(typeof(XmlFieldNameDto).GetProperty(nameof(XmlFieldNameDto.Attribute))!))
            .IsEqualTo("attribute-name");
        await Assert.That(sut.GetFieldNameForProperty(typeof(XmlFieldNameDto).GetProperty(nameof(XmlFieldNameDto.Unannotated))!))
            .IsNull();
    }

    /// <summary>Verifies XML reader/writer settings constructors apply async overrides and null guards.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task XmlReaderWriterSettingsConstructorsAndGuardsShouldWork()
    {
        var readerSettings = new XmlReaderSettings();
        var writerSettings = new XmlWriterSettings();

        var readerOnly = new XmlReaderWriterSettings(readerSettings);
        var writerOnly = new XmlReaderWriterSettings(writerSettings);
        var both = new XmlReaderWriterSettings(new(), new());

        await Assert.That(readerOnly.ReaderSettings).IsSameReferenceAs(readerSettings);
        await Assert.That(readerOnly.ReaderSettings.Async).IsTrue();
        await Assert.That(writerOnly.WriterSettings).IsSameReferenceAs(writerSettings);
        await Assert.That(writerOnly.WriterSettings.Async).IsTrue();
        await Assert.That(both.ReaderSettings.Async).IsTrue();
        await Assert.That(both.WriterSettings.Async).IsTrue();
        await Assert.That(() => both.ReaderSettings = null!).ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => both.WriterSettings = null!).ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Verifies DTD processing is forced off and the resolver cleared even when the caller opts into parsing (XXE hardening).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReaderSettingsAlwaysProhibitDtdAndClearResolver()
    {
        var settings = new XmlReaderWriterSettings(
            new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Parse,
                XmlResolver = new XmlUrlResolver()
            });

        await Assert.That(settings.ReaderSettings.DtdProcessing).IsEqualTo(DtdProcessing.Prohibit);
    }

    /// <summary>Verifies the obsolete opt-out leaves caller-configured DTD processing intact.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AllowDtdProcessingOptOutHonorsCallerSettings()
    {
        var settings = new XmlReaderWriterSettings(
            new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse });
#pragma warning disable CS0618 // Intentionally exercising the obsolete XXE opt-out.
        settings.AllowDtdProcessing = true;
#pragma warning restore CS0618

        await Assert.That(settings.ReaderSettings.DtdProcessing).IsEqualTo(DtdProcessing.Parse);
    }

    /// <summary>Verifies a payload carrying an external entity (XXE) is rejected rather than resolved.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DeserializeRejectsExternalEntityPayload()
    {
        var sut = new XmlContentSerializer(new XmlContentSerializerSettings { XmlNamespaces = new() });
        const string xxe =
            "<?xml version=\"1.0\"?>"
            + "<!DOCTYPE Dto [ <!ENTITY xxe SYSTEM \"file:///etc/passwd\"> ]>"
            + "<Dto><Identifier>&xxe;</Identifier></Dto>";

        // XmlSerializer wraps the underlying "DTD is prohibited" XmlException in an InvalidOperationException.
        var exception = await Assert.That(() => sut.FromHttpContentAsync<Dto>(new StringContent(xxe)))
            .ThrowsExactly<InvalidOperationException>();
        await Assert.That(exception!.InnerException).IsTypeOf<XmlException>();
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
            CreatedOn = DateTime.UnixEpoch,
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

    /// <summary>DTO used to verify XML field-name lookup.</summary>
    public class XmlFieldNameDto
    {
        /// <summary>Gets or sets an XML element-backed value.</summary>
        [XmlElement("element-name")]
        public string? Element { get; set; }

        /// <summary>Gets or sets an XML attribute-backed value.</summary>
        [XmlAttribute("attribute-name")]
        public string? Attribute { get; set; }

        /// <summary>Gets or sets an unannotated value.</summary>
        public string? Unannotated { get; set; }
    }
}
