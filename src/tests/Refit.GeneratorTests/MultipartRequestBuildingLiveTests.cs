// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

namespace Refit.GeneratorTests;

/// <summary>
/// Compiles, loads and invokes generated multipart request building, asserting the produced
/// <see cref="System.Net.Http.MultipartFormDataContent"/> matches the reflection request builder for every part type
/// that generates inline: same boundary, same part count, and for each part the same <c>Content-Disposition</c> name
/// and file name, <c>Content-Type</c>, and body bytes.
/// </summary>
public sealed class MultipartRequestBuildingLiveTests
{
    /// <summary>A stable score used by the serialized DTO part scenario.</summary>
    private const int ReportScore = 42;

    /// <summary>The bytes uploaded by the primary binary part scenarios.</summary>
    private static readonly byte[] SampleBytes = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    /// <summary>A second, distinct byte payload used by multi-part scenarios.</summary>
    private static readonly byte[] OtherBytes = [200, 150, 100, 50, 25];

    /// <summary>A stable identifier used by the formattable-part scenario.</summary>
    private static readonly Guid SampleId = new("6f9619ff-8b86-d011-b42d-00cf4fc964ff");

    /// <summary>A stable timestamp used by the formattable-part scenario.</summary>
    private static readonly DateTimeOffset SampleTimestamp = new(2026, 7, 4, 12, 30, 0, TimeSpan.Zero);

    /// <summary>Verifies generated stream and binary parts match the reflection builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task StreamAndBinaryPartsMatchReflection()
    {
        using var harness = LiveMultipartHarness.Create();

        await harness.AssertParityAsync("UploadStream", static () => [new MemoryStream(SampleBytes)]);
        await harness.AssertParityAsync(
            "UploadStreamPart",
            static () => [new StreamPart(new MemoryStream(SampleBytes), "upload.bin", "application/octet-stream", "custom")]);
        await harness.AssertParityAsync("UploadBytes", static () => [SampleBytes]);
        await harness.AssertParityAsync("UploadBytesPart", static () => [new ByteArrayPart(SampleBytes, "data.bin")]);
    }

    /// <summary>Verifies generated string and formattable parts match the reflection builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task TextAndFormattablePartsMatchReflection()
    {
        using var harness = LiveMultipartHarness.Create();

        await harness.AssertParityAsync("UploadString", static () => ["hello world"]);
        await harness.AssertParityAsync("UploadFormattable", static () => [SampleId, SampleTimestamp]);
        await harness.AssertParityAsync("UploadCustomBoundary", static () => [OtherBytes]);
    }

    /// <summary>Verifies generated file and enumerable parts match the reflection builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task FileAndEnumerablePartsMatchReflection()
    {
        using var harness = LiveMultipartHarness.Create();
        var first = harness.CreateTempFile(SampleBytes);
        var second = harness.CreateTempFile(OtherBytes);

        await harness.AssertParityAsync("UploadFileInfoPart", () => [new FileInfoPart(new FileInfo(first), "doc.pdf")]);
        await harness.AssertParityAsync("UploadFile", () => [new FileInfo(first)]);
        await harness.AssertParityAsync(
            "UploadFiles",
            () => [new[] { new FileInfo(first), new FileInfo(second) }, new FileInfo(first)]);
        await harness.AssertParityAsync(
            "UploadStreamParts",
            static () =>
            [
                new[]
                {
                    new StreamPart(new MemoryStream(SampleBytes), "a.bin"),
                    new StreamPart(new MemoryStream(OtherBytes), "b.bin"),
                },
            ]);
    }

    /// <summary>Verifies generated serialized parts (a bool and a concrete (non-sealed) DTO) match the reflection builder, byte for byte
    /// through the content serializer, mirroring its <c>AddSerializedMultipartItem</c> fallback.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task SerializedPartsMatchReflection()
    {
        using var harness = LiveMultipartHarness.Create();

        await harness.AssertParityAsync("UploadFlag", static () => [true]);
        await harness.AssertParityAsync(
            "UploadReport",
            () => [harness.CreateApiValue("Refit.LiveMultipart.Report", ("Title", "Q3"), ("Score", ReportScore))]);
    }

    /// <summary>Verifies a header, request property and path parameter never become multipart parts.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task HeaderPropertyAndPathParametersAreNotParts()
    {
        using var harness = LiveMultipartHarness.Create();

        // Only the StreamPart becomes a part; the path, header and property parameters must not. Content parity with
        // the reflection builder (a single part) confirms none of them leaked into the multipart body.
        await harness.AssertParityAsync(
            "UploadWithHeaderPropertyPath",
            static () => ["folder", "token-value", "trace-value", new StreamPart(new MemoryStream(SampleBytes), "x.bin")]);
    }

