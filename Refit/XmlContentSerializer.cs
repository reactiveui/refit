using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Refit
{

    public class XmlContentSerializer : IHttpContentSerializer
    {
        readonly XmlContentSerializerSettings settings;
        readonly ConcurrentDictionary<Type, XmlSerializer> serializerCache = new();

        public XmlContentSerializer() : this(new XmlContentSerializerSettings())
        {
        }

        public XmlContentSerializer(XmlContentSerializerSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public HttpContent ToHttpContent<T>(T item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            var xmlSerializer = serializerCache.GetOrAdd(item.GetType(), t => new XmlSerializer(t, settings.XmlAttributeOverrides));

            using var stream = new MemoryStream();
            using var writer = XmlWriter.Create(stream, settings.XmlReaderWriterSettings.WriterSettings);
            var encoding = settings.XmlReaderWriterSettings.WriterSettings?.Encoding ?? Encoding.Unicode;
            xmlSerializer.Serialize(writer, item, settings.XmlNamespaces);
            var str = encoding.GetString(stream.ToArray());
            var content = new StringContent(str, encoding, "application/xml");
            return content;
        }

        public async Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default)
        {
            var xmlSerializer = serializerCache.GetOrAdd(typeof(T), t => new XmlSerializer(
                t,
                settings.XmlAttributeOverrides,
                Array.Empty<Type>(),
                null,
                settings.XmlDefaultNamespace));


            using var input = new StringReader(await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

            using var reader = XmlReader.Create(input, settings.XmlReaderWriterSettings.ReaderSettings);
            return (T?)xmlSerializer.Deserialize(reader);
        }

        public string? GetFieldNameForProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo is null)
                throw new ArgumentNullException(nameof(propertyInfo));

            return propertyInfo.GetCustomAttributes<XmlElementAttribute>(true)
                       .Select(a => a.ElementName)
                       .FirstOrDefault()
                  ??
                  propertyInfo.GetCustomAttributes<XmlAttributeAttribute>(true)
                       .Select(a => a.AttributeName)
                       .FirstOrDefault();
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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public XmlReaderWriterSettings(XmlReaderSettings readerSettings, XmlWriterSettings writerSettings)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
            XmlDefaultNamespace = null;
            XmlReaderWriterSettings = new XmlReaderWriterSettings();
            XmlNamespaces = new XmlSerializerNamespaces(
                new[]
                {
                    new XmlQualifiedName(string.Empty, string.Empty),
                });

            XmlAttributeOverrides = new XmlAttributeOverrides();
        }

        public string? XmlDefaultNamespace { get; set; }

        public XmlReaderWriterSettings XmlReaderWriterSettings { get; set; }

        public XmlSerializerNamespaces XmlNamespaces { get; set; }

        public XmlAttributeOverrides XmlAttributeOverrides { get; set; }
    }
}
