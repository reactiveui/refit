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
    /// <summary>
    /// A <see langword="class"/> implementing <see cref="IHttpContentSerializer"/> which provides Xml content serialization.
    /// </summary>
    public class XmlContentSerializer : IHttpContentSerializer
    {
        readonly XmlContentSerializerSettings settings;
        readonly ConcurrentDictionary<Type, XmlSerializer> serializerCache = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlContentSerializer"/> class.
        /// </summary>
        public XmlContentSerializer()
            : this(new XmlContentSerializerSettings()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlContentSerializer"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <exception cref="System.ArgumentNullException">settings</exception>
        public XmlContentSerializer(XmlContentSerializerSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Serialize object of type <typeparamref name="T"/> to a <see cref="HttpContent"/> with Xml.
        /// </summary>
        /// <typeparam name="T">Type of the object to serialize from.</typeparam>
        /// <param name="item">Object to serialize.</param>
        /// <returns><see cref="HttpContent"/> that contains the serialized <typeparamref name="T"/> object in Xml.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public HttpContent ToHttpContent<T>(T item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            var xmlSerializer = serializerCache.GetOrAdd(
                item.GetType(),
                t => new XmlSerializer(t, settings.XmlAttributeOverrides)
            );

            using var stream = new MemoryStream();
            using var writer = XmlWriter.Create(
                stream,
                settings.XmlReaderWriterSettings.WriterSettings
            );
            var encoding =
                settings.XmlReaderWriterSettings.WriterSettings?.Encoding ?? Encoding.Unicode;
            xmlSerializer.Serialize(writer, item, settings.XmlNamespaces);
            var str = encoding.GetString(stream.ToArray());
            var content = new StringContent(str, encoding, "application/xml");
            return content;
        }

        /// <summary>
        /// Deserializes an object of type <typeparamref name="T"/> from a <see cref="HttpContent"/> object that contains Xml content.
        /// </summary>
        /// <typeparam name="T">Type of the object to deserialize to.</typeparam>
        /// <param name="content">HttpContent object with Xml content to deserialize.</param>
        /// <param name="cancellationToken">CancellationToken to abort the deserialization.</param>
        /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
        public async Task<T?> FromHttpContentAsync<T>(
            HttpContent content,
            CancellationToken cancellationToken = default
        )
        {
            var xmlSerializer = serializerCache.GetOrAdd(
                typeof(T),
                t =>
                    new XmlSerializer(
                        t,
                        settings.XmlAttributeOverrides,
                        [],
                        null,
                        settings.XmlDefaultNamespace
                    )
            );

            using var input = new StringReader(
                await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
            );

            using var reader = XmlReader.Create(
                input,
                settings.XmlReaderWriterSettings.ReaderSettings
            );
            return (T?)xmlSerializer.Deserialize(reader);
        }

        /// <inheritdoc/>
        public string? GetFieldNameForProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo is null)
                throw new ArgumentNullException(nameof(propertyInfo));

            return propertyInfo
                    .GetCustomAttributes<XmlElementAttribute>(true)
                    .Select(a => a.ElementName)
                    .FirstOrDefault()
                ?? propertyInfo
                    .GetCustomAttributes<XmlAttributeAttribute>(true)
                    .Select(a => a.AttributeName)
                    .FirstOrDefault();
        }
    }

    /// <summary>
    /// XmlReaderWriterSettings.
    /// </summary>
    public class XmlReaderWriterSettings
    {
        XmlReaderSettings readerSettings;
        XmlWriterSettings writerSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlReaderWriterSettings"/> class.
        /// </summary>
        public XmlReaderWriterSettings()
            : this(new XmlReaderSettings(), new XmlWriterSettings()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlReaderWriterSettings"/> class.
        /// </summary>
        /// <param name="readerSettings">The reader settings.</param>
        public XmlReaderWriterSettings(XmlReaderSettings readerSettings)
            : this(readerSettings, new XmlWriterSettings()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlReaderWriterSettings"/> class.
        /// </summary>
        /// <param name="writerSettings">The writer settings.</param>
        public XmlReaderWriterSettings(XmlWriterSettings writerSettings)
            : this(new XmlReaderSettings(), writerSettings) { }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlReaderWriterSettings"/> class.
        /// </summary>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="writerSettings">The writer settings.</param>
        public XmlReaderWriterSettings(
            XmlReaderSettings readerSettings,
            XmlWriterSettings writerSettings
        )
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            ReaderSettings = readerSettings;
            WriterSettings = writerSettings;
        }

        /// <summary>
        /// Gets or sets the reader settings.
        /// </summary>
        /// <value>
        /// The reader settings.
        /// </value>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public XmlReaderSettings ReaderSettings
        {
            get
            {
                ApplyOverrideSettings();
                return readerSettings;
            }
            set => readerSettings = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the writer settings.
        /// </summary>
        /// <value>
        /// The writer settings.
        /// </value>
        /// <exception cref="System.ArgumentNullException">value</exception>
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

    /// <summary>
    /// XmlContentSerializerSettings.
    /// </summary>
    public class XmlContentSerializerSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlContentSerializerSettings"/> class.
        /// </summary>
        public XmlContentSerializerSettings()
        {
            XmlDefaultNamespace = null;
            XmlReaderWriterSettings = new XmlReaderWriterSettings();
            XmlNamespaces = new XmlSerializerNamespaces(
                [new XmlQualifiedName(string.Empty, string.Empty),]
            );

            XmlAttributeOverrides = new XmlAttributeOverrides();
        }

        /// <summary>
        /// Gets or sets the XML default namespace.
        /// </summary>
        /// <value>
        /// The XML default namespace.
        /// </value>
        public string? XmlDefaultNamespace { get; set; }

        /// <summary>
        /// Gets or sets the XML reader writer settings.
        /// </summary>
        /// <value>
        /// The XML reader writer settings.
        /// </value>
        public XmlReaderWriterSettings XmlReaderWriterSettings { get; set; }

        /// <summary>
        /// Gets or sets the XML namespaces.
        /// </summary>
        /// <value>
        /// The XML namespaces.
        /// </value>
        public XmlSerializerNamespaces XmlNamespaces { get; set; }

        /// <summary>
        /// Gets or sets the XML attribute overrides.
        /// </summary>
        /// <value>
        /// The XML attribute overrides.
        /// </value>
        public XmlAttributeOverrides XmlAttributeOverrides { get; set; }
    }
}
