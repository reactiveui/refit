// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;

namespace Refit.Documentation;

/// <summary>Verifies response construction and concrete versus interface guard behavior.</summary>
internal static class Responses
{
    /// <summary>Runs success, content-failure and transport-failure checks.</summary>
    /// <param name="host">The shared local HTTP client and settings.</param>
    /// <returns>Completion after every response check.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        await ShowSuccessAsync(host.Settings);
        CheckMetadataAndOwnership(host.Settings);
        await ShowInvalidJsonAsync(host);
        await ShowTransportFailureAsync(host.Settings);
    }

    /// <summary>Checks successful construction and all interface guard forms.</summary>
    /// <param name="settings">Settings retained by the wrapper.</param>
    /// <returns>Completion after the guards accept the response.</returns>
    private static async Task ShowSuccessAsync(RefitSettings settings)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "https://people.example/person");
        using HttpResponseMessage message = new(HttpStatusCode.OK) { RequestMessage = request };
        using ApiResponse<Person> response = new(message, new(1, "Ada"), settings);
        using ApiResponse<Person> explicitError = new(message, new(1, "Ada"), settings, error: null);
        Console.WriteLine(response.IsReceived); // True
        Console.WriteLine(response.StatusCode); // OK
        Console.WriteLine(response.RequestMessage.RequestUri);
        Console.WriteLine(response.Settings == settings); // True
        if (response.IsSuccessfulWithContent)
        {
            Console.WriteLine(response.Content.Name); // Ada
        }

        SampleCheck.Equal(true, response.IsSuccessStatusCode);
        SampleCheck.Equal(true, response.HasContent);
        SampleCheck.Equal(null, response.Error);

        IApiResponse<Person> typed = response;
        IApiResponse untyped = response;
        _ = await response.EnsureSuccessStatusCodeAsync();
        _ = await response.EnsureSuccessfulAsync();
        _ = await typed.EnsureSuccessStatusCodeAsync();
        _ = await typed.EnsureSuccessfulAsync();
        _ = await untyped.EnsureSuccessStatusCodeAsync();
        _ = await untyped.EnsureSuccessfulAsync();

        SampleCheck.Equal(response, await typed.EnsureSuccessfulAsync());
    }

    /// <summary>Checks response metadata through both interfaces and disposal of owned content.</summary>
    /// <param name="settings">The settings retained by the response.</param>
    private static void CheckMetadataAndOwnership(RefitSettings settings)
    {
        using MemoryStream requestStream = new();
        using MemoryStream responseStream = new();
        using HttpRequestMessage request = new(HttpMethod.Post, "https://people.example/metadata") { Content = new StreamContent(requestStream) };
        using HttpResponseMessage message = new(HttpStatusCode.OK)
        {
            RequestMessage = request,
            Content = new StreamContent(responseStream),
            ReasonPhrase = "Accepted",
            Version = HttpVersion.Version20,
        };
        ApiResponse<Person> response = new(message, new(1, "Ada"), settings);
        SampleCheck.Equal(message.Headers, response.Headers);
        SampleCheck.Equal(message.Content.Headers, response.ContentHeaders);
        SampleCheck.Equal("Accepted", response.ReasonPhrase);
        SampleCheck.Equal(HttpVersion.Version20, response.Version);
        CheckInterfaceMetadata(response, message);

        response.Dispose();
        response.Dispose();
        SampleCheck.Equal(false, responseStream.CanRead);
        SampleCheck.Equal(true, requestStream.CanRead);
        using ApiResponse<int> absentValue = new(request, response: null, content: default, settings);
        SampleCheck.Equal(0, absentValue.Content);
        SampleCheck.Equal(true, absentValue.HasContent);
        SampleCheck.Equal(false, absentValue.IsSuccessfulWithContent);
    }

    /// <summary>Checks metadata through the shared response interface contract.</summary>
    /// <typeparam name="TResponse">The concrete response implementation.</typeparam>
    /// <param name="response">The implementation being read through its interface constraint.</param>
    /// <param name="message">The expected HTTP metadata.</param>
    private static void CheckInterfaceMetadata<TResponse>(TResponse response, HttpResponseMessage message)
        where TResponse : IApiResponse<Person>
    {
        SampleCheck.Equal(message.Headers, response.Headers);
        SampleCheck.Equal(message.Content.Headers, response.ContentHeaders);
        SampleCheck.Equal(message.ReasonPhrase, response.ReasonPhrase);
        SampleCheck.Equal(message.Version, response.Version);
        SampleCheck.Equal(message.RequestMessage, response.RequestMessage);
        SampleCheck.Equal(HttpStatusCode.OK, response.StatusCode);
        SampleCheck.Equal(true, response.IsReceived);
        SampleCheck.Equal(true, response.IsSuccessStatusCode);
        SampleCheck.Equal(true, response.IsSuccessful);
        SampleCheck.Equal(true, response.HasContent);
        SampleCheck.Equal(null, response.Error);
        SampleCheck.Equal(false, response.HasRequestError(out _));
        SampleCheck.Equal(false, response.HasResponseError(out _));
    }

    /// <summary>Checks that an HTTP success can still contain a deserialization error.</summary>
    /// <param name="host">The shared local HTTP client and route table.</param>
    /// <returns>Completion after verifying both success guards.</returns>
    /// <exception cref="InvalidOperationException">The content guard accepts malformed JSON.</exception>
    private static async Task ShowInvalidJsonAsync(SampleHost host)
    {
        IErrorsApi api = Errors.CreateClient(host);

        using ApiResponse<Person> malformed = await api.GetMalformedAsync(CancellationToken.None);
        Console.WriteLine(malformed.IsReceived); // True
        Console.WriteLine(malformed.IsSuccessStatusCode); // True
        Console.WriteLine(malformed.IsSuccessful); // False
        Console.WriteLine(malformed.HasContent); // False
        _ = await malformed.EnsureSuccessStatusCodeAsync();
        if (malformed.HasResponseError(out ApiException? readError))
        {
            Console.WriteLine(readError.InnerException?.GetType().Name); // JsonException
        }

        SampleCheck.Equal(false, malformed.IsSuccessful);
        SampleCheck.Equal(true, malformed.HasResponseError(out _));
        try
        {
            _ = await malformed.EnsureSuccessfulAsync();
            throw new InvalidOperationException("The content guard should throw.");
        }
        catch (ApiException error)
        {
            SampleCheck.Equal(malformed.Error, error);
        }
    }

    /// <summary>Reproduces guard disagreement when no HTTP response exists.</summary>
    /// <param name="settings">Settings retained by the transport exception.</param>
    /// <returns>Completion after checking each concrete and interface exception path.</returns>
    /// <exception cref="InvalidOperationException">An expected guard exception does not occur.</exception>
    private static async Task ShowTransportFailureAsync(RefitSettings settings)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "https://people.example/person");
        HttpRequestException cause = new("Connection unavailable.");
        ApiRequestException fromCause = new(request, request.Method, settings, cause);
        ApiRequestException withMessage = new("Could not contact people API.", request, request.Method, settings);
        ApiRequestException withBoth = new("Could not contact people API.", request, request.Method, settings, cause);
        using ApiResponse<Person> missing = new(request, response: null, content: null, settings, fromCause);
        Console.WriteLine(missing.IsReceived); // False
        Console.WriteLine(missing.StatusCode is null); // True
        if (missing.HasRequestError(out ApiRequestException? sendError))
        {
            Console.WriteLine(sendError.Message); // Connection unavailable.
        }

        SampleCheck.Equal(withMessage.Message, withBoth.Message);
        SampleCheck.Equal(cause, withBoth.InnerException);
        SampleCheck.Equal(false, missing.HasResponseError(out _));

        IApiResponse<Person> typed = missing;
        try
        {
            _ = await typed.EnsureSuccessfulAsync();
            throw new InvalidOperationException("The interface guard should throw.");
        }
        catch (ApiRequestException error)
        {
            SampleCheck.Equal(fromCause, error);
        }

        try
        {
            _ = await missing.EnsureSuccessfulAsync();
            throw new InvalidOperationException("The concrete guard should throw.");
        }
        catch (InvalidOperationException error)
        {
            SampleCheck.Equal("The response is unavailable for this API response.", error.Message);
        }

        IApiResponse untyped = missing;
        try
        {
            _ = await untyped.EnsureSuccessStatusCodeAsync();
            throw new InvalidOperationException("The status guard should throw.");
        }
        catch (ApiRequestException error)
        {
            SampleCheck.Equal(fromCause, error);
        }
    }
}
