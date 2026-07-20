// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>Pins the parsed <see cref="RestMethodInfoInternal"/> shape the reflection request-builder constructor produces
/// for every attribute shape on <see cref="IReflectionParseShapeApi"/>. Constructing the metadata reads each parameter's
/// request-shaping attributes; these assertions guard that per-parameter classification (path binding, headers, header
/// collection, authorization, request property, body, multipart and absolute URL) against silent regressions when the
/// constructor's attribute reads are restructured for allocation.</summary>
public sealed class ReflectionConstructorParseShapeTests
{
    /// <summary>The count of static headers a constant route inherits from the interface plus its method.</summary>
    private const int StaticHeaderCount = 2;

    /// <summary>The length of the property chain a two-level nested path placeholder resolves.</summary>
    private const int NestedChainLength = 2;

    /// <summary>The mapped parameter count of the scalar-query method.</summary>
    private const int ScalarParameterCount = 2;

    /// <summary>The mapped parameter count of the densely attributed method.</summary>
    private const int DenseParameterCount = 5;

    /// <summary>The parameter index of the authorization argument on the densely attributed method.</summary>
    private const int AuthorizeParameterIndex = 2;

    /// <summary>The parameter index of the request-property argument on the densely attributed method.</summary>
    private const int PropertyParameterIndex = 3;

    /// <summary>The parameter index of the scalar-query argument on the densely attributed method.</summary>
    private const int QueryParameterIndex = 4;

    /// <summary>The part count of the multipart upload method.</summary>
    private const int MultipartPartCount = 3;

    /// <summary>The parameter index of the stream part on the multipart upload method.</summary>
    private const int StreamPartIndex = 2;

    /// <summary>A constant route carries both the interface and method static headers and no parameters.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ConstantRouteParsesStaticHeadersAndNoParameters()
    {
        var info = Parse(nameof(IReflectionParseShapeApi.Constant));

        await Assert.That(info.RelativePath).IsEqualTo("/constant");
        await Assert.That(info.IsMultipart).IsFalse();
        await Assert.That(info.ParameterInfoArray.Length).IsEqualTo(0);
        await Assert.That(info.ParameterMap.Count).IsEqualTo(0);
        await Assert.That(info.QueryParameterMap.Count).IsEqualTo(0);
        await Assert.That(info.BodyParameterInfo).IsNull();
        await Assert.That(info.AuthorizeParameterInfo).IsNull();
        await Assert.That(info.UrlParameterInfo).IsLessThan(0);
        await Assert.That(info.CancellationToken).IsNull();
        await Assert.That(info.Headers.Count).IsEqualTo(StaticHeaderCount);
        await Assert.That(info.Headers["X-Interface"]).IsEqualTo("iface");
        await Assert.That(info.Headers["X-Method"]).IsEqualTo("method");
    }

