// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.IO.Compression;
using System.Text.Json;

namespace Refit.Documentation.RuntimeHelpers;

/// <summary>Exercises body helpers with generated JSON metadata and explicit form getters.</summary>
internal static class BodySample
{
    /// <summary>The shared immutable serializer options backed by generated metadata.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new() { TypeInfoResolver = HelperJsonContext.Default };

    /// <summary>Creates serializer settings that never require runtime JSON type discovery.</summary>
    /// <returns>Settings backed by generated metadata for the sample types.</returns>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    internal static RefitSettings CreateSettings() => new(new SystemTextJsonContentSerializer(JsonOptions));

    /// <summary>Checks form descriptors, body passthrough, JSON Lines, and compressor availability.</summary>
    /// <returns>Completion of the asynchronous content reads.</returns>
    internal static async Task RunAsync()
    {
        RefitSettings settings = CreateSettings();

        FormBody body = new();
        const string formPrefix = "form.";
        FormField<FormBody> count = new(static value => value.Count, nameof(FormBody.Count), nameof(count), formPrefix, "D3", null, false);
        FormField<FormBody> note = new(static value => value.Note, nameof(FormBody.Note), "note", null, null, CollectionFormat.Csv, true);
        FormField<FormBody>[] fields = [count, note];
        using HttpContent form = GeneratedRequestRunner.CreateUrlEncodedBodyContent(settings, body, fields);
        string formText = await form.ReadAsStringAsync();
        string? fieldName = count.ResolveFieldName(settings.UrlParameterKeyFormatter);

        Check.Require(formText == "form.count=012&note=" && fieldName == "form.count", "Descriptors apply naming, formats, and null serialization.");
        Check.Require(count.Getter(body) is int number && number == SampleValues.Count, "The getter reads the declared property.");
        Check.Require(count.ClrName == nameof(FormBody.Count) && count.ExplicitName == nameof(count), "The descriptor retains both names.");
        Check.Require(count.PrefixSegment == formPrefix && count.Format == "D3", "The descriptor retains prefix and format.");
        Check.Require(count.CollectionFormat is null && !count.SerializeNull, "The descriptor retains collection and null policy.");
        Check.Require(note.CollectionFormat == CollectionFormat.Csv && note.SerializeNull, "Explicit descriptor collection and null policies are retained.");
        FormField<FormBody> unaliased = new(static value => value.Count, nameof(FormBody.Count), null, formPrefix, null, null, false);
        Check.Require(unaliased.ResolveFieldName(new CamelCaseUrlParameterKeyFormatter()) == "form.count", "Unaliased fields use the supplied key formatter before adding their prefix.");
        Check.Require(GeneratedRequestRunner.CanUnrollForm(body) && !GeneratedRequestRunner.CanUnrollForm("text"), "Only plain objects enter the unrolled path.");

        using HttpContent raw = GeneratedRequestRunner.CreateBodyContent(settings, "plain text", BodySerializationMethod.Default, streamBody: false);
        using HttpContent json = GeneratedRequestRunner.CreateBodyContent(settings, SampleValues.Count, BodySerializationMethod.Serialized, streamBody: true);
        using HttpContent lines = GeneratedRequestRunner.CreateJsonLinesBodyContent(settings, SampleValues.Items);
        using HttpContent multipartPart = GeneratedRequestRunner.SerializeMultipartPart(settings, SampleValues.Count, nameof(count));

        Check.Require(await raw.ReadAsStringAsync() == "plain text", "Default strings remain raw text.");
        Check.Require(await json.ReadAsStringAsync() == "12", "The serializer writes the integer as JSON.");
        Check.Require(await lines.ReadAsStringAsync() == "1\n2", "JSON Lines separates items without a trailing newline.");
        Check.Require(await multipartPart.ReadAsStringAsync() == "12", "Multipart parts use the serializer.");
        await CheckCompressionAsync(settings);
        await CheckBodyShapesAsync(settings);
        CheckMultipartFailure(settings);

        await using MemoryStream stream = new(SampleValues.StreamBytes.ToArray());
        using (HttpContent streamContent = GeneratedRequestRunner.CreateStreamContent(stream))
        {
            Check.Require((await streamContent.ReadAsByteArrayAsync()).AsSpan().SequenceEqual(SampleValues.StreamBytes), "Caller stream bytes are preserved in order.");
        }

        Check.Require(stream.CanRead, "Disposing stream content leaves the caller's stream open.");
        using HttpContent passthrough = new StringContent("existing");
        HttpContent existing = GeneratedRequestRunner.CreateBodyContent(settings, passthrough, BodySerializationMethod.Serialized, false);
        Check.Require(ReferenceEquals(passthrough, existing), "Existing content passes through unchanged.");
        using HttpContent dictionaryForm = GeneratedRequestRunner.CreateUrlEncodedBodyContent(settings, new Dictionary<string, string> { ["name"] = "a b" });
        using HttpContent stringForm = GeneratedRequestRunner.CreateUrlEncodedBodyContent(settings, "a=b");
        Check.Require(await dictionaryForm.ReadAsStringAsync() == "name=a+b" && await stringForm.ReadAsStringAsync() == "a%3Db", "Dictionary and whole-string forms differ.");
    }

