// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Reflection.Benchmarks;

/// <summary>
/// EventPipe GC-verbose allocation profile of body and multipart payload assembly: setting a serialized JSON body,
/// serializing a body by runtime type, and adding text, byte-array and stream multipart parts.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class ReflectionPayloadBenchmarks
{
    /// <summary>The multipart parameter index of the text part.</summary>
    private const int TitlePartIndex = 0;

    /// <summary>The multipart parameter index of the byte-array part.</summary>
    private const int PayloadPartIndex = 1;

    /// <summary>The multipart parameter index of the stream part.</summary>
    private const int StreamPartIndex = 2;

    /// <summary>The sample byte-array multipart payload.</summary>
    private static readonly byte[] _payloadBytes = [1, 2, 3, 4, 5, 6, 7, 8];

    /// <summary>The sample bytes backing the stream multipart part.</summary>
    private static readonly byte[] _streamBytes = [9, 8, 7, 6, 5, 4, 3, 2, 1];

    /// <summary>The request builder under test.</summary>
    private RequestBuilderImplementation _builder = null!;

    /// <summary>The parsed JSON-body method.</summary>
    private RestMethodInfoInternal _bodyMethod = null!;

    /// <summary>The parsed multipart method.</summary>
    private RestMethodInfoInternal _multipartMethod = null!;

    /// <summary>The content serializer used to serialize the body.</summary>
    private IHttpContentSerializer _serializer = null!;

    /// <summary>The boxed JSON-body value.</summary>
    private object _userValue = null!;

    /// <summary>The boxed multipart text value.</summary>
    private object _titleValue = null!;

    /// <summary>The boxed multipart byte-array value.</summary>
    private object _payloadValue = null!;

    /// <summary>The boxed multipart stream value.</summary>
    private object _streamValue = null!;

    /// <summary>Prepares the builder, parsed methods, serializer and payload values before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var settings = ReflectionBenchmarkFixtures.CreateSettings();
        _builder = new(typeof(IReflectionRequestService), settings);
        _serializer = settings.ContentSerializer;
        _bodyMethod = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.CreateUserAsync), settings);
        _multipartMethod = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.UploadAsync), settings);

        _userValue = new ReflectionUserModel { Id = ReflectionBenchmarkFixtures.SampleBodyUserId, Name = "octocat", Email = "octo@example.test" };
        _titleValue = "quarterly report";
        _payloadValue = _payloadBytes;
        _streamValue = new MemoryStream(_streamBytes);
    }

    /// <summary>Sets a serialized JSON body on a request.</summary>
    /// <returns>The serialized body content.</returns>
    [Benchmark]
    public HttpContent? AddBodyToRequest()
    {
        var request = new HttpRequestMessage { Method = HttpMethod.Post };
        _builder.AddBodyToRequest(_bodyMethod, _userValue, request);
        return request.Content;
    }

    /// <summary>Serializes a body by its runtime type.</summary>
    /// <returns>The serialized body content.</returns>
    [Benchmark]
    public HttpContent SerializeBody() =>
        RequestBuilderImplementation.SerializeBody(_serializer, _userValue, typeof(ReflectionUserModel));

    /// <summary>Adds a text multipart part.</summary>
    /// <returns>The multipart content.</returns>
    [Benchmark]
    public MultipartFormDataContent AddMultiPartText()
    {
        var content = new MultipartFormDataContent();
        _builder.AddMultiPart(_multipartMethod, TitlePartIndex, _titleValue, content);
        return content;
    }

    /// <summary>Adds a byte-array multipart part.</summary>
    /// <returns>The multipart content.</returns>
    [Benchmark]
    public MultipartFormDataContent AddMultiPartBytes()
    {
        var content = new MultipartFormDataContent();
        _builder.AddMultiPart(_multipartMethod, PayloadPartIndex, _payloadValue, content);
        return content;
    }

    /// <summary>Adds a stream multipart part.</summary>
    /// <returns>The multipart content.</returns>
    [Benchmark]
    public MultipartFormDataContent AddMultiPartStream()
    {
        var content = new MultipartFormDataContent();
        _builder.AddMultiPart(_multipartMethod, StreamPartIndex, _streamValue, content);
        return content;
    }
}
