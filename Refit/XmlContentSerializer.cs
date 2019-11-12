using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Refit
{

    public class XmlContentSerializer : IContentSerializer
    {
        readonly XmlContentSerializerSettings settings;
        readonly ConcurrentDictionary<Type, XmlSerializer> serializerCache = new ConcurrentDictionary<Type, XmlSerializer>();

        public XmlContentSerializer() : this(new XmlContentSerializerSettings())
        {
        }

        public XmlContentSerializer(XmlContentSerializerSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public Task<HttpContent> SerializeAsync<T>(T item)
        {
            var xmlSerializer = serializerCache.GetOrAdd(item.GetType(), t => new XmlSerializer(t, settings.XmlAttributeOverrides));

            using var stream = new MemoryStream();
            using var writer = XmlWriter.Create(stream, settings.XmlReaderWriterSettings.WriterSettings);
            var encoding = settings.XmlReaderWriterSettings.WriterSettings?.Encoding ?? Encoding.Unicode;
            xmlSerializer.Serialize(writer, item, settings.XmlNamespaces);
            var str = encoding.GetString(stream.ToArray());
            var content = new StringContent(str, encoding, "application/xml");
            return Task.FromResult((HttpContent)content);
        }

        public async Task<T> DeserializeAsync<T>(HttpContent content)
        {
            var xmlSerializer = serializerCache.GetOrAdd(typeof(T), t => new XmlSerializer(t, settings.XmlAttributeOverrides));

            using var input = new StringReader(await content.ReadAsStringAsync().ConfigureAwait(false));
            using var reader = XmlReader.Create(input, settings.XmlReaderWriterSettings.ReaderSettings);
            return (T)xmlSerializer.Deserialize(reader);
        }
    }

    public class XmlReaderWriterSettings
    {
        XmlReaderSettings readerSettings;
        XmlWriterSettings writerSettings;

        public XmlReaderWriterSettings() : this(new XmlReaderSettings(), new XmlWriterSettings())
        {
        }

        public XmlReaderWriterSettings(XmlReaderSettings readerSettings) : this(readerSettings, new XmlWriterSettings())
        {
        }

        public XmlReaderWriterSettings(XmlWriterSettings writerSettings) : this(new XmlReaderSettings(), writerSettings)
        {
        }

        public XmlReaderWriterSettings(XmlReaderSettings readerSettings, XmlWriterSettings writerSettings)
        {
            ReaderSettings = readerSettings;
            WriterSettings = writerSettings;
        }

        public XmlReaderSettings ReaderSettings
        {
            get
            {
                ApplyOverrideSettings();
                return readerSettings;
            }
            set => readerSettings = value ?? throw new ArgumentNullException(nameof(value));
        }

        public XmlWriterSettings WriterSettings
        {
            get
            {
                ApplyOverrideSettings();
                return writerSettings;
            }
            set => writerSettings = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// The writer and reader settings are set by the caller, but certain properties
        /// should remain set to meet the demands of the XmlContentSerializer. Those properties
        /// are always set here.
        /// </summary>
        void ApplyOverrideSettings()
        {
            writerSettings.Async = true;
            readerSettings.Async = true;
        }
    }

    public class XmlContentSerializerSettings
    {
        public XmlContentSerializerSettings()
        {
            XmlReaderWriterSettings = new XmlReaderWriterSettings();
            XmlNamespaces = new XmlSerializerNamespaces(
                new[]
                {
                    new XmlQualifiedName(string.Empty, string.Empty),
                });

            XmlAttributeOverrides = new XmlAttributeOverrides();
        }

        public XmlReaderWriterSettings XmlReaderWriterSettings { get; set; }

        public XmlSerializerNamespaces XmlNamespaces { get; set; }

        public XmlAttributeOverrides XmlAttributeOverrides { get; set; }
    }
}
