// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Refit;

/// <summary>Body, query and multipart payload assembly for <see cref="RequestBuilderImplementation"/>.</summary>
internal partial class RequestBuilderImplementation
{
    /// <summary>Sets the request content from the body parameter using the configured serialization method.</summary>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="param">The body argument value.</param>
    /// <param name="ret">The request message to populate.</param>
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    internal void AddBodyToRequest(RestMethodInfoInternal restMethod, object param, HttpRequestMessage ret)
    {
        if (param is HttpContent httpContentParam)
        {
            ret.Content = httpContentParam;
            return;
        }

        if (param is Stream streamParam)
        {
            // The stream is caller-owned; wrap it so disposing the request never closes it.
            ret.Content = GeneratedRequestRunner.CreateStreamContent(streamParam);
            return;
        }

        // Default sends raw strings
        if (restMethod.BodyParameterInfo!.Item1 == BodySerializationMethod.Default
            && param is string stringParam)
        {
            ret.Content = new StringContent(stringParam);
            return;
        }

        switch (restMethod.BodyParameterInfo.Item1)
        {
            case BodySerializationMethod.UrlEncoded:
            {
                ret.Content = param is string str
                    ? new StringContent(
                        StringHelpers.EscapeDataString(str),
                        Encoding.UTF8,
                        "application/x-www-form-urlencoded")
                    : new FormUrlEncodedContent(new FormValueMultimap(param, _settings));
                break;
            }

            case BodySerializationMethod.JsonLines:
            {
                ret.Content = new JsonLinesContent(AsJsonLinesSequence(param), _serializer);
                break;
            }

            case BodySerializationMethod.Default or BodySerializationMethod.Serialized:
            {
                AddSerializedBodyToRequest(restMethod, param, ret);
                break;
            }

            default:
            {
                // The obsolete legacy JSON serialization method must still serialize, because
                // already-compiled callers can pass it. Treating it as Default would incorrectly
                // send string bodies as raw text. It is matched by value rather than by name so
                // this file never references the obsolete member.
                if (GeneratedRequestRunner.IsObsoleteJsonSerializationMethod(restMethod.BodyParameterInfo.Item1))
                {
                    AddSerializedBodyToRequest(restMethod, param, ret);
                }

                break;
            }
        }
    }

    /// <summary>Sets the request content from a serialized body, optionally streaming it.</summary>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="param">The body argument value.</param>
    /// <param name="ret">The request message to populate.</param>
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    internal void AddSerializedBodyToRequest(RestMethodInfoInternal restMethod, object param, HttpRequestMessage ret)
    {
        var declaredBodyType = restMethod.ParameterInfoArray[
            restMethod.BodyParameterInfo!.Item3].ParameterType;

        // Synchronous serialization lets the source-gen fast-path engage: Buffered produces a ByteArrayContent up
        // front, Streamed writes through a Utf8JsonWriter on the request stream without buffering the whole body.
        if (_settings.RequestBodySerialization != RequestBodySerializationMode.Default
            && _serializer is ISynchronousContentSerializer syncSerializer)
        {
            ret.Content = _settings.RequestBodySerialization == RequestBodySerializationMode.Streamed
                ? SerializeBodyStreaming(syncSerializer, param, declaredBodyType)
                : SerializeBodySynchronously(syncSerializer, param, declaredBodyType);
            return;
        }

        var content = SerializeBody(_serializer, param, declaredBodyType);

        if (restMethod.BodyParameterInfo.Item2)
        {
            ret.Content = content;
            return;
        }

        ret.Content = new PushStreamContent(
            async (stream, _, _) =>
            {
#if NET8_0_OR_GREATER
                await using (stream.ConfigureAwait(false))
#else
                using (stream)
#endif
                {
                    await content
                        .CopyToAsync(stream)
                        .ConfigureAwait(false);
                }
            },
            content.Headers.ContentType);
    }

