using System.Net.Http;
using System.Text;

namespace Refit;

// TODO: evaluate use of methodinlining, prolly a good idea for small single line methods
// TODO: when I move this to emit will I need to ensure all parameter names are unique?
public static class RefitHelper
{
    public static Uri BaseUri = new ("http://api");

    public static global::System.Net.Http.HttpMethod Patch = new ("PATCH");

    public static void AddUrlFragment<T>(ref ValueStringBuilder vsb, T value, global::Refit.RefitSettings settings,
        global::System.Type type)
    {
        // TODO: implement this properly
        vsb.Append(value.ToString());
    }

    public static void AddRoundTripUrlFragment(ref ValueStringBuilder vsb, global::Refit.RefitSettings settings,
        global::System.Type type) => throw new NotImplementedException(nameof(AddRoundTripUrlFragment));

    public static void AddPropertyFragment(ref ValueStringBuilder vsb, global::Refit.RefitSettings settings,
        global::System.Type type) => throw new NotImplementedException(nameof(AddPropertyFragment));

    public static void AddQueryObject(ref ValueStringBuilder vsb, global::Refit.RefitSettings settings,
        string key, object value)
    {
        vsb.Append(key);
        vsb.Append('=');
        vsb.Append(value.ToString());
    }

    public static void InitialiseHeaders(global::System.Net.Http.HttpRequestMessage request)
    {
        // TODO: ensure not emitted when body.
        request.Content ??= new global::System.Net.Http.ByteArrayContent([]);
    }

    public static void AddHeader(global::System.Net.Http.HttpRequestMessage request, string key, string value)
    {
        SetHeader(request, key, value);
    }

    public static void AddHeaderCollection(global::System.Net.Http.HttpRequestMessage request, IDictionary<string, string> keys)
    {
        foreach (var pairs in keys)
        {
            SetHeader(request, pairs.Key, pairs.Value);
        }
    }

    static void SetHeader(global::System.Net.Http.HttpRequestMessage request, string name, string? value)
    {
        // Clear any existing version of this header that might be set, because
        // we want to allow removal/redefinition of headers.
        // We also don't want to double up content headers which may have been
        // set for us automatically.

        // NB: We have to enumerate the header names to check existence because
        // Contains throws if it's the wrong header type for the collection.
        if (request.Headers.Any(x => x.Key == name))
        {
            request.Headers.Remove(name);
        }

        if (request.Content != null && request.Content.Headers.Any(x => x.Key == name))
        {
            request.Content.Headers.Remove(name);
        }

        if (value == null)
            return;

        // CRLF injection protection
        name = EnsureSafe(name);
        value = EnsureSafe(value);

        var added = request.Headers.TryAddWithoutValidation(name, value);

        // Don't even bother trying to add the header as a content header
        // if we just added it to the other collection.
        if (!added && request.Content != null)
        {
            request.Content.Headers.TryAddWithoutValidation(name, value);
        }
    }

    static string EnsureSafe(string value)
    {
        // Remove CR and LF characters
#pragma warning disable CA1307 // Specify StringComparison for clarity
        return value.Replace("\r", string.Empty).Replace("\n", string.Empty);
#pragma warning restore CA1307 // Specify StringComparison for clarity
    }