    /// <summary>Checks compressor payloads, coding headers, passthrough and unavailable algorithms.</summary>
    /// <param name="settings">The serializer and compression settings.</param>
    /// <returns>Completion after compressed bodies are decoded and compared.</returns>
    private static async Task CheckCompressionAsync(RefitSettings settings)
    {
        const string compressionText = "compress me";
        using HttpContent gzip = GeneratedRequestRunner.CompressBodyContent(new StringContent(compressionText), settings, RequestCompression.GZip, CompressionLevel.Fastest);
        await using MemoryStream gzipBytes = new(await gzip.ReadAsByteArrayAsync());
        await using GZipStream gzipDecoder = new(gzipBytes, CompressionMode.Decompress);
        using StreamReader gzipReader = new(gzipDecoder);
        Check.Require(await gzipReader.ReadToEndAsync() == compressionText && gzip.Headers.ContentEncoding.Contains("gzip"), "GZip bytes decode to the original body and carry their coding header.");
        using HttpContent brotli = GeneratedRequestRunner.CompressBodyContent(new StringContent(compressionText), settings, RequestCompression.Brotli, CompressionLevel.Fastest);
        await using MemoryStream brotliBytes = new(await brotli.ReadAsByteArrayAsync());
        await using BrotliStream brotliDecoder = new(brotliBytes, CompressionMode.Decompress);
        using StreamReader brotliReader = new(brotliDecoder);
        Check.Require(
            await brotliReader.ReadToEndAsync() == compressionText && brotli.Headers.ContentEncoding.Contains("br"),
            "Brotli bytes decode to the original body and carry their coding header.");
        using HttpContent noCompression = new StringContent("unchanged");
        settings.RequestCompression = RequestCompression.GZip;
        using HttpContent inherited = GeneratedRequestRunner.CompressBodyContent(new StringContent(compressionText), settings, RequestCompression.Default, CompressionLevel.Fastest);
        Check.Require(inherited.Headers.ContentEncoding.Contains("gzip"), "Default compression inherits the coding from settings.");
        HttpContent unchanged = GeneratedRequestRunner.CompressBodyContent(noCompression, settings, RequestCompression.None, CompressionLevel.Fastest);
        Check.Require(ReferenceEquals(noCompression, unchanged), "Explicit None returns the same content.");
        bool unsupported = false;
        try
        {
            using HttpContent zstandard = GeneratedRequestRunner.CompressBodyContent(noCompression, settings, RequestCompression.Zstandard, CompressionLevel.Fastest);
        }
        catch (PlatformNotSupportedException)
        {
            unsupported = true;
        }

        Check.Require(unsupported, "Zstandard requires .NET 11.");
    }

    /// <summary>Checks scalar JSON Lines, body modes, stream passthrough and invalid modes.</summary>
    /// <param name="settings">The serializer settings.</param>
    /// <returns>Completion after each body representation is checked.</returns>
    private static async Task CheckBodyShapesAsync(RefitSettings settings)
    {
        using HttpContent scalarLines = GeneratedRequestRunner.CreateJsonLinesBodyContent(settings, SampleValues.Count);
        Check.Require(await scalarLines.ReadAsStringAsync() == "12", "A scalar JSON Lines body produces one line without a newline.");
        using HttpContent existing = new StringContent("retained");
        Check.Require(ReferenceEquals(existing, GeneratedRequestRunner.CreateJsonLinesBodyContent(settings, existing)), "JSON Lines content passes through unchanged.");
        Check.Require(ReferenceEquals(existing, GeneratedRequestRunner.CreateUrlEncodedBodyContent(settings, existing)), "URL-encoded content passes through unchanged.");
        foreach (RequestBodySerializationMode mode in new[] { RequestBodySerializationMode.Default, RequestBodySerializationMode.Buffered, RequestBodySerializationMode.Streamed })
        {
            settings.RequestBodySerialization = mode;
            using HttpContent serialized = GeneratedRequestRunner.CreateBodyContent(settings, SampleValues.Count, BodySerializationMethod.Serialized, streamBody: true);
            Check.Require(await serialized.ReadAsStringAsync() == "12", "Every supported serialization mode preserves the JSON value.");
        }

        await using MemoryStream stream = new(SampleValues.StreamBytes.ToArray());
        using (HttpContent streamBody = GeneratedRequestRunner.CreateBodyContent(settings, stream, BodySerializationMethod.Serialized, streamBody: true))
        {
            Check.Require((await streamBody.ReadAsByteArrayAsync()).AsSpan().SequenceEqual(SampleValues.StreamBytes), "Body streams bypass JSON serialization and preserve their bytes.");
        }

        Check.Require(stream.CanRead && !GeneratedRequestRunner.CanUnrollForm(stream), "Body stream wrappers retain caller ownership and bypass unrolled forms.");
        Check.Require(!GeneratedRequestRunner.CanUnrollForm(null) && !GeneratedRequestRunner.CanUnrollForm(new Dictionary<string, string>()), "Null and dictionary bodies bypass unrolled forms.");
        bool invalidMode = false;
        try
        {
            using HttpContent invalid = GeneratedRequestRunner.CreateBodyContent(settings, SampleValues.Count, BodySerializationMethod.UrlEncoded, streamBody: false);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            invalidMode = exception.ParamName == "serializationMethod";
        }

        Check.Require(invalidMode, "URL-encoded bodies require their dedicated creation helper.");
    }

    /// <summary>Checks how multipart serialization failures retain the field name and original cause.</summary>
    /// <param name="settings">The serializer with no metadata for Version.</param>
    private static void CheckMultipartFailure(RefitSettings settings)
    {
        bool wrapped = false;
        try
        {
            using HttpContent unsupported = GeneratedRequestRunner.SerializeMultipartPart(settings, new Version(1, 0), "version");
        }
        catch (ArgumentException exception)
        {
            wrapped = exception.ParamName == "value"
                && exception.InnerException is NotSupportedException
                && exception.Message.Contains("Parameter version is of type Version", StringComparison.Ordinal);
        }

        Check.Require(wrapped, "Multipart helper errors retain the offending field name and serializer failure.");
    }
}
