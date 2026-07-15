// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Refit;

/// <summary>A <see langword="class"/> implementing <see cref="IHttpContentSerializer"/> which provides Xml content serialization.</summary>
/// <remarks>
/// Initializes a new instance of the <see cref="XmlContentSerializer"/> class.
/// </remarks>
public class XmlContentSerializer : IHttpContentSerializer, ISynchronousContentDeserializer
{
    /// <summary>Explains why the trimming warning is suppressed for XML reflection.</summary>
    private const string XmlReflectionTrimmingJustification =
        "Refit's XML serialization uses System.Xml.Serialization reflection that trimming cannot statically preserve. Use the Refit source generator for trimmed/AOT apps.";

    /// <summary>Explains why the AOT warning is suppressed for XML reflection.</summary>
    private const string XmlReflectionAotJustification =
        "Refit's XML serialization may generate serialization assemblies at runtime. Use the Refit source generator for AOT apps.";

    /// <summary>The settings controlling XML serialization.</summary>
    private readonly XmlContentSerializerSettings _settings;

    /// <summary>Caches XML serializers keyed by the serialized type.</summary>
    private readonly ConcurrentDictionary<Type, XmlSerializer> _serializerCache = new();

    /// <summary>Initializes a new instance of the <see cref="XmlContentSerializer"/> class.</summary>
    /// <param name="settings">The settings.</param>
    /// <exception cref="System.ArgumentNullException">settings</exception>
    public XmlContentSerializer(XmlContentSerializerSettings settings)
    {
        ArgumentExceptionHelper.ThrowIfNull(settings);
        _settings = settings;
    }

    /// <summary>Initializes a new instance of the <see cref="XmlContentSerializer"/> class.</summary>
    public XmlContentSerializer()
        : this(new())
    {
    }

    /// <summary>Serialize object of type <typeparamref name="T"/> to a <see cref="HttpContent"/> with Xml.</summary>
    /// <typeparam name="T">Type of the object to serialize from.</typeparam>
    /// <param name="item">Object to serialize.</param>
    /// <returns><see cref="HttpContent"/> that contains the serialized <typeparamref name="T"/> object in Xml.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = XmlReflectionTrimmingJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = XmlReflectionAotJustification)]
    public HttpContent ToHttpContent<T>(T item)
    {
        ArgumentExceptionHelper.ThrowIfNull(item);

        var xmlSerializer = _serializerCache.GetOrAdd(
            item.GetType(),
            static (t, settings) => new XmlSerializer(t, settings.XmlAttributeOverrides),
            _settings);

        using var stream = new MemoryStream();
        using var writer = XmlWriter.Create(
            stream,
            _settings.XmlReaderWriterSettings.WriterSettings);

        // XmlWriter.Create above rejects a writer whose Encoding is null, so by this point the encoding is always set.
        var encoding = _settings.XmlReaderWriterSettings.WriterSettings.Encoding;
        xmlSerializer.Serialize(writer, item, _settings.XmlNamespaces);
        writer.Flush();

        var content = new ByteArrayContent(stream.GetBuffer(), 0, (int)stream.Length);
        content.Headers.ContentType = new("application/xml") { CharSet = encoding.WebName };
        return content;
    }

    /// <summary>Deserializes an object of type <typeparamref name="T"/> from a <see cref="HttpContent"/> object that contains Xml content.</summary>
    /// <typeparam name="T">Type of the object to deserialize to.</typeparam>
    /// <param name="content">HttpContent object with Xml content to deserialize.</param>
    /// <param name="cancellationToken">CancellationToken to abort the deserialization.</param>
    /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = XmlReflectionTrimmingJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = XmlReflectionAotJustification)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter selected explicitly by callers.")]
    public async Task<T?> FromHttpContentAsync<T>(
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        var xmlSerializer = _serializerCache.GetOrAdd(
            typeof(T),
            static (t, settings) =>
                new XmlSerializer(
                    t,
                    settings.XmlAttributeOverrides,
                    [],
                    null,
                    settings.XmlDefaultNamespace),
            _settings);

        using var input = new StringReader(
            await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

        using var reader = XmlReader.Create(
            input,
            _settings.XmlReaderWriterSettings.ReaderSettings);
        return (T?)xmlSerializer.Deserialize(reader);
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = XmlReflectionTrimmingJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = XmlReflectionAotJustification)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter selected explicitly by callers.")]
    public T? DeserializeFromString<T>(string content)
    {
        var xmlSerializer = _serializerCache.GetOrAdd(
            typeof(T),
            static (t, settings) =>
                new XmlSerializer(
                    t,
                    settings.XmlAttributeOverrides,
                    [],
                    null,
                    settings.XmlDefaultNamespace),
            _settings);

        using var input = new StringReader(content);
        using var reader = XmlReader.Create(
            input,
            _settings.XmlReaderWriterSettings.ReaderSettings);
        return (T?)xmlSerializer.Deserialize(reader);
    }

    /// <inheritdoc/>
    public string? GetFieldNameForProperty(PropertyInfo propertyInfo)
    {
        ArgumentExceptionHelper.ThrowIfNull(propertyInfo);

        return propertyInfo.GetCustomAttribute<XmlElementAttribute>(true)?.ElementName
               ?? propertyInfo.GetCustomAttribute<XmlAttributeAttribute>(true)?.AttributeName;
    }
}