    public static void AddBody(global::System.Net.Http.HttpRequestMessage request,
        global::Refit.RefitSettings settings, object param, bool isBuffered, BodySerializationMethod serializationMethod)
    {
        if (param is HttpContent httpContentParam)
        {
            request.Content = httpContentParam;
        }
        else if (param is Stream streamParam)
        {
            request.Content = new StreamContent(streamParam);
        }
        // Default sends raw strings
        else if (
            serializationMethod == BodySerializationMethod.Default
            && param is string stringParam
        )
        {
            request.Content = new StringContent(stringParam);
        }
        else
        {
            switch (serializationMethod)
            {
                case BodySerializationMethod.UrlEncoded:
                    request.Content = param is string str
                        ? (HttpContent)
                        new StringContent(
                            Uri.EscapeDataString(str),
                            Encoding.UTF8,
                            "application/x-www-form-urlencoded"
                        )
                        : new FormUrlEncodedContent(
                            new FormValueMultimap(param, settings)
                        );
                    break;
                case BodySerializationMethod.Default:
#pragma warning disable CS0618 // Type or member is obsolete
                case BodySerializationMethod.Json:
#pragma warning restore CS0618 // Type or member is obsolete
                case BodySerializationMethod.Serialized:
                    var content = settings.ContentSerializer.ToHttpContent(param);
                    switch (isBuffered)
                    {
                        case false:
                            request.Content = new PushStreamContent(
#pragma warning disable IDE1006 // Naming Styles
                                async (stream, _, __) =>
#pragma warning restore IDE1006 // Naming Styles
                                {
                                    using (stream)
                                    {
                                        await content
                                            .CopyToAsync(stream)
                                            .ConfigureAwait(false);
                                    }
                                },
                                content.Headers.ContentType
                            );
                            break;
                        case true:
                            request.Content = content;
                            break;
                    }

                    break;
            }
        }
    }

    public static void WriteProperty(global::System.Net.Http.HttpRequestMessage request, object key, object value) =>
        throw new NotImplementedException(nameof(WriteProperty));

    public static void WriteRefitSettingsProperties(global::System.Net.Http.HttpRequestMessage request, global::Refit.RefitSettings settings)
    {
        // Add RefitSetting.HttpRequestMessageOptions to the HttpRequestMessage
        if (settings.HttpRequestMessageOptions != null)
        {
            foreach (var p in settings.HttpRequestMessageOptions)
            {
#if NET6_0_OR_GREATER
                request.Options.Set(new HttpRequestOptionsKey<object>(p.Key), p.Value);
#else
                request.Properties.Add(p);
#endif
            }
        }
    }

    // TODO: qualify types, check nullability and use of generics here. Might stop a cheeky box
    public static void WriteRefitSettingsProperties(global::System.Net.Http.HttpRequestMessage request,
        string? key, object value)
    {
#if NET6_0_OR_GREATER
                request.Options.Set(
                    new HttpRequestOptionsKey<object?>(key),
                    value
                );
#else
        request.Properties[key] = value;
#endif
    }

    // TODO: is RestMethodInfo neeeded here? I feel like it breaks AOT
    public static void WriteRefitSettingsProperties(global::System.Net.Http.HttpRequestMessage request,
        Type interfaceType, RestMethodInfo restMethodInfo)
    {
        // Always add the top-level type of the interface to the properties
#if NET6_0_OR_GREATER
            request.Options.Set(
                new HttpRequestOptionsKey<Type>(HttpRequestMessageOptions.InterfaceType),
                interfaceType
            );
            request.Options.Set(
                new HttpRequestOptionsKey<RestMethodInfo>(
                    HttpRequestMessageOptions.RestMethodInfo
                ),
                restMethodInfo
            );
#else
        request.Properties[HttpRequestMessageOptions.InterfaceType] = interfaceType;
        request.Properties[HttpRequestMessageOptions.RestMethodInfo] =
            restMethodInfo;
#endif
    }

    public static void AddVersionToRequest(global::System.Net.Http.HttpRequestMessage request,
        global::Refit.RefitSettings settings)
    {
#if NET6_0_OR_GREATER
        request.Version = settings.Version;
        request.VersionPolicy = settings.VersionPolicy;
#endif
    }