    /// <summary>An aliased path parameter binds the segment under its alias and never enters the query map.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AliasedSegmentParsesAsNormalPathParameter()
    {
        var info = Parse(nameof(IReflectionParseShapeApi.AliasedSegment));

        await Assert.That(info.RelativePath).IsEqualTo("/users/{id}");
        await Assert.That(info.ParameterInfoArray.Length).IsEqualTo(1);
        await Assert.That(info.ParameterMap.Count).IsEqualTo(1);
        await Assert.That(info.ParameterMap[0].IsObjectPropertyParameter).IsFalse();
        await Assert.That(info.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(info.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(info.QueryParameterMap.Count).IsEqualTo(0);
        await Assert.That(info.Headers.Count).IsEqualTo(1);
        await Assert.That(info.Headers["X-Interface"]).IsEqualTo("iface");
    }

    /// <summary>A nested-object path placeholder resolves the full property chain in declared order.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NestedObjectPathParsesFullPropertyChain()
    {
        var info = Parse(nameof(IReflectionParseShapeApi.NestedPath));

        await Assert.That(info.RelativePath).IsEqualTo("/orgs/{request.Inner.Code}/audit");
        await Assert.That(info.ParameterMap.Count).IsEqualTo(1);
        await Assert.That(info.ParameterMap[0].IsObjectPropertyParameter).IsTrue();
        await Assert.That(info.ParameterMap[0].ParameterProperties.Count).IsEqualTo(1);

        var chain = info.ParameterMap[0].ParameterProperties[0].PropertyChain;
        await Assert.That(chain.Count).IsEqualTo(NestedChainLength);
        await Assert.That(chain[0].Name).IsEqualTo("Inner");
        await Assert.That(chain[1].Name).IsEqualTo("Code");
    }

    /// <summary>Scalar query parameters populate the query map under their CLR names and are not path parameters.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ScalarQueryParametersPopulateQueryMap()
    {
        var info = Parse(nameof(IReflectionParseShapeApi.ScalarQuery));

        await Assert.That(info.RelativePath).IsEqualTo("/search");
        await Assert.That(info.ParameterInfoArray.Length).IsEqualTo(ScalarParameterCount);
        await Assert.That(info.ParameterMap.Count).IsEqualTo(0);
        await Assert.That(info.BodyParameterInfo).IsNull();
        await Assert.That(info.QueryParameterMap[0]).IsEqualTo("q");
        await Assert.That(info.QueryParameterMap[1]).IsEqualTo("page");
    }

    /// <summary>A collection query parameter carries its <see cref="QueryAttribute"/> in the materialized array.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CollectionQueryParameterCachesQueryAttribute()
    {
        var info = Parse(nameof(IReflectionParseShapeApi.CollectionQuery));

        await Assert.That(info.QueryParameterMap[0]).IsEqualTo("tags");
        await Assert.That(info.ParameterQueryAttributes[0]).IsNotNull();
        await Assert.That(info.ParameterQueryAttributes[0]!.CollectionFormat).IsEqualTo(CollectionFormat.Multi);
    }

    /// <summary>Each distinctly attributed parameter is classified into its own map: header, header collection,
    /// authorization, request property, and a plain scalar query.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DenseAttributesClassifyEachParameterIntoItsOwnMap()
    {
        var info = Parse(nameof(IReflectionParseShapeApi.DenseAttributes));

        await Assert.That(info.ParameterInfoArray.Length).IsEqualTo(DenseParameterCount);

        await Assert.That(info.HeaderParameterMap.Count).IsEqualTo(1);
        await Assert.That(info.HeaderParameterMap[0]).IsEqualTo("X-Api-Key");

        await Assert.That(info.HasHeaderCollection).IsTrue();
        await Assert.That(info.HeaderCollectionAt(1)).IsTrue();

        await Assert.That(info.AuthorizeParameterInfo).IsNotNull();
        await Assert.That(info.AuthorizeParameterInfo!.Item1).IsEqualTo("Bearer");
        await Assert.That(info.AuthorizeParameterInfo.Item2).IsEqualTo(AuthorizeParameterIndex);

        await Assert.That(info.PropertyParameterMap.Count).IsEqualTo(1);
        await Assert.That(info.PropertyParameterMap[PropertyParameterIndex]).IsEqualTo("trace-id");

        await Assert.That(info.QueryParameterMap.Count).IsEqualTo(1);
        await Assert.That(info.QueryParameterMap[QueryParameterIndex]).IsEqualTo("filter");

        await Assert.That(info.BodyParameterInfo).IsNull();
        await Assert.That(info.Headers["X-Method"]).IsEqualTo("dense");
    }

    /// <summary>An implicit reference-type argument on a POST is parsed as the serialized body parameter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SerializedBodyParsesImplicitBodyParameter()
    {
        var info = Parse(nameof(IReflectionParseShapeApi.SerializedBody));

        await Assert.That(info.RelativePath).IsEqualTo("/body");
        await Assert.That(info.IsMultipart).IsFalse();
        await Assert.That(info.BodyParameterInfo).IsNotNull();
        await Assert.That(info.BodyParameterInfo!.Item3).IsEqualTo(0);
        await Assert.That(info.BodyParameterInfo.Item1).IsEqualTo(BodySerializationMethod.Default);
    }

    /// <summary>A multipart method has no body parameter and exposes each part as a query-map name.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartMethodParsesPartsWithoutBody()
    {
        var info = Parse(nameof(IReflectionParseShapeApi.Upload));

        await Assert.That(info.IsMultipart).IsTrue();
        await Assert.That(info.MultipartBoundary).IsNotEmpty();
        await Assert.That(info.ParameterInfoArray.Length).IsEqualTo(MultipartPartCount);
        await Assert.That(info.BodyParameterInfo).IsNull();
        await Assert.That(info.AttachmentNameMap.Count).IsEqualTo(0);
        await Assert.That(info.QueryParameterMap.Count).IsEqualTo(MultipartPartCount);
        await Assert.That(info.QueryParameterMap[0]).IsEqualTo("title");
        await Assert.That(info.QueryParameterMap[1]).IsEqualTo("payload");
        await Assert.That(info.QueryParameterMap[StreamPartIndex]).IsEqualTo("content");
    }

    /// <summary>A <c>[Url]</c> parameter is recorded as the absolute-URI source and excluded from the query map.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AbsoluteUrlParameterIsRecordedAndExcludedFromQuery()
    {
        var info = Parse(nameof(IReflectionParseShapeApi.AbsoluteUrl));

        await Assert.That(info.RelativePath).IsEqualTo(string.Empty);
        await Assert.That(info.UrlParameterInfo).IsEqualTo(0);
        await Assert.That(info.ParameterInfoArray.Length).IsEqualTo(1);
        await Assert.That(info.QueryParameterMap.Count).IsEqualTo(0);
    }

    /// <summary>A cancellation-token argument is stripped from the mapped parameters but recorded on the method.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CancellationTokenParameterIsStrippedButRecorded()
    {
        var info = Parse(nameof(IReflectionParseShapeApi.Cancelable));

        await Assert.That(info.RelativePath).IsEqualTo("/cancelable/{id}");
        await Assert.That(info.ParameterInfoArray.Length).IsEqualTo(1);
        await Assert.That(info.CancellationToken).IsNotNull();
        await Assert.That(info.ParameterMap.Count).IsEqualTo(1);
        await Assert.That(info.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
    }

    /// <summary>An open generic method is parsed for selection with its single mapped parameter and no route binding.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodParsesForSelection()
    {
        var info = Parse(nameof(IReflectionParseShapeApi.TypedItem));

        await Assert.That(info.RelativePath).IsEqualTo("/items");
        await Assert.That(info.ParameterInfoArray.Length).IsEqualTo(1);
        await Assert.That(info.ParameterMap.Count).IsEqualTo(0);
        await Assert.That(info.QueryParameterMap[0]).IsEqualTo("probe");
    }

    /// <summary>Builds the parsed metadata for a method on the shape interface.</summary>
    /// <param name="name">The method name to parse.</param>
    /// <returns>The parsed method metadata.</returns>
    private static RestMethodInfoInternal Parse(string name) =>
        new(
            typeof(IReflectionParseShapeApi),
            typeof(IReflectionParseShapeApi).GetMethod(name)
                ?? throw new MissingMethodException(nameof(IReflectionParseShapeApi), name),
            new RefitSettings());
}