    /// <summary>Adds query-string parameters for a single argument to the pending query list.</summary>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="queryAttribute">The query attribute on the parameter, if any.</param>
    /// <param name="param">The argument value.</param>
    /// <param name="queryParamsToAdd">The list of query parameters being built.</param>
    /// <param name="i">The index of the parameter.</param>
    /// <param name="parameterInfo">Optional parameter info for property-bound parameters.</param>
    internal void AddQueryParameters(
        RestMethodInfoInternal restMethod,
        QueryAttribute? queryAttribute,
        object param,
        List<QueryParameterEntry> queryParamsToAdd,
        int i,
        RestMethodParameterInfo? parameterInfo)
    {
        var attr = queryAttribute ?? DefaultQueryAttribute;

        // TreatAsString, or an explicitly empty Format, serializes the value via ToString() under the
        // parameter name instead of flattening a complex object's public properties into the query.
        if (attr.TreatAsString || attr.Format is { Length: 0 })
        {
            AppendQueryParameter(
                queryParamsToAdd,
                param.ToString(),
                restMethod.ParameterInfoArray[i],
                restMethod.QueryParameterMap[i],
                attr);
            return;
        }

        if (DoNotConvertToQueryMap(param))
        {
            AppendQueryParameter(
                queryParamsToAdd,
                param,
                restMethod.ParameterInfoArray[i],
                restMethod.QueryParameterMap[i],
                attr);
            return;
        }

        var parameterCollectionFormat = attr.IsCollectionFormatSpecified
            ? attr.CollectionFormat
            : (CollectionFormat?)null;
        var queryMap = BuildQueryMap(param, attr.Delimiter, parameterInfo, parameterCollectionFormat);
        for (var queryMapIndex = 0; queryMapIndex < queryMap.Count; queryMapIndex++)
        {
            var kvp = queryMap[queryMapIndex];
            var path = !string.IsNullOrWhiteSpace(attr.Prefix)
                ? attr.Prefix + attr.Delimiter + kvp.Key
                : kvp.Key;
            AppendQueryParameter(
                queryParamsToAdd,
                kvp.Value,
                restMethod.ParameterInfoArray[i],
                path,
                attr);
        }
    }

    /// <summary>Adds one (or each enumerated) multipart part for a single argument.</summary>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="i">The index of the parameter.</param>
    /// <param name="param">The argument value, which may be a single item or an enumerable.</param>
    /// <param name="multiPartContent">The multipart content to add to.</param>
    internal void AddMultiPart(
        RestMethodInfoInternal restMethod,
        int i,
        object param,
        MultipartFormDataContent? multiPartContent)
    {
        // we are in a multipart method, add the part to the content
        // the parameter name should be either the attachment name or the parameter name (as fallback)
        string itemName;
        string parameterName;

        // An opt-in [FormObject] parameter is flattened into one text form-data part per property (resolved field name +
        // formatted value) so server-side form model binding sees individual fields instead of a single serialized part.
        if (restMethod.ParameterFormObjectFlags[i])
        {
            AddFlattenedFormObject(multiPartContent!, param);
            return;
        }

        if (!restMethod.AttachmentNameMap.TryGetValue(i, out var attachment))
        {
            itemName = restMethod.QueryParameterMap[i];
            parameterName = itemName;
        }
        else
        {
            itemName = attachment.Item1;
            parameterName = attachment.Item2;
        }

        // Check to see if it's an IEnumerable
        if (param is IEnumerable<object> enumerable)
        {
            foreach (var item in enumerable)
            {
                AddMultipartItem(multiPartContent!, itemName, parameterName, item);
            }
        }
        else
        {
            AddMultipartItem(multiPartContent!, itemName, parameterName, param);
        }
    }

    /// <summary>Flattens a complex object's properties into one text multipart part each for a <c>[FormObject]</c> parameter.</summary>
    /// <param name="multiPartContent">The multipart content to add to.</param>
    /// <param name="param">The object (or dictionary) whose fields become individual form-data parts.</param>
    /// <remarks>Reuses <see cref="FormValueMultimap"/> so field-name resolution (alias, serializer, key formatter),
    /// value formatting, collection handling and nested <c>parent.child</c> composition match url-encoded body
    /// flattening. Each entry is added as its own text <see cref="StringContent"/> under the resolved field name.</remarks>
    internal void AddFlattenedFormObject(MultipartFormDataContent multiPartContent, object param)
    {
        foreach (var field in new FormValueMultimap(param, _settings))
        {
            // A field with no resolvable name cannot be a valid form-data part (the framework rejects an empty
            // content-disposition name), so it is skipped rather than allowed to throw mid-request.
            if (string.IsNullOrWhiteSpace(field.Key))
            {
                continue;
            }

            multiPartContent.Add(new StringContent(field.Value ?? string.Empty), field.Key!);
        }
    }

