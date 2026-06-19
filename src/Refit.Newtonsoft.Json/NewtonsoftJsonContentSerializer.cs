// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace Refit;

/// <summary>A <see langword="class"/> implementing <see cref="IHttpContentSerializer"/> using the Newtonsoft.Json APIs.</summary>
/// <remarks>
/// Creates a new <see cref="NewtonsoftJsonContentSerializer"/> instance with the specified parameters.
/// </remarks>
/// <param name="jsonSerializerSettings">The serialization settings to use for the current instance</param>
public sealed class NewtonsoftJsonContentSerializer(
    JsonSerializerSettings? jsonSerializerSettings) : IHttpContentSerializer
{
    /// <summary>The number of characters consumed by a leading and trailing quote pair around a charset value.</summary>
    private const int QuotePairLength = 2;

    /// <summary>The <see cref="Lazy{T}"/> instance providing the JSON serialization settings to use</summary>
    private readonly Lazy<JsonSerializerSettings> _jsonSerializerSettings =
        new(() =>
            jsonSerializerSettings
            ?? JsonConvert.DefaultSettings?.Invoke()
            ?? new JsonSerializerSettings());

    /// <summary>Initializes a new instance of the <see cref="NewtonsoftJsonContentSerializer"/> class.</summary>
    public NewtonsoftJsonContentSerializer()
        : this(null)
    {
    }

    /// <inheritdoc/>
#if !NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Refit's Newtonsoft.Json serialization uses reflection that trimming cannot statically preserve. Use the Refit source generator for trimmed/AOT apps.")]
    [RequiresDynamicCode("Refit's Newtonsoft.Json serialization may generate code dynamically for runtime types. Use the Refit source generator for AOT apps.")]
#else
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with RequiresUnreferencedCodeAttribute may break when trimming",
        Justification = "Interface method is unannotated on net8.0+ so cannot propagate; Newtonsoft path is documented as unsuitable for trimmed/AOT apps.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Calling members annotated with RequiresDynamicCodeAttribute may break when AOT compiling",
        Justification = "Interface method is unannotated on net8.0+ so cannot propagate; Newtonsoft path is documented as unsuitable for trimmed/AOT apps.")]
#endif
    public HttpContent ToHttpContent<T>(T item)
    {
        return new StringContent(
            JsonConvert.SerializeObject(item, _jsonSerializerSettings.Value),
            Encoding.UTF8,
            "application/json");
    }

    /// <inheritdoc/>
#if !NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Refit's Newtonsoft.Json serialization uses reflection that trimming cannot statically preserve. Use the Refit source generator for trimmed/AOT apps.")]
    [RequiresDynamicCode("Refit's Newtonsoft.Json serialization may generate code dynamically for runtime types. Use the Refit source generator for AOT apps.")]
#else
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with RequiresUnreferencedCodeAttribute may break when trimming",
        Justification = "Interface method is unannotated on net8.0+ so cannot propagate; Newtonsoft path is documented as unsuitable for trimmed/AOT apps.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Calling members annotated with RequiresDynamicCodeAttribute may break when AOT compiling",
        Justification = "Interface method is unannotated on net8.0+ so cannot propagate; Newtonsoft path is documented as unsuitable for trimmed/AOT apps.")]
#endif
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Implements IHttpContentSerializer.FromHttpContentAsync<T>; the type parameter is the deserialization target and cannot be inferred from arguments.")]
    public async Task<T?> FromHttpContentAsync<T>(
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        if (content is null)
        {
            return default;
        }

        var serializer = JsonSerializer.Create(_jsonSerializerSettings.Value);

        await content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
        var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

#if NET6_0_OR_GREATER
        await using (stream.ConfigureAwait(false))
#else
        using (stream)
#endif
        {
            using var reader = new StreamReader(stream, GetEncoding(content) ?? Encoding.UTF8);

            var jsonTextReader = new JsonTextReader(reader);
#if NET6_0_OR_GREATER
            await using (jsonTextReader.ConfigureAwait(false))
#else
            using (jsonTextReader)
#endif
            {
                return serializer.Deserialize<T>(jsonTextReader);
            }
        }
    }

    /// <summary>
    /// Calculates what the field name should be for the given property. This may be affected by custom attributes the serializer understands.
    /// </summary>
    /// <param name="propertyInfo">A PropertyInfo object.</param>
    /// <returns>
    /// The calculated field name.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">propertyInfo</exception>
    public string? GetFieldNameForProperty(PropertyInfo propertyInfo) =>
        propertyInfo switch
        {
            null => throw new ArgumentNullException(nameof(propertyInfo)),
            _
                => propertyInfo
                    .GetCustomAttributes<JsonPropertyAttribute>(true)
                    .Select(a => a.PropertyName)
                    .FirstOrDefault()
        };

    /// <summary>Resolves the text encoding from the content type charset, if present.</summary>
    /// <param name="content">The HTTP content to inspect.</param>
    /// <returns>The resolved encoding, or null when no charset is specified.</returns>
    private static Encoding? GetEncoding(HttpContent content)
    {
        var charset = content.Headers.ContentType?.CharSet;
        if (charset is null)
        {
            return null;
        }

        try
        {
            if (charset.Length > QuotePairLength && charset[0] == '"' && charset[charset.Length - 1] == '"')
            {
                charset = charset.Substring(1, charset.Length - QuotePairLength);
            }

            return Encoding.GetEncoding(charset);
        }
        catch (ArgumentException e)
        {
            throw new InvalidOperationException("The character set provided in ContentType is invalid.", e);
        }
    }
}