    /// <summary>Hosts one compiled generated client plus the reflection builder for multipart parity assertions.</summary>
    /// <param name="context">The collectible load context holding the compiled assembly.</param>
    /// <param name="handler">The capturing message handler.</param>
    /// <param name="client">The HTTP client shared by both request paths.</param>
    /// <param name="interfaceType">The compiled Refit interface type.</param>
    /// <param name="generatedApi">The generated client instance.</param>
    /// <param name="requestBuilder">The reflection request builder for the compiled interface.</param>
    private sealed class LiveMultipartHarness(
        CollectibleAssemblyLoadContext context,
        CapturingMultipartHandler handler,
        HttpClient client,
        Type interfaceType,
        object generatedApi,
        IRequestBuilder requestBuilder) : IDisposable
    {
        /// <summary>The base address the relative request URIs resolve against.</summary>
        private const string BaseAddress = "https://example.test/base/";

        /// <summary>The multipart interface compiled through the generator for every scenario.</summary>
        private const string ApiSource =
            """
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Threading.Tasks;
            using Refit;

            namespace Refit.LiveMultipart;

            // Not sealed: exercises the concrete (non-sealed) serialized part; the test value is not a subtype.
            public class Report
            {
                public string? Title { get; set; }

                public int Score { get; set; }
            }

            public interface ILiveMultipartApi
            {
                [Multipart]
                [Post("/upload")]
                Task<string> UploadFlag([AliasAs("flag")] bool flag);

                [Multipart]
                [Post("/upload")]
                Task<string> UploadReport([AliasAs("report")] Report report);

                [Multipart]
                [Post("/upload")]
                Task<string> UploadStream(Stream stream);

                [Multipart]
                [Post("/upload")]
                Task<string> UploadStreamPart(StreamPart part);

                [Multipart]
                [Post("/upload")]
                Task<string> UploadBytes(byte[] bytes);

                [Multipart]
                [Post("/upload")]
                Task<string> UploadBytesPart([AliasAs("blob")] ByteArrayPart part);

                [Multipart]
                [Post("/upload")]
                Task<string> UploadString([AliasAs("alias")] string value);

                [Multipart]
                [Post("/upload")]
                Task<string> UploadFileInfoPart(FileInfoPart part);

                [Multipart]
                [Post("/upload")]
                Task<string> UploadFile(FileInfo file);

                [Multipart]
                [Post("/upload")]
                Task<string> UploadFiles(IEnumerable<FileInfo> files, FileInfo extra);

                [Multipart]
                [Post("/upload")]
                Task<string> UploadStreamParts(IEnumerable<StreamPart> parts);

                [Multipart]
                [Post("/upload")]
                Task<string> UploadFormattable([AliasAs("id")] Guid id, [AliasAs("at")] DateTimeOffset at);

                [Multipart("----CustomBoundary")]
                [Post("/upload")]
                Task<string> UploadCustomBoundary(byte[] bytes);

                [Multipart]
                [Post("/upload/{folder}")]
                Task<string> UploadWithHeaderPropertyPath(
                    string folder,
                    [Header("X-Token")] string token,
                    [Property("Trace")] string trace,
                    [AliasAs("file")] StreamPart part);
            }
            """;

        /// <summary>The temporary files created for file-part scenarios, deleted on disposal.</summary>
        private readonly List<string> _tempFiles = [];

        /// <summary>Compiles the multipart interface and creates the generated and reflection clients.</summary>
        /// <returns>The live harness.</returns>
        [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
        public static LiveMultipartHarness Create()
        {
            var result = Fixture.RunGenerator(ApiSource, generatedRequestBuilding: true);
            if (!result.CompilesWithoutErrors)
            {
                throw new InvalidOperationException(
                    "Generated compilation failed: " + string.Join(Environment.NewLine, result.CompilationErrors));
            }

            var (assembly, loadContext) = Fixture.EmitAndLoad(result);
            var interfaceType = assembly.GetType("Refit.LiveMultipart.ILiveMultipartApi", throwOnError: true)!;
            var generatedType = assembly
                .GetTypes()
                .Single(type => type.IsClass && interfaceType.IsAssignableFrom(type));

            var handler = new CapturingMultipartHandler();
            var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };
            var requestBuilder = RequestBuilder.ForType(interfaceType, new RefitSettings());
            var generatedApi = Activator.CreateInstance(generatedType, [client, requestBuilder])!;
            return new(loadContext, handler, client, interfaceType, generatedApi, requestBuilder);
        }

        /// <summary>Creates an instance of a compiled scenario type with the given properties assigned.</summary>
        /// <param name="typeName">The compiled type's full name.</param>
        /// <param name="properties">The property name/value pairs to assign.</param>
        /// <returns>The created instance.</returns>
        [RequiresUnreferencedCode("Reflects over generated types and members.")]
        public object CreateApiValue(string typeName, params (string Name, object? Value)[] properties)
        {
            var type = interfaceType.Assembly.GetType(typeName, throwOnError: true)!;
            var instance = Activator.CreateInstance(type)!;
            foreach (var (name, value) in properties)
            {
                type.GetProperty(name)!.SetValue(instance, value);
            }

            return instance;
        }

        /// <summary>Creates a temporary file with the given bytes, tracked for cleanup on disposal.</summary>
        /// <param name="bytes">The file content.</param>
        /// <returns>The temporary file path.</returns>
        public string CreateTempFile(byte[] bytes)
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.WriteAllBytes(path, bytes);
            _tempFiles.Add(path);
            return path;
        }