    /// <summary>Adds a single value to a multipart form as the appropriate content type.</summary>
    /// <param name="multiPartContent">The multipart content to add to.</param>
    /// <param name="fileName">The file name to use for file-like parts.</param>
    /// <param name="parameterName">The form field name for the part.</param>
    /// <param name="itemValue">The value to add.</param>
    internal void AddMultipartItem(
        MultipartFormDataContent multiPartContent,
        string fileName,
        string parameterName,
        object itemValue)
    {
        if (itemValue is HttpContent content)
        {
            multiPartContent.Add(content);
            return;
        }

        if (itemValue is MultipartItem multipartItem)
        {
            var httpContent = multipartItem.ToContent();
            multiPartContent.Add(
                httpContent,
                multipartItem.Name ?? parameterName,
                string.IsNullOrEmpty(multipartItem.FileName) ? fileName : multipartItem.FileName);
            return;
        }

        if (itemValue is Stream streamValue)
        {
            // The stream is caller-owned; wrap it so disposing the request never closes it.
            var streamContent = GeneratedRequestRunner.CreateStreamContent(streamValue);
            multiPartContent.Add(streamContent, parameterName, fileName);
            return;
        }

        if (itemValue is string stringValue)
        {
            multiPartContent.Add(new StringContent(stringValue), parameterName);
            return;
        }

        if (itemValue is FileInfo fileInfoValue)
        {
            var fileContent = new StreamContent(fileInfoValue.OpenRead());
            multiPartContent.Add(fileContent, parameterName, fileInfoValue.Name);
            return;
        }

        if (itemValue is byte[] byteArrayValue)
        {
            var fileContent = new ByteArrayContent(byteArrayValue);
            multiPartContent.Add(fileContent, parameterName, fileName);
            return;
        }

        AddSerializedMultipartItem(multiPartContent, fileName, parameterName, itemValue);
    }

    /// <summary>Adds a multipart part by serializing the value, throwing a descriptive error on failure.</summary>
    /// <param name="multiPartContent">The multipart content to add to.</param>
    /// <param name="fileName">The file name used in the error message.</param>
    /// <param name="parameterName">The form field name for the part.</param>
    /// <param name="itemValue">The value to serialize and add.</param>
    internal void AddSerializedMultipartItem(
        MultipartFormDataContent multiPartContent,
        string fileName,
        string parameterName,
        object itemValue)
    {
        // Date/time and Guid values are wrapped in quotes by the JSON serializer (e.g. "<guid>"),
        // which servers reject when reading a multipart form field. Send them as their plain text
        // representation instead. Numbers, booleans and enums are intentionally left to the
        // serializer to preserve existing behavior.
        if (itemValue is Guid or DateTime or DateTimeOffset or TimeSpan
#if NET6_0_OR_GREATER
            or DateOnly or TimeOnly
#endif
            )
        {
            var formatted = _settings.FormUrlEncodedParameterFormatter.Format(itemValue, null);
            multiPartContent.Add(new StringContent(formatted ?? string.Empty), parameterName);
            return;
        }

        // Fallback to serializer
        Exception e;
        try
        {
            multiPartContent.Add(
                _settings.ContentSerializer.ToHttpContent(itemValue),
                parameterName);
            return;
        }
        catch (Exception ex)
        {
            // Eat this since we're about to throw as a fallback anyway
            e = ex;
        }

        const string allowedTypes = "String, Stream, FileInfo, Byte array and anything that's JSON serializable";
        var parameterType = itemValue.GetType().Name;
        throw new ArgumentException(
            $"Unexpected parameter type in a Multipart request. Parameter {fileName} is of type {parameterType}, whereas allowed types are {allowedTypes}",
            nameof(itemValue),
            e);
    }

    /// <summary>Appends query key/value pairs for a single parameter value.</summary>
    /// <param name="queryParamsToAdd">The list receiving query parameters.</param>
    /// <param name="param">The parameter value.</param>
    /// <param name="parameterInfo">Reflection info for the parameter.</param>
    /// <param name="queryPath">The query key path for the parameter.</param>
    /// <param name="queryAttribute">The query attribute governing formatting.</param>
    internal void AppendQueryParameter(
        List<QueryParameterEntry> queryParamsToAdd,
        object? param,
        ParameterInfo parameterInfo,
        string queryPath,
        QueryAttribute queryAttribute)
    {
        if (param is not string and IEnumerable paramValues)
        {
            AppendFormattedEnumerableValues(
                paramValues,
                parameterInfo,
                parameterInfo.ParameterType,
                queryAttribute,
                null,
                new QueryParameterEntrySink(queryParamsToAdd, queryPath));
            return;
        }

        queryParamsToAdd.Add(
            new(
                queryPath,
                GeneratedRequestRunner.FormatUrlParameter(
                    _settings,
                    param,
                    GetCachedAttributeProvider(parameterInfo),
                    parameterInfo.ParameterType)));
    }

