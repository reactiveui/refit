// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.IO;
using System.Text.Json;

namespace Refit.Documentation;

/// <summary>Checks multipart wrappers, generated requests, extension points and stream ownership.</summary>
internal static class Multipart
{
    /// <summary>Names the text media type reused in the checks.</summary>
    private const string TextMediaType = "text/plain";

    /// <summary>Provides the expected stream body.</summary>
    private const string StreamText = "stream text";

    /// <summary>Provides the expected byte-array body.</summary>
    private const string ByteText = "byte text";

    /// <summary>Provides the transmitted byte-array file name.</summary>
    private const string BytesFileName = "bytes.txt";

    /// <summary>Provides the shared form and model title.</summary>
    private const string ReportTitle = "Annual report";

    /// <summary>Provides the custom request boundary.</summary>
    private const string Boundary = "sample-boundary";

    /// <summary>Provides the custom extension's text body.</summary>
    private const string Greeting = "hello";

    /// <summary>Identifies the second repeated attachment's byte value.</summary>
    private const byte SecondByte = 2;

    /// <summary>Provides pre-encoded stream sample bytes without repeated UTF-8 conversion.</summary>
    private static readonly byte[] StreamBytes = "stream text"u8.ToArray();

    /// <summary>Provides pre-encoded byte-part sample data.</summary>
    private static readonly byte[] ByteBytes = "byte text"u8.ToArray();

    /// <summary>Supplies generated JSON metadata for the model part.</summary>
    private static readonly JsonSerializerOptions Options = new(MultipartJsonContext.Default.Options) { TypeInfoResolver = MultipartJsonContext.Default };

    /// <summary>Uses generated model metadata for multipart serialization.</summary>
    private static readonly RefitSettings Settings = new(new SystemTextJsonContentSerializer(Options));

