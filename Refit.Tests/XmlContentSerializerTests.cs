using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xunit;

namespace Refit.Tests
{
    public class XmlContentSerializerTests
    {
        public class Dto
        {
            public DateTime CreatedOn { get; set; }

            public string Identifier { get; set; }

            [XmlElement(Namespace = "https://google.com")]
            public string Name { get; set; }
        }

        [Fact]
        public async Task MediaTypeShouldBeApplicationXmlAsync()
        {
            var dto = BuildDto();
            var sut = new XmlContentSerializer();

            var content = await sut.SerializeAsync(dto);

            Assert.Equal("application/xml", content.Headers.ContentType.MediaType);
        }

        [Fact]
        public async Task ShouldSerializeToXml()
        {
            var dto = BuildDto();
            var sut = new XmlContentSerializer();

            var content = await sut.SerializeAsync(dto);
            var document = new XmlDocument();
            document.LoadXml(await content.ReadAsStringAsync());

            var root = document[nameof(Dto)] ?? throw new NullReferenceException("Root element was not found");
            Assert.Equal(dto.CreatedOn, XmlConvert.ToDateTime(root[nameof(Dto.CreatedOn)].InnerText, XmlDateTimeSerializationMode.Utc));
            Assert.Equal(dto.Identifier, root[nameof(Dto.Identifier)].InnerText);
            Assert.Equal(dto.Name, root[nameof(Dto.Name)].InnerText);
        }

        [Fact]
        public async Task ShouldSerializeToXmlUsingAttributeOverrides()
        {
            const string overridenRootElementName = "dto-ex";

            var dto = BuildDto();
            var serializerSettings = new XmlContentSerializerSettings();
            var attributes = new XmlAttributes { XmlRoot = new XmlRootAttribute(overridenRootElementName) };
            serializerSettings.XmlAttributeOverrides.Add(dto.GetType(), attributes);
            var sut = new XmlContentSerializer(serializerSettings);

            var content = await sut.SerializeAsync(dto);
            var document = new XmlDocument();
            document.LoadXml(await content.ReadAsStringAsync());

            Assert.Equal(overridenRootElementName, document.DocumentElement?.Name);
        }

        [Fact]
        public async Task ShouldSerializeToXmlUsingNamespaceOverrides()
        {
            const string prefix = "google";

            var dto = BuildDto();
            var serializerSettings = new XmlContentSerializerSettings { XmlNamespaces = new XmlSerializerNamespaces() };
            serializerSettings.XmlNamespaces.Add(prefix, "https://google.com");
            var sut = new XmlContentSerializer(serializerSettings);

            var content = await sut.SerializeAsync(dto);
            var document = new XmlDocument();
            document.LoadXml(await content.ReadAsStringAsync());

            Assert.Equal(prefix, document["Dto"]?["Name", "https://google.com"]?.Prefix);
        }

        [Fact]
        public async Task ShouldDeserializeFromXmlAsync()
        {
            var serializerSettings = new XmlContentSerializerSettings { XmlNamespaces = new XmlSerializerNamespaces() };
            var sut = new XmlContentSerializer(serializerSettings);

            var dto = await sut.DeserializeAsync<Dto>(new StringContent("<Dto><Identifier>123</Identifier></Dto>"));

            Assert.Equal("123", dto.Identifier);
        }

        [Fact]
        public async Task XmlEncodingShouldMatchWriterSettingAsync()
        {
            var encoding = Encoding.UTF7;
            var serializerSettings = new XmlContentSerializerSettings
            {
                XmlReaderWriterSettings = new XmlReaderWriterSettings()
                {
                    WriterSettings = new XmlWriterSettings()
                    {
                        Encoding = encoding
                    }
                }
            };
            var sut = new XmlContentSerializer(serializerSettings);

            var dto = BuildDto();
            var content = await sut.SerializeAsync(dto);
            var xml = XDocument.Parse(await content.ReadAsStringAsync());
            var documentEncoding = xml.Declaration.Encoding;
            Assert.Equal(encoding.WebName, documentEncoding);
        }

        static Dto BuildDto()
        {
            var dto = new Dto
            {
                CreatedOn = DateTime.UtcNow,
                Identifier = Guid.NewGuid().ToString(),
                Name = "Test Dto Object"
            };
            return dto;
        }
    }
}
