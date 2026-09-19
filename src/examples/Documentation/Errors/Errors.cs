// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Text.Json;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Verifies exception factories, buffered error parsing and known AOT discrepancies.</summary>
internal static class Errors
{
    /// <summary>The rejected reply's structured error body.</summary>
    private const string RejectedJson = """{"code":"name_taken","message":"Pick another name."}""";

    /// <summary>A problem response containing validation and extension data.</summary>
    private const string ProblemJson = """
        {
          "type": "about:blank",
          "title": "Invalid name",
          "status": 400,
          "detail": "Choose a name.",
          "instance": "/people",
          "errors": { "name": ["Name is required."] },
          "trace": "sample"
        }
        """;

    /// <summary>Reuses generated readers for the registered reply and error models.</summary>
    private static readonly JsonSerializerOptions Options = new(ErrorJsonContext.Default.Options) { TypeInfoResolver = ErrorJsonContext.Default };

    /// <summary>Uses generated JSON metadata for every error-body conversion.</summary>
    private static readonly RefitSettings Settings = new(new SystemTextJsonContentSerializer(Options));

    /// <summary>Runs error-body, factory and discrepancy reproductions.</summary>
    /// <param name="host">The shared local HTTP client and route table.</param>
    /// <returns>Completion after all error checks.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        ExceptionConstructors.Run(Settings);
        IErrorsApi api = CreateClient(host);
        await ReadRejectedAsync(api);
        await CheckProblemMetadataAsync();
        await ReadProblemAsync(api);
        await CreateExceptionsAsync();
        await ErrorPolicies.RunAsync();
        await ShowCustomFactoryDiscrepancyAsync(host);
        await ShowCustomDeserializationDiscrepancyAsync(host);
    }

    /// <summary>Adds reusable failure routes and resolves their generated client.</summary>
    /// <param name="host">The shared local HTTP client and route table.</param>
    /// <returns>A client configured with generated error-model metadata.</returns>
    internal static IErrorsApi CreateClient(SampleHost host)
    {
        host.Http.Add(new() { Method = HttpMethod.Get, Template = "/failures/rejected", Reusable = true }, Reply.Json(RejectedJson, HttpStatusCode.BadRequest));
        host.Http.Add(new() { Method = HttpMethod.Get, Template = "/failures/malformed", Reusable = true }, Reply.Json("not JSON"));
        host.Http.Add(new() { Method = HttpMethod.Get, Template = "/failures/problem", Reusable = true }, Reply.From(static request => new(HttpStatusCode.BadRequest)
        {
            RequestMessage = request,
            Content = new StringContent(ProblemJson, System.Text.Encoding.UTF8, "application/problem+json"),
        }));
        return RestService.ForGenerated<IErrorsApi>(host.Client, Settings);
    }

    /// <summary>Reads the structured body retained by an HTTP error.</summary>
    /// <param name="api">The generated client for local failure routes.</param>
    /// <returns>Completion after the rejected call is handled.</returns>
    /// <exception cref="InvalidOperationException">The local rejected request unexpectedly succeeds.</exception>
    private static async Task ReadRejectedAsync(IErrorsApi api)
    {
        try
        {
            _ = await api.GetRejectedAsync(CancellationToken.None);
            throw new InvalidOperationException("The rejected call should throw.");
        }
        catch (ApiException error)
        {
            Console.WriteLine(error.StatusCode); // BadRequest
            Failure? failure = await error.GetContentAsAsync<Failure>();
            Console.WriteLine(failure?.Code); // name_taken
        }
    }

    /// <summary>Compares both synchronous readers with the asynchronous result.</summary>
    /// <param name="error">The buffered HTTP exception.</param>
    /// <param name="expected">The result from the asynchronous reader.</param>
    private static void ReadSynchronously(ApiException error, Failure? expected)
    {
        Failure? synchronous = error.GetContentAs<Failure>();
        bool read = error.TryGetContentAs(out Failure? attempted);
        Console.WriteLine(attempted?.Code); // name_taken

        SampleCheck.Equal(expected, synchronous);
        SampleCheck.Equal(expected, attempted);
        SampleCheck.Equal(true, read);
    }

    /// <summary>Handles a problem response through the public manual parser.</summary>
    /// <param name="api">The generated client for the problem route.</param>
    /// <returns>Completion after the problem response is checked.</returns>
    /// <exception cref="InvalidOperationException">The local problem request unexpectedly succeeds.</exception>
    private static async Task ReadProblemAsync(IErrorsApi api)
    {
        try
        {
            _ = await api.GetProblemAsync(CancellationToken.None);
            throw new InvalidOperationException("The problem reply should throw.");
        }
        catch (ApiException error)
        {
            ReadProblem(error);
        }
    }

    /// <summary>Checks validation entries and extension data without JSON reflection.</summary>
    /// <param name="error">The HTTP exception retaining the problem body.</param>
    private static void ReadProblem(ApiException error)
    {
        ValidationApiException validation = error as ValidationApiException ?? ValidationApiException.Create(error);
        ProblemDetails? problem = validation.Content;
        Console.WriteLine(problem?.Title); // Invalid name
        Console.WriteLine(problem?.Errors["name"][0]); // Name is required.
        Console.WriteLine(error.Content); // Raw JSON

        SampleCheck.Equal((int)HttpStatusCode.BadRequest, problem?.Status);
        SampleCheck.Equal("sample", problem?.Extensions["trace"].ToString());
    }

    /// <summary>Reproduces the generated metadata failure for the extension-data property.</summary>
    /// <returns>Completion after the exact serializer exception is verified.</returns>
    /// <exception cref="InvalidOperationException">The expected metadata failure does not occur.</exception>
    private static async Task CheckProblemMetadataAsync()
    {
        using HttpContent content = new StringContent(ProblemJson);
        try
        {
            _ = await Settings.ContentSerializer.FromHttpContentAsync<ProblemDetails>(content);
            throw new InvalidOperationException("Generated ProblemDetails metadata should reproduce the failure.");
        }
        catch (InvalidOperationException error)
        {
            Console.WriteLine(error.Message);
            SampleCheck.Equal("The extension data property 'Extensions' on type 'Refit.ProblemDetails' cannot bind with a parameter in the deserialization constructor.", error.Message);
        }
    }

    /// <summary>Checks public exception constructors and HTTP/validation factories.</summary>
    /// <returns>Completion after the created exceptions are verified.</returns>
    private static async Task CreateExceptionsAsync()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "https://people.example/person");
        using HttpResponseMessage response = new(HttpStatusCode.BadRequest) { RequestMessage = request, Content = new StringContent(RejectedJson) };
        ApiException error = await ApiException.Create(request, request.Method, response, Settings);
        Console.WriteLine(error.HasContent); // True
        Console.WriteLine(error.Uri);
        DefaultApiExceptionFactory factory = new(Settings);
        using HttpResponseMessage success = new(HttpStatusCode.OK) { RequestMessage = request };
        Exception? noError = await factory.CreateAsync(success);
        Console.WriteLine(noError is null); // True

        SampleCheck.Equal(null, noError);
        ReadSynchronously(error, await error.GetContentAsAsync<Failure>());

        using HttpResponseMessage unreadable = new(HttpStatusCode.OK) { RequestMessage = request, Content = new StringContent("not JSON") };
        JsonException cause = new("Invalid person JSON.");
        ApiException readError = await ApiException.Create("Could not read the person.", request, request.Method, unreadable, Settings, cause);

        SampleCheck.Equal(HttpStatusCode.OK, readError.StatusCode);
        SampleCheck.Equal(cause, readError.InnerException);
        error.Content = """{"title":"Invalid name","status":400,"errors":{"name":["Name is required."]},"trace":"sample"}""";
        ValidationApiException validation = ValidationApiException.Create(error);
        ValidationApiException withMessage = new("Invalid input.");
        ValidationApiException withCause = new("Invalid input.", cause);
        ProblemDetails local = new()
        {
            Type = "about:blank",
            Title = "Invalid name",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = "Choose a name.",
            Instance = "/people",
            Errors = new() { ["name"] = ["Name is required."] },
            Extensions = new Dictionary<string, object> { ["trace"] = "sample" },
        };

        SampleCheck.Equal(local.Title, validation.Content?.Title);
        SampleCheck.Equal(withMessage.Message, withCause.Message);
    }

    /// <summary>Reproduces a custom exception being discarded by a response wrapper.</summary>
    /// <param name="host">The shared HTTP client with reusable failure routes.</param>
    /// <returns>Completion after the missing error is verified.</returns>
    private static async Task ShowCustomFactoryDiscrepancyAsync(SampleHost host)
    {
        RefitSettings customSettings = new(new SystemTextJsonContentSerializer(Options))
        {
            ExceptionFactory = static _ => ValueTask.FromResult<Exception?>(new InvalidOperationException("App-specific failure.")),
        };
        IErrorsApi custom = RestService.ForGenerated<IErrorsApi>(host.Client, customSettings);
        using ApiResponse<Person> wrapper = await custom.GetRejectedResponseAsync(CancellationToken.None);
        Console.WriteLine(wrapper.Error is null); // True: the custom exception is lost.
        SampleCheck.Equal(false, wrapper.IsSuccessStatusCode);
        SampleCheck.Equal(null, wrapper.Error);
    }

    /// <summary>Checks a successful HTTP status whose custom JSON error is discarded.</summary>
    /// <param name="host">The shared HTTP client with the malformed JSON route.</param>
    /// <returns>Completion after the wrapper and its success guard are checked.</returns>
    private static async Task ShowCustomDeserializationDiscrepancyAsync(SampleHost host)
    {
        bool factoryCalled = false;
        RefitSettings customSettings = new(new SystemTextJsonContentSerializer(Options))
        {
            DeserializationExceptionFactory = (_, error) =>
            {
                factoryCalled = true;
                return ValueTask.FromResult<Exception?>(new InvalidOperationException("Could not read the reply.", error));
            },
        };
        IErrorsApi custom = RestService.ForGenerated<IErrorsApi>(host.Client, customSettings);
        using ApiResponse<Person> wrapper = await custom.GetMalformedAsync(CancellationToken.None);
        await wrapper.EnsureSuccessfulAsync();

        SampleCheck.Equal(true, factoryCalled);
        SampleCheck.Equal(true, wrapper.IsSuccessStatusCode);
        SampleCheck.Equal(true, wrapper.IsSuccessful);
        SampleCheck.Equal(null, wrapper.Content);
        SampleCheck.Equal(null, wrapper.Error);
    }
}