    /// <summary>Runs local checks using a file beneath the executable's output directory.</summary>
    /// <returns>A task that completes after every assertion passes.</returns>
    internal static async Task RunAsync()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "sample-files");
        _ = Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, $"{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(path, "disk text");
        try
        {
            await ShowPartContentAsync(new(path));
            using MultipartHandler handler = new();
            using HttpClient client = CreateClient(handler);
            IMultipartApi api = RestService.ForGenerated<IMultipartApi>(client, Settings);
            await ShowUploadAsync(api, handler);
            await ShowFilesAsync(api, handler, new(path));
            await ShowRawAsync(api, handler, new(path));
            await ShowMetadataAsync(api, handler);
            await ShowCustomAsync(api);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Creates one local client reused across the upload scenarios without owning its handler.</summary>
    /// <param name="handler">The independently owned local multipart handler.</param>
    /// <returns>The client, which the caller must dispose.</returns>
    private static HttpClient CreateClient(MultipartHandler handler) => new(handler, disposeHandler: false) { BaseAddress = new("https://uploads.example") };

    /// <summary>Checks direct wrapper conversion separately from request construction.</summary>
    /// <param name="file">The sample file opened by FileInfoPart.</param>
    /// <returns>A task that completes after content and ownership assertions pass.</returns>
    private static async Task ShowPartContentAsync(FileInfo file)
    {
        await using MemoryStream stream = new(StreamBytes);
        StreamPart streaming = new(stream, "source.txt");
        using (HttpContent content = streaming.ToContent())
        {
            SampleCheck.Equal(StreamText, await content.ReadAsStringAsync());
            SampleCheck.Equal(null, content.Headers.ContentDisposition);
        }

        SampleCheck.Equal(true, stream.CanRead);
        SampleCheck.Equal(stream.Length, stream.Position);
        SampleCheck.Equal(stream, streaming.Value);
        SampleCheck.Equal("source.txt", streaming.FileName);
        SampleCheck.Equal(null, streaming.Name);
        SampleCheck.Equal(null, streaming.ContentType);
        using (HttpContent consumed = streaming.ToContent())
        {
            SampleCheck.Equal(string.Empty, await consumed.ReadAsStringAsync());
        }

        stream.Position = 1;
        using (HttpContent remaining = streaming.ToContent())
        {
            SampleCheck.Equal(StreamText[1..], await remaining.ReadAsStringAsync());
        }

        SampleCheck.Equal(true, stream.CanRead);
        byte[] value = ByteBytes;
        ByteArrayPart bytes = new(value, BytesFileName, TextMediaType, "attachment");
        SampleCheck.Equal(value, bytes.Value);
        SampleCheck.Equal("attachment", bytes.Name);
        SampleCheck.Equal(TextMediaType, bytes.ContentType);
        using HttpContent byteContent = bytes.ToContent();
        SampleCheck.Equal(ByteText, await byteContent.ReadAsStringAsync());
        SampleCheck.Equal(TextMediaType, byteContent.Headers.ContentType?.MediaType);
        FileInfoPart disk = new(file, "public-name.txt");
        SampleCheck.Equal(file, disk.Value);
        using (HttpContent diskContent = disk.ToContent())
        {
            SampleCheck.Equal("disk text", await diskContent.ReadAsStringAsync());
            SampleCheck.Equal(null, diskContent.Headers.ContentType);
        }

        await using FileStream exclusive = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
        SampleCheck.Equal(true, exclusive.CanRead);
    }

    /// <summary>Checks field-name precedence, query separation and caller stream ownership.</summary>
    /// <param name="api">The client whose multipart requests are generated inline.</param>
    /// <param name="handler">The local snapshot handler.</param>
    /// <returns>A task that completes after upload assertions pass.</returns>
    private static async Task ShowUploadAsync(IMultipartApi api, MultipartHandler handler)
    {
        await using MemoryStream stream = new(StreamBytes);
        StreamPart file = new(stream, "report.txt", TextMediaType, "chosen-field");
        using HttpResponseMessage reply = await api.UploadAsync(file, ReportTitle, "preview");
        SampleCheck.Equal(Boundary, handler.Boundary);
        SampleCheck.Equal("?mode=preview", handler.Query);
        SampleCheck.Equal("chosen-field", handler.Parts[0].Name);
        SampleCheck.Equal("report.txt", handler.Parts[0].FileName);
        SampleCheck.Equal(StreamText, handler.Parts[0].Body);
        SampleCheck.Equal("title", handler.Parts[1].Name);
        SampleCheck.Equal(ReportTitle, handler.Parts[1].Body);
        SampleCheck.Equal(true, stream.CanRead);
    }

    /// <summary>Checks wrappers, the default boundary and repeated collection entries.</summary>
    /// <param name="api">The client whose multipart requests are generated inline.</param>
    /// <param name="handler">The local snapshot handler.</param>
    /// <param name="file">The sample file whose transmitted name is overridden.</param>
    /// <returns>A task that completes after file-part assertions pass.</returns>
    private static async Task ShowFilesAsync(IMultipartApi api, MultipartHandler handler, FileInfo file)
    {
        ByteArrayPart bytes = new(ByteBytes, BytesFileName, TextMediaType);
        FileInfoPart disk = new(file, "download-name.txt", TextMediaType, "document");
        using HttpResponseMessage reply = await api.UploadFilesAsync(bytes, disk, [new([1], "one.bin"), new([SecondByte], "two.bin")]);
        SampleCheck.Equal(new MultipartAttribute().BoundaryText, handler.Boundary);
        SampleCheck.Equal("bytes", handler.Parts[0].Name);
        SampleCheck.Equal(BytesFileName, handler.Parts[0].FileName);
        SampleCheck.Equal("document", handler.Parts[1].Name);
        SampleCheck.Equal("download-name.txt", handler.Parts[1].FileName);
        SampleCheck.Equal("attachments", handler.Parts[2].Name);
        SampleCheck.Equal("attachments", handler.Parts[3].Name);
        SampleCheck.Equal(Boundary, new MultipartAttribute(Boundary).BoundaryText);
    }

    /// <summary>Checks existing dispositions and default names of raw file-like values.</summary>
    /// <param name="api">The client whose multipart requests are generated inline.</param>
    /// <param name="handler">The local snapshot handler.</param>
    /// <param name="file">The sample file whose filesystem name is transmitted.</param>
    /// <returns>A task that completes after raw-part assertions pass.</returns>
    private static async Task ShowRawAsync(IMultipartApi api, MultipartHandler handler, FileInfo file)
    {
        using StringContent content = new("custom body");
        content.Headers.ContentDisposition = new("form-data") { Name = "custom-field" };
        await using MemoryStream stream = new("raw stream"u8.ToArray());
        using HttpResponseMessage reply = await api.UploadRawAsync(content, stream, "raw bytes"u8.ToArray(), file);
        SampleCheck.Equal("custom-field", handler.Parts[0].Name);
        SampleCheck.Equal("raw", handler.Parts[1].FileName);
        SampleCheck.Equal("bytes", handler.Parts[2].FileName);
        SampleCheck.Equal(file.Name, handler.Parts[3].FileName);
        SampleCheck.Equal(true, stream.CanRead);
    }

    /// <summary>Checks one JSON model part alongside an unquoted Guid text part.</summary>
    /// <param name="api">The client whose multipart requests are generated inline.</param>
    /// <param name="handler">The local snapshot handler.</param>
    /// <returns>A task that completes after model assertions pass.</returns>
    private static async Task ShowMetadataAsync(IMultipartApi api, MultipartHandler handler)
    {
        using HttpResponseMessage reply = await api.UploadMetadataAsync(new(ReportTitle), Guid.Empty);
        SampleCheck.Equal("metadata", handler.Parts[0].Name);
        SampleCheck.Equal("application/json", handler.Parts[0].MediaType);
        SampleCheck.Equal("{\"title\":\"Annual report\"}", handler.Parts[0].Body);
        SampleCheck.Equal(Guid.Empty.ToString(), handler.Parts[1].Body);
        SampleCheck.Equal(TextMediaType, handler.Parts[1].MediaType);
    }

    /// <summary>Checks both protected base constructors and overridden fresh-content creation.</summary>
    /// <param name="api">The generated client used to build unsent requests.</param>
    /// <returns>A task that completes after metadata and fallback assertions pass.</returns>
    private static async Task ShowCustomAsync(IMultipartApi api)
    {
        TextPart simple = new(Greeting, "hello.txt");
        using HttpContent standalone = simple.ToContent();
        SampleCheck.Equal(TextMediaType, standalone.Headers.ContentType?.MediaType);
        TextPart named = new(Greeting, string.Empty, "application/x-sample", "chosen");
        using HttpRequestMessage request = await api.BuildAsync(named);
        using IEnumerator<HttpContent> parts = ((MultipartFormDataContent)request.Content!).GetEnumerator();
        SampleCheck.Equal(true, parts.MoveNext());
        HttpContent part = parts.Current;
        SampleCheck.Equal(false, parts.MoveNext());
        SampleCheck.Equal("chosen", part.Headers.ContentDisposition?.Name?.Trim('"'));
        SampleCheck.Equal("aliased", part.Headers.ContentDisposition?.FileName?.Trim('"'));
        SampleCheck.Equal("application/x-sample", part.Headers.ContentType?.MediaType);
        SampleCheck.Equal(Greeting, await part.ReadAsStringAsync());
    }
}