    /// <summary>Formats an enumerable value according to the effective collection format and appends each result to a
    /// sink, without allocating an intermediate sequence or iterator state machine.</summary>
    /// <typeparam name="TSink">The sink that receives each formatted value.</typeparam>
    /// <param name="paramValues">The enumerable values to format.</param>
    /// <param name="customAttributeProvider">The attribute provider for the parameter or property.</param>
    /// <param name="type">The element type used for formatting.</param>
    /// <param name="queryAttribute">The query attribute governing the collection format, if any.</param>
    /// <param name="fallbackCollectionFormat">The collection format to use when none is specified.</param>
    /// <param name="sink">The sink receiving each formatted value.</param>
    internal void AppendFormattedEnumerableValues<TSink>(
        IEnumerable paramValues,
        ICustomAttributeProvider customAttributeProvider,
        Type type,
        QueryAttribute? queryAttribute,
        CollectionFormat? fallbackCollectionFormat,
        TSink sink)
        where TSink : struct, IQueryValueSink
    {
        // Precedence: the property's own [Query] format wins; otherwise the format
        // supplied by the enclosing parameter's [Query] attribute (if any); finally
        // the global RefitSettings default.
        var collectionFormat =
            queryAttribute?.IsCollectionFormatSpecified == true
                ? queryAttribute.CollectionFormat
                : fallbackCollectionFormat ?? _settings.CollectionFormat;

        if (collectionFormat == CollectionFormat.Multi)
        {
            var cachedProvider = GetCachedAttributeProvider(customAttributeProvider);
            foreach (var paramValue in paramValues)
            {
                sink.Add(
                    GeneratedRequestRunner.FormatUrlParameter(
                        _settings,
                        paramValue,
                        cachedProvider,
                        type));
            }

            return;
        }

        var delimiter =
            collectionFormat switch
            {
                CollectionFormat.Ssv => " ",
                CollectionFormat.Tsv => "\t",
                CollectionFormat.Pipes => "|",
                _ => ","
            };

        // A single joined value is emitted; a missing default arm here previously dropped the collection entirely.
        sink.Add(JoinFormattedQueryValues(paramValues, customAttributeProvider, type, delimiter));
    }

    /// <summary>Formats and joins an enumerable query value without LINQ adapters.</summary>
    /// <param name="paramValues">The enumerable values to format.</param>
    /// <param name="customAttributeProvider">The attribute provider for the parameter or property.</param>
    /// <param name="type">The element type used for formatting.</param>
    /// <param name="delimiter">The delimiter between formatted values.</param>
    /// <returns>The joined formatted values.</returns>
    [SuppressMessage(
        "Correctness",
        "SST2410:A created disposable is never disposed",
        Justification = "ValueStringBuilder.ToString() disposes the builder and returns its pooled buffer; Dispose is idempotent.")]
    internal string JoinFormattedQueryValues(
        IEnumerable paramValues,
        ICustomAttributeProvider customAttributeProvider,
        Type type,
        string delimiter)
    {
        var enumerator = paramValues.GetEnumerator();
        try
        {
            if (!enumerator.MoveNext())
            {
                return string.Empty;
            }

            var cachedProvider = GetCachedAttributeProvider(customAttributeProvider);
            var builder = new ValueStringBuilder(stackalloc char[StackallocThreshold]);
            builder.Append(
                GeneratedRequestRunner.FormatUrlParameter(
                    _settings,
                    enumerator.Current,
                    cachedProvider,
                    type));

            while (enumerator.MoveNext())
            {
                builder.Append(delimiter);
                builder.Append(
                    GeneratedRequestRunner.FormatUrlParameter(
                        _settings,
                        enumerator.Current,
                        cachedProvider,
                        type));
            }

            return builder.ToString();
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }
}