    // TODO: double check if its an interface type of concrete type
    public static void AddTopLevelTypes(global::System.Net.Http.HttpRequestMessage request,
        global::System.Type interfaceType,
        global::Refit.RestMethodInfo restMethodInfo)
    {
// Always add the top-level type of the interface to the properties
#if NET6_0_OR_GREATER
                request.Options.Set(
                    new HttpRequestOptionsKey<Type>(HttpRequestMessageOptions.InterfaceType),
                    interfaceType
                );
                request.Options.Set(
                    new HttpRequestOptionsKey<RestMethodInfo>(
                        HttpRequestMessageOptions.RestMethodInfo
                    ),
                    restMethodInfo
                );
#else
        request.Properties[HttpRequestMessageOptions.InterfaceType] = interfaceType;
        request.Properties[HttpRequestMessageOptions.RestMethodInfo] = restMethodInfo;
#endif
    }


    // TODO: should this be split into methods for T, IApiResponse and ApiResponse??
    public static async Task<T> SendTaskResultAsync<T>(global::System.Net.Http.HttpRequestMessage request,
        global::System.Net.Http.HttpClient client,
        global::Refit.RefitSettings settings,
        bool isBodyBuffered,
        global::System.Threading.CancellationToken cancellationToken)
    {
        global::System.Net.Http.HttpResponseMessage? resp = null;
        global::System.Net.Http.HttpContent? content = null;
        var disposeResponse = true;
        try
        {
            // Load the data into buffer when body should be buffered.
            if (IsBodyBuffered(isBodyBuffered, request))
            {
                await request.Content!.LoadIntoBufferAsync().ConfigureAwait(false);
            }
            resp = await client
                .SendAsync(request, global::System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            content = resp.Content ?? new global::System.Net.Http.StringContent(string.Empty);
            Exception? e = null;

            // TODO: dispose
            disposeResponse = true;
            // disposeResponse = restMethod.ShouldDisposeResponse;

            if (typeof(T) != typeof(global::System.Net.Http.HttpResponseMessage))
            {
                e = await settings.ExceptionFactory(resp).ConfigureAwait(false);
            }

            // if (restMethod.IsApiResponse)
            // {
            //     var body = default(TBody);
            //
            //     try
            //     {
            //         // Only attempt to deserialize content if no error present for backward-compatibility
            //         body =
            //             e == null
            //                 ? await DeserializeContentAsync<TBody>(resp, content, cancellationToken)
            //                     .ConfigureAwait(false)
            //                 : default;
            //     }
            //     catch (Exception ex)
            //     {
            //         //if an error occured while attempting to deserialize return the wrapped ApiException
            //         if (settings.DeserializationExceptionFactory != null)
            //             e = await settings.DeserializationExceptionFactory(resp, ex).ConfigureAwait(false);
            //         else
            //         {
            //             e = await ApiException.Create(
            //                 "An error occured deserializing the response.",
            //                 resp.RequestMessage!,
            //                 resp.RequestMessage!.Method,
            //                 resp,
            //                 settings,
            //                 ex
            //             );
            //         }
            //     }
            //
            //     return ApiResponse.Create<T, TBody>(
            //         resp,
            //         body,
            //         settings,
            //         e as ApiException
            //     );
            // }
            if (e != null)
            {
                disposeResponse = false; // caller has to dispose
                throw e;
            }
            else
            {
                try
                {
                    return await DeserializeContentAsync<T>(resp, content, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (settings.DeserializationExceptionFactory != null)
                    {
                        var customEx = await settings.DeserializationExceptionFactory(resp, ex).ConfigureAwait(false);
                        if (customEx != null)
                            throw customEx;
                        return default;
                    }
                    else
                    {
                        throw await ApiException.Create(
                            "An error occured deserializing the response.",
                            resp.RequestMessage!,
                            resp.RequestMessage!.Method,
                            resp,
                            settings,
                            ex
                        );
                    }
                }
            }
        }
        finally
        {
            // Ensure we clean up the request
            // Especially important if it has open files/streams
            request.Dispose();
            if (disposeResponse)
            {
                resp?.Dispose();
                content?.Dispose();
            }
        }
    }

    private static bool IsBodyBuffered(
        bool isBuffered,
        HttpRequestMessage? request
    )
    {
        return isBuffered && (request?.Content != null);
    }


    // TODO: lots of overlap in cod etry and share?
    public static async Task<T?> SendTaskIApiResultAsync<T, TBody>(global::System.Net.Http.HttpRequestMessage request,
        global::System.Net.Http.HttpClient client,
        global::Refit.RefitSettings settings,
        bool isBuffered,
        global::System.Threading.CancellationToken cancellationToken)
    {
        global::System.Net.Http.HttpResponseMessage? resp = null;
        global::System.Net.Http.HttpContent? content = null;
        var disposeResponse = true;
        try
        {
            // TODO: add isBody buffered
            // Load the data into buffer when body should be buffered.
            if (IsBodyBuffered(isBuffered, request))
            {
                await request.Content!.LoadIntoBufferAsync().ConfigureAwait(false);
            }
            resp = await client
                .SendAsync(request, global::System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            content = resp.Content ?? new global::System.Net.Http.StringContent(string.Empty);
            Exception? e = null;

            // TODO: dispose
            disposeResponse = true;
            // disposeResponse = restMethod.ShouldDisposeResponse;

            if (typeof(T) != typeof(global::System.Net.Http.HttpResponseMessage))
            {
                e = await settings.ExceptionFactory(resp).ConfigureAwait(false);
            }

            var body = default(T);

            try
            {
                // Only attempt to deserialize content if no error present for backward-compatibility
                body =
                    e == null
                        ? await DeserializeContentAsync<T>(resp, content, cancellationToken)
                            .ConfigureAwait(false)
                        : default;
            }
            catch (Exception ex)
            {
                //if an error occured while attempting to deserialize return the wrapped ApiException
                if (settings.DeserializationExceptionFactory != null)
                    e = await settings.DeserializationExceptionFactory(resp, ex).ConfigureAwait(false);
                else
                {
                    e = await ApiException.Create(
                        "An error occured deserializing the response.",
                        resp.RequestMessage!,
                        resp.RequestMessage!.Method,
                        resp,
                        settings,
                        ex
                    );
                }
            }

            return ApiResponse.Create<T, TBody>(
                resp,
                body,
                settings,
                e as ApiException
            );
        }
        finally
        {
            // Ensure we clean up the request
            // Especially important if it has open files/streams
            request.Dispose();
            if (disposeResponse)
            {
                resp?.Dispose();
                content?.Dispose();
            }
        }
    }

    static async Task<T?> DeserializeContentAsync<T>(
        global::System.Net.Http.HttpResponseMessage resp,
        global::System.Net.Http.HttpContent content,
        CancellationToken cancellationToken
    )
    {
        T? result;
        if (typeof(T) == typeof(global::System.Net.Http.HttpResponseMessage))
        {
            // NB: This double-casting manual-boxing hate crime is the only way to make
            // this work without a 'class' generic constraint. It could blow up at runtime
            // and would be A Bad Idea if we hadn't already vetted the return type.
            result = (T)(object)resp;
        }
        else if (typeof(T) == typeof(global::System.Net.Http.HttpContent))
        {
            result = (T)(object)content;
        }
        else if (typeof(T) == typeof(Stream))
        {
            var stream = (object)
                await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            result = (T)stream;
        }
        else if (typeof(T) == typeof(string))
        {
            using var stream = await content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            var str = (object)await reader.ReadToEndAsync().ConfigureAwait(false);
            result = (T)str;
        }
        else
        {
            // result = await serializer
            //     .FromHttpContentAsync<T>(content, cancellationToken)
            //     .ConfigureAwait(false);
            throw new NotImplementedException("serializer");
        }
        return result;
    }

    public static async Task SendVoidTaskAsync(global::System.Net.Http.HttpRequestMessage request,
        global::System.Net.Http.HttpClient httpClient,
        global::Refit.RefitSettings settings,
        global::System.Threading.CancellationToken cancellationToken)
    {
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var exception = await settings.ExceptionFactory(response).ConfigureAwait(false);
        if(exception != null)
            throw exception;
    }
}