        /// <summary>Invokes a method through both request paths and asserts the multipart contents are identical.</summary>
        /// <param name="methodName">The interface method name.</param>
        /// <param name="argsFactory">Produces a fresh argument set for each path, so single-use streams are not shared.</param>
        /// <returns>A task representing the asynchronous assertion.</returns>
        [RequiresUnreferencedCode("Reflects over generated types and members.")]
        [RequiresDynamicCode("Builds the reflection request delegate for parity comparison.")]
        public async Task AssertParityAsync(string methodName, Func<object[]> argsFactory)
        {
            var generatedTask = (Task)interfaceType.GetMethod(methodName)!.Invoke(generatedApi, argsFactory())!;
            await generatedTask.ConfigureAwait(false);
            var generated = handler.TakeSnapshot();

            var reflectionFunc = requestBuilder.BuildRestResultFuncForMethod(methodName);
            var reflectionTask = (Task)reflectionFunc(client, argsFactory())!;
            await reflectionTask.ConfigureAwait(false);
            var reflection = handler.TakeSnapshot();

            await Assert.That(generated.Boundary).IsEqualTo(reflection.Boundary);
            await Assert.That(generated.Parts.Count).IsEqualTo(reflection.Parts.Count);
            for (var i = 0; i < reflection.Parts.Count; i++)
            {
                var generatedPart = generated.Parts[i];
                var reflectionPart = reflection.Parts[i];
                await Assert.That(generatedPart.Name).IsEqualTo(reflectionPart.Name);
                await Assert.That(generatedPart.FileName).IsEqualTo(reflectionPart.FileName);
                await Assert.That(generatedPart.ContentType).IsEqualTo(reflectionPart.ContentType);
                await Assert.That(generatedPart.Body.SequenceEqual(reflectionPart.Body)).IsTrue();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            client.Dispose();
            handler.Dispose();
            context.Dispose();
            foreach (var path in _tempFiles)
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>Captures each outgoing multipart request, snapshotting its boundary and parts before disposal.</summary>
    private sealed class CapturingMultipartHandler : HttpMessageHandler
    {
        /// <summary>The snapshot captured for the most recent request.</summary>
        private MultipartSnapshot? _snapshot;

        /// <summary>Takes the snapshot captured for the last request, clearing the slot.</summary>
        /// <returns>The captured multipart snapshot.</returns>
        public MultipartSnapshot TakeSnapshot()
        {
            var snapshot = _snapshot ?? throw new InvalidOperationException("No multipart content was captured.");
            _snapshot = null;
            return snapshot;
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var multipart = (MultipartContent)request.Content!;
            var boundary = multipart.Headers.ContentType!.Parameters
                .Single(static parameter => parameter.Name == "boundary").Value;

            var parts = new List<CapturedPart>();
            foreach (var part in multipart)
            {
                var disposition = part.Headers.ContentDisposition;
                var body = await part.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                parts.Add(new(
                    disposition?.Name,
                    disposition?.FileName,
                    part.Headers.ContentType?.ToString(),
                    body));
            }

            _snapshot = new(boundary, parts);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("\"done\"", Encoding.UTF8, "application/json"),
            };
        }
    }

    /// <summary>The captured boundary and ordered parts of one multipart request.</summary>
    /// <param name="Boundary">The multipart boundary value from the content type header.</param>
    /// <param name="Parts">The captured parts in order.</param>
    private sealed record MultipartSnapshot(string? Boundary, List<CapturedPart> Parts);

    /// <summary>The captured metadata and body of one multipart part.</summary>
    /// <param name="Name">The <c>Content-Disposition</c> form-field name.</param>
    /// <param name="FileName">The <c>Content-Disposition</c> file name, or null.</param>
    /// <param name="ContentType">The part's <c>Content-Type</c>, or null.</param>
    /// <param name="Body">The part's body bytes.</param>
    private sealed record CapturedPart(string? Name, string? FileName, string? ContentType, byte[] Body);
}
