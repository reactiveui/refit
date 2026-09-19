// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Checks error capture limits, redaction and transport factory behavior.</summary>
internal static class ErrorPolicies
{
    /// <summary>The response text used to compare bounded and unbounded capture.</summary>
    private const string ReplyText = "private response";

    /// <summary>The bounded response prefix length.</summary>
    private const int PrefixLength = 5;

    /// <summary>The authorization scheme removed by the redaction example.</summary>
    private const string AuthorizationScheme = "Bearer";

    /// <summary>The artificial token used to verify request-header redaction.</summary>
    private const string SampleToken = "sample-token";

    /// <summary>Runs the error-policy scenarios with local messages and a failing transport.</summary>
    /// <returns>Completion after each policy is checked.</returns>
    internal static async Task RunAsync()
    {
        await CheckCaptureLimitsAsync();
        await CheckResponseRedactionAsync();
        await CheckTransportRedactionAsync();
        await ApplyTransportRedactionAsync();
    }

    /// <summary>Checks character limits, retained headers and factory inner exceptions.</summary>
    /// <returns>Completion after unbounded, empty and truncated capture are checked.</returns>
    private static async Task CheckCaptureLimitsAsync()
    {
        foreach (int? limit in new int?[] { null, 0, PrefixLength })
        {
            using HttpRequestMessage request = new(HttpMethod.Post, "https://people.example/person");
            using HttpResponseMessage response = new(HttpStatusCode.BadRequest) { ReasonPhrase = "Rejected", Content = new StringContent(ReplyText) };
            RefitSettings settings = new() { MaxExceptionContentLength = limit };
            InvalidOperationException cause = new("The service rejected the person.");
            ApiException error = await ApiException.Create(request, request.Method, response, settings, cause);

            SampleCheck.Equal(limit is { } count ? ReplyText[..count] : ReplyText, error.Content);
            SampleCheck.Equal(cause, error.InnerException);
            SampleCheck.Equal(HttpMethod.Post, error.HttpMethod);
            SampleCheck.Equal("Rejected", error.ReasonPhrase);
            SampleCheck.Equal(response.Headers, error.Headers);
            SampleCheck.Equal("text/plain", error.ContentHeaders?.ContentType?.MediaType);
        }
    }

    /// <summary>Removes captured request and reply details before returning an HTTP exception.</summary>
    /// <returns>Completion after the redacted exception is checked.</returns>
    private static async Task CheckResponseRedactionAsync()
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "https://people.example/person");
        request.Headers.Authorization = new(AuthorizationScheme, SampleToken);
        request.Options.Set(new(HttpRequestMessageOptions.RequestContent), "private request");
        using HttpResponseMessage response = new(HttpStatusCode.BadRequest) { Content = new StringContent(ReplyText) };
        RefitSettings settings = new()
        {
            ExceptionRedactor = static error =>
            {
                SampleCheck.Equal(true, error.HasRequestContent);
                error.RequestContent = null;
                error.RequestMessage.Headers.Authorization = null;
                if (error is ApiException replyError)
                {
                    replyError.Content = null;
                }
            },
        };
        ApiException redacted = await ApiException.Create("App request rejected.", request, request.Method, response, settings);

        SampleCheck.Equal("App request rejected.", redacted.Message);
        SampleCheck.Equal(null, redacted.Content);
        SampleCheck.Equal(null, redacted.RequestContent);
        SampleCheck.Equal(false, redacted.HasRequestContent);
        SampleCheck.Equal(null, redacted.RequestMessage.Headers.Authorization);
    }

    /// <summary>Checks whether default and custom transport factories apply exception redaction.</summary>
    /// <returns>Completion after both transport factory paths are checked.</returns>
    private static async Task CheckTransportRedactionAsync()
    {
        using SampleHost host = new();
        host.Client.DefaultRequestHeaders.Authorization = new(AuthorizationScheme, SampleToken);
        host.Http.Add(
            new() { Method = HttpMethod.Get, Template = "/failures/rejected", Reusable = true },
            Reply.From(static _ => Task.FromException<HttpResponseMessage>(new HttpRequestException("Connection unavailable."))));
        bool redactorCalled = false;
        Action<ApiExceptionBase> redact = error =>
        {
            redactorCalled = true;
            error.RequestMessage.Headers.Authorization = null;
        };
        RefitSettings settings = new(host.Settings.ContentSerializer) { ExceptionRedactor = redact };
        IErrorsApi api = RestService.ForGenerated<IErrorsApi>(host.Client, settings);
        using ApiResponse<Person> defaultFailure = await api.GetRejectedResponseAsync(CancellationToken.None);
        SampleCheck.Equal(true, defaultFailure.HasRequestError(out _));
        SampleCheck.Equal(false, redactorCalled);
        SampleCheck.Equal(SampleToken, defaultFailure.Error?.RequestMessage.Headers.Authorization?.Parameter);

        bool factoryCalled = false;
        RefitSettings customSettings = new(host.Settings.ContentSerializer) { ExceptionRedactor = redact };
        customSettings.TransportExceptionFactory = (request, cause, _) =>
        {
            factoryCalled = true;
            return new ApiRequestException("App transport failure.", request, request.Method, customSettings, cause);
        };
        IErrorsApi custom = RestService.ForGenerated<IErrorsApi>(host.Client, customSettings);
        using ApiResponse<Person> customFailure = await custom.GetRejectedResponseAsync(CancellationToken.None);
        SampleCheck.Equal(true, factoryCalled);
        SampleCheck.Equal("App transport failure.", customFailure.Error?.Message);
        SampleCheck.Equal(false, redactorCalled);
        SampleCheck.Equal(SampleToken, customFailure.Error?.RequestMessage.Headers.Authorization?.Parameter);
    }

    /// <summary>Applies the redaction hook explicitly inside an app's transport factory.</summary>
    /// <returns>Completion after the retained request header is removed.</returns>
    private static async Task ApplyTransportRedactionAsync()
    {
        using SampleHost host = new();
        host.Client.DefaultRequestHeaders.Authorization = new(AuthorizationScheme, SampleToken);
        host.Http.Add(
            Route.Get("/failures/rejected"),
            Reply.From(static _ => Task.FromException<HttpResponseMessage>(new HttpRequestException("Connection unavailable."))));
        RefitSettings settings = new(host.Settings.ContentSerializer) { ExceptionRedactor = static error => error.RequestMessage.Headers.Authorization = null };
        settings.TransportExceptionFactory = (request, cause, token) =>
        {
            if (cause is OperationCanceledException && token.IsCancellationRequested)
            {
                return cause;
            }

            ApiRequestException error = new(request, request.Method, settings, cause);
            settings.ExceptionRedactor?.Invoke(error);
            return error;
        };
        IErrorsApi api = RestService.ForGenerated<IErrorsApi>(host.Client, settings);
        using ApiResponse<Person> failure = await api.GetRejectedResponseAsync(CancellationToken.None);
        SampleCheck.Equal(true, failure.HasRequestError(out _));
        SampleCheck.Equal(null, failure.Error?.RequestMessage.Headers.Authorization);
    }
}
