// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
extern alias RefitAnalyzers;

using System.Collections;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using AnalyzerBodyBufferMode = RefitAnalyzers::Refit.Generator.BodyBufferMode;
using AnalyzerFactory = RefitAnalyzers::Refit.Generator.ImmutableEquatableArrayFactory;
using AnalyzerHeader = RefitAnalyzers::Refit.Generator.HeaderModel;
using AnalyzerInterfaceGenerationContext = RefitAnalyzers::Refit.Generator.InterfaceGenerationContext;
using AnalyzerParameterAttributeModel = RefitAnalyzers::Refit.Generator.ParameterAttributeModel;
using AnalyzerParser = RefitAnalyzers::Refit.Generator.Parser;
using AnalyzerRequestModel = RefitAnalyzers::Refit.Generator.RequestModel;
using AnalyzerRequestParameterKind = RefitAnalyzers::Refit.Generator.RequestParameterKind;
using AnalyzerRequestParameterModel = RefitAnalyzers::Refit.Generator.RequestParameterModel;
using AnalyzerReturnTypeInfo = RefitAnalyzers::Refit.Generator.ReturnTypeInfo;
using AnalyzerTypeExtensions = RefitAnalyzers::Refit.Generator.ITypeSymbolExtensions;

namespace Refit.GeneratorTests;

/// <summary>Direct contract coverage for the generator parser components embedded in the RF006 analyzer.</summary>
public sealed class AnalyzerEmbeddedParserComponentTests
{
    /// <summary>The two-element collection size used by the value-contract checks.</summary>
    private const int TwoElements = 2;

    /// <summary>The end offset of the sample path-placeholder range.</summary>
    private const int PlaceholderEnd = 4;

    /// <summary>The rendered type name stored in the request model.</summary>
    private const string StringTypeName = "string";

    /// <summary>The URL-encoded body serialization name.</summary>
    private const string UrlEncodedSerialization = "UrlEncoded";

    /// <summary>An unsupported body serialization enum value.</summary>
    private const int UnsupportedSerialization = 99;

    /// <summary>The serialized body serialization enum value.</summary>
    private const int SerializedSerialization = 3;

    /// <summary>The sample users route.</summary>
    private const string UsersRoute = "/users";

    /// <summary>The metadata-reference alias used by the qualification checks.</summary>
    private const string ExternalAlias = "external";

    /// <summary>The fully-qualified system string type name.</summary>
    private const string GlobalStringTypeName = "global::System.String";

    /// <summary>The metadata name of the standard formattable interface.</summary>
    private const string FormattableMetadataName = "System.IFormattable";

    /// <summary>Exercises the embedded immutable array's collection, equality, hash, and enumeration contracts.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ImmutableArrayImplementsValueAndCollectionContracts()
    {
        var empty = AnalyzerFactory.Empty<string>();
        var first = AnalyzerFactory.FromArray(["one", "two"]);
        List<string> equalValues = ["one", "two"];
        List<string> noValues = [];
        var equal = AnalyzerFactory.FromList(equalValues);
        var differentValue = AnalyzerFactory.FromArray(["one", "three"]);
        var differentLength = AnalyzerFactory.FromArray(["one"]);
        var emptyFromArray = AnalyzerFactory.FromArray<string>([]);
        var emptyFromList = AnalyzerFactory.FromList(noValues);

        await Assert.That(empty.Count).IsEqualTo(0);
        await Assert.That(empty.AsArray()).IsEmpty();
        await Assert.That(empty.GetHashCode()).IsEqualTo(0);
        await Assert.That(empty.Equals(emptyFromArray)).IsTrue();
        await Assert.That(empty.Equals(emptyFromList)).IsTrue();
        await Assert.That(((IEnumerable<string>)empty).Any()).IsFalse();
        var emptyNonGenericEnumerator = ((IEnumerable)empty).GetEnumerator();
        await Assert.That(emptyNonGenericEnumerator.MoveNext()).IsFalse();
        await Assert.That(first.Count).IsEqualTo(TwoElements);
        await Assert.That(first[0]).IsEqualTo("one");
        await Assert.That(first.Equals(equal)).IsTrue();
        await Assert.That(first.Equals(differentValue)).IsFalse();
        await Assert.That(first.Equals(differentLength)).IsFalse();
        await Assert.That(first.Equals((object)equal)).IsTrue();
        await Assert.That(first.Equals("not an array")).IsFalse();
        await Assert.That(first.GetHashCode()).IsEqualTo(equal.GetHashCode());
        var directEnumeration = first.ToArray();
        var genericEnumeration = ((IEnumerable<string>)first).ToArray();
        var nonGenericEnumeration = ((IEnumerable)first).Cast<string>().ToArray();
        await Assert.That(directEnumeration.Length).IsEqualTo(TwoElements);
        await Assert.That(directEnumeration[1]).IsEqualTo("two");
        await Assert.That(genericEnumeration.Length).IsEqualTo(TwoElements);
        await Assert.That(genericEnumeration[1]).IsEqualTo("two");
        await Assert.That(nonGenericEnumeration.Length).IsEqualTo(TwoElements);
        await Assert.That(nonGenericEnumeration[1]).IsEqualTo("two");
    }

    /// <summary>Exercises the embedded request records and their initialized properties.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestModelsRetainAllValues()
    {
        var header = new AnalyzerHeader("X-Test", "value");
        var headers = AnalyzerFactory.FromArray([header]);
        var parameters = AnalyzerFactory.Empty<AnalyzerRequestParameterModel>();
        var model = new AnalyzerRequestModel(
            "GET",
            "/items",
            StringTypeName,
            StringTypeName,
            IsApiResponse: true,
            ShouldDisposeResponse: false,
            CanGenerateInline: true,
            "Adapter",
            headers,
            parameters)
        {
            IsMultipart = true,
            MultipartBoundary = "boundary",
            QueryUriFormat = 1,
            TimeoutMilliseconds = TwoElements,
        };

        await Assert.That(header.Name).IsEqualTo("X-Test");
        await Assert.That(header.Value).IsEqualTo("value");
        await Assert.That(model.HttpMethod).IsEqualTo("GET");
        await Assert.That(model.Path).IsEqualTo("/items");
        await Assert.That(model.ResultType).IsEqualTo(StringTypeName);
        await Assert.That(model.DeserializedResultType).IsEqualTo(StringTypeName);
        await Assert.That(model.IsApiResponse).IsTrue();
        await Assert.That(model.ShouldDisposeResponse).IsFalse();
        await Assert.That(model.CanGenerateInline).IsTrue();
        await Assert.That(model.AdapterTypeExpression).IsEqualTo("Adapter");
        await Assert.That(model.StaticHeaders[0]).IsEqualTo(header);
        await Assert.That(model.Parameters).IsEmpty();
        await Assert.That(model.IsMultipart).IsTrue();
        await Assert.That(model.MultipartBoundary).IsEqualTo("boundary");
        await Assert.That(model.QueryUriFormat).IsEqualTo(1);
        await Assert.That(model.TimeoutMilliseconds).IsEqualTo(TwoElements);
        await Assert.That(AnalyzerRequestModel.Empty.Path).IsEmpty();
    }

    /// <summary>Exercises embedded type hierarchy checks with base classes, interfaces, and unrelated types.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TypeHierarchyChecksIncludeInterfacesOnlyWhenRequested()
    {
        var compilation = CSharpCompilation.Create(
            "hierarchy",
            [CSharpSyntaxTree.ParseText("interface IFirst { } interface IMarker { } class Base { } class Derived : Base, IFirst, IMarker { }")]);
        var derived = compilation.GetTypeByMetadataName("Derived") ?? throw new InvalidOperationException("Missing Derived symbol.");
        var baseType = compilation.GetTypeByMetadataName("Base") ?? throw new InvalidOperationException("Missing Base symbol.");
        var marker = compilation.GetTypeByMetadataName("IMarker") ?? throw new InvalidOperationException("Missing IMarker symbol.");

        await Assert.That(AnalyzerTypeExtensions.InheritsFromOrEquals(derived, derived)).IsTrue();
        await Assert.That(AnalyzerTypeExtensions.InheritsFromOrEquals(derived, baseType)).IsTrue();
        await Assert.That(AnalyzerTypeExtensions.InheritsFromOrEquals(derived, marker)).IsFalse();
        await Assert.That(AnalyzerTypeExtensions.InheritsFromOrEquals(derived, baseType, includeInterfaces: true)).IsTrue();
        await Assert.That(AnalyzerTypeExtensions.InheritsFromOrEquals(derived, marker, includeInterfaces: false)).IsFalse();
        await Assert.That(AnalyzerTypeExtensions.InheritsFromOrEquals(derived, marker, includeInterfaces: true)).IsTrue();
        await Assert.That(AnalyzerTypeExtensions.InheritsFromOrEquals(baseType, marker, includeInterfaces: true)).IsFalse();
    }

    /// <summary>Exercises parser helpers compiled into the analyzer rather than reaching them through reflection.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmbeddedParserHelpersClassifyPathsBodiesAndHeaders()
    {
        const string path = "/path";
        const int whitespaceLength = 2;
        var headers = new List<AnalyzerHeader>();

        await Assert.That(AnalyzerParser.NormalizeConstantPathForInline(path)).IsEqualTo(path);
        await Assert.That(AnalyzerParser.NormalizeConstantPathForInline("/path?")).IsEqualTo(path);
        await Assert.That(AnalyzerParser.NormalizeConstantPathForInline("/path?one=1&&two=2#fragment")).IsEqualTo("/path?one=1&two=2");
        await Assert.That(AnalyzerParser.IsPathSupported(string.Empty)).IsTrue();
        await Assert.That(AnalyzerParser.IsPathSupported("/{id}")).IsTrue();
        await Assert.That(AnalyzerParser.IsPathSupported("/id}")).IsFalse();
        await Assert.That(AnalyzerParser.IsPathSupported("/line\nbreak")).IsFalse();
        await Assert.That(AnalyzerParser.IsPathSupported("/line\rbreak")).IsFalse();
        await Assert.That(AnalyzerParser.IsWhiteSpace(" \t", 0, whitespaceLength)).IsTrue();
        await Assert.That(AnalyzerParser.IsWhiteSpace(" a", 0, whitespaceLength)).IsFalse();
        await Assert.That(AnalyzerParser.CombinePathPrefix(string.Empty, UsersRoute)).IsEqualTo(UsersRoute);
        await Assert.That(AnalyzerParser.CombinePathPrefix("/", UsersRoute)).IsEqualTo(UsersRoute);
        await Assert.That(AnalyzerParser.CombinePathPrefix("/api/", UsersRoute)).IsEqualTo("/api/users");
        await Assert.That(AnalyzerParser.CombinePathPrefix("/api", string.Empty)).IsEqualTo("/api");

        AnalyzerParser.AddStaticHeader(headers, " ");
        AnalyzerParser.AddStaticHeader(headers, "X-One");
        AnalyzerParser.AddStaticHeader(headers, "X-Two: two");
        AnalyzerParser.AddStaticHeader(headers, "X-One: replaced");
        await Assert.That(headers.Count).IsEqualTo(TwoElements);
        await Assert.That(headers[0].Value).IsEqualTo("replaced");

        await Assert.That(AnalyzerParser.GetBodySerializationMethodName(0)).IsEqualTo("Default");
        await Assert.That(AnalyzerParser.GetBodySerializationMethodName(1)).IsEqualTo("Json");
        await Assert.That(AnalyzerParser.GetBodySerializationMethodName(TwoElements)).IsEqualTo(UrlEncodedSerialization);
        await Assert.That(AnalyzerParser.GetBodySerializationMethodName(SerializedSerialization)).IsEqualTo("Serialized");
        await Assert.That(AnalyzerParser.GetBodySerializationMethodName(PlaceholderEnd)).IsEqualTo("JsonLines");
        await Assert.That(AnalyzerParser.GetBodySerializationMethodName(UnsupportedSerialization)).IsEmpty();

        var headerParameter = CreateParameter(AnalyzerRequestParameterKind.Header, string.Empty);
        var unsupportedBody = CreateParameter(AnalyzerRequestParameterKind.Body, string.Empty);
        var supportedBody = CreateParameter(AnalyzerRequestParameterKind.Body, UrlEncodedSerialization);
        await Assert.That(AnalyzerParser.IsSupportedInlineBody(AnalyzerFactory.Empty<AnalyzerRequestParameterModel>())).IsTrue();
        await Assert.That(AnalyzerParser.IsSupportedInlineBody(AnalyzerFactory.FromArray([headerParameter]))).IsTrue();
        await Assert.That(AnalyzerParser.IsSupportedInlineBody(AnalyzerFactory.FromArray([unsupportedBody]))).IsFalse();
        await Assert.That(AnalyzerParser.IsSupportedInlineBody(AnalyzerFactory.FromArray([supportedBody]))).IsTrue();
        await Assert.That(AnalyzerParser.ShouldDisposeResponse("global::System.Net.Http.HttpResponseMessage")).IsFalse();
        await Assert.That(AnalyzerParser.ShouldDisposeResponse("global::System.Net.Http.HttpContent")).IsFalse();
        await Assert.That(AnalyzerParser.ShouldDisposeResponse("global::System.IO.Stream")).IsFalse();
        await Assert.That(AnalyzerParser.ShouldDisposeResponse(GlobalStringTypeName)).IsTrue();

        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(
            "namespace System.Threading.Tasks { public sealed class Marker { } } public sealed class Root { }"));
        var marker = compilation.GetTypeByMetadataName("System.Threading.Tasks.Marker")
            ?? throw new InvalidOperationException("Missing Marker symbol.");
        var root = compilation.GetTypeByMetadataName("Root")
            ?? throw new InvalidOperationException("Missing Root symbol.");
        await Assert.That(AnalyzerParser.IsInNamespace(null, "System")).IsFalse();
        await Assert.That(AnalyzerParser.IsInNamespace(marker, "System.Threading.Tasks")).IsTrue();
        await Assert.That(AnalyzerParser.IsInNamespace(marker, "System.Collections.Generic")).IsFalse();
        await Assert.That(AnalyzerParser.IsInNamespace(root, "System")).IsFalse();
    }

    /// <summary>Exercises analyzer-embedded parsing paths using real Roslyn symbols and attribute data.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmbeddedParserConsumesCompiledMethodsAndAttributesDirectly()
    {
        const string source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            [Headers("X-One: one")]
            public interface IApi
            {
                [Get("/")]
                [Headers("X-Two: two")]
                Task<string> Get([Body(true)] string body);

                [Post("/")]
                Task<string> Stream([Body(false)] string body);

                [Post("/")]
                IAsyncEnumerable<string> Serialize([Body(BodySerializationMethod.Json)] string body);

                [Get("/")]
                Task<string> Property([Property] string first, [Property("")] string second, [Property("key")] string third);
            }
            """;
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(source));
        var api = compilation.GetTypeByMetadataName("IApi") ?? throw new InvalidOperationException("Missing IApi symbol.");
        var methods = api.GetMembers().OfType<IMethodSymbol>().ToArray();
        var get = methods.Single(static method => method.Name == "Get");
        var stream = methods.Single(static method => method.Name == "Stream");
        var serialize = methods.Single(static method => method.Name == "Serialize");
        var property = methods.Single(static method => method.Name == "Property");
        var httpMethodAttribute = compilation.GetTypeByMetadataName("Refit.HttpMethodAttribute")
            ?? throw new InvalidOperationException("Missing HttpMethodAttribute symbol.");
        var formattable = compilation.GetTypeByMetadataName(FormattableMetadataName);
        await Assert.That(AnalyzerParser.CanBuildRequestInline(get, httpMethodAttribute, formattable)).IsTrue();
        await Assert.That(AnalyzerParser.DiscoverReturnTypeAdapters(compilation, null, CancellationToken.None)).IsEmpty();
        await Assert.That(AnalyzerParser.ParseRequest(get, AnalyzerReturnTypeInfo.AsyncResult, CreateAliasContext(compilation, generatedRequestBuilding: false)))
            .IsEqualTo(AnalyzerRequestModel.Empty);

        var headers = AnalyzerParser.ParseStaticHeaders(get);
        await Assert.That(headers.Count).IsEqualTo(TwoElements);
        var buffered = AnalyzerParser.ParseBodyAttribute(get.Parameters[0].GetAttributes().Single());
        var streaming = AnalyzerParser.ParseBodyAttribute(stream.Parameters[0].GetAttributes().Single());
        var serialized = AnalyzerParser.ParseBodyAttribute(serialize.Parameters[0].GetAttributes().Single());
        var boolArgument = get.Parameters[0].GetAttributes().Single().ConstructorArguments[0];
        var enumArgument = serialize.Parameters[0].GetAttributes().Single().ConstructorArguments[0];
        await Assert.That(buffered.BufferMode).IsEqualTo(AnalyzerBodyBufferMode.Buffered);
        await Assert.That(streaming.BufferMode).IsEqualTo(AnalyzerBodyBufferMode.Streaming);
        await Assert.That(serialized.SerializationMethod).IsEqualTo("Json");
        await Assert.That(AnalyzerParser.TryGetBodySerializationMethodName(boolArgument, out var noMethod)).IsFalse();
        await Assert.That(noMethod).IsEmpty();
        await Assert.That(AnalyzerParser.TryGetBodyBufferedValue(enumArgument, out var noBuffered)).IsFalse();
        await Assert.That(noBuffered).IsFalse();
        await Assert.That(AnalyzerParser.ClassifyInlineReturnShape(serialize.ReturnType)).IsEqualTo(AnalyzerReturnTypeInfo.AsyncEnumerable);
        await Assert.That(AnalyzerParser.GetReturnResultType(serialize.ReturnType).ToDisplayString()).IsEqualTo("string");
        var context = CreateAliasContext(compilation);
        await AssertPropertyParametersParse(property, context);
    }

    /// <summary>Exercises non-generic API response and empty constant-query parsing paths.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmbeddedParserHandlesNonGenericApiResponseAndEmptyQueryParts()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText("public sealed class Marker { }"));
        var apiResponse = compilation.GetTypeByMetadataName("Refit.IApiResponse")
            ?? throw new InvalidOperationException("Missing IApiResponse symbol.");

        await Assert.That(AnalyzerParser.GetDeserializedResultTypeName(apiResponse, isApiResponse: true, CreateAliasContext(compilation)))
            .IsEqualTo("global::System.Net.Http.HttpContent");
        await Assert.That(AnalyzerParser.NormalizeConstantPathForInline("/path? &=one&&")).IsEqualTo("/path");
        await Assert.That(AnalyzerParser.IsWhiteSpace(string.Empty, 0, 0)).IsTrue();
        await Assert.That(AnalyzerParser.IsPathTemplateValid("/{id/x".AsSpan())).IsFalse();

        var intType = compilation.GetSpecialType(SpecialType.System_Int32);
        var decimalType = compilation.GetSpecialType(SpecialType.System_Decimal);
        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        var markerType = compilation.GetTypeByMetadataName("Marker")
            ?? throw new InvalidOperationException("Missing Marker symbol.");
        var formattable = compilation.GetTypeByMetadataName(FormattableMetadataName);
        var spanFormattable = compilation.GetTypeByMetadataName("System.ISpanFormattable");
        await Assert.That(AnalyzerParser.ClassifyFormattable(intType, formattable, spanFormattable)).IsEqualTo((true, true));
        var fastContext = CreateAliasContext(compilation, supportsSpanEscape: true);
        var noSpanContext = CreateAliasContext(compilation, includeSpanFormattable: false);
        await Assert.That(AnalyzerParser.ComputeSpanFormattableTiers(intType, null, true, fastContext)).IsEqualTo((true, true));
        await Assert.That(AnalyzerParser.ComputeSpanFormattableTiers(intType, "X", true, fastContext)).IsEqualTo((false, true));
        await Assert.That(AnalyzerParser.ComputeSpanFormattableTiers(decimalType, null, true, fastContext)).IsEqualTo((false, true));
        await Assert.That(AnalyzerParser.ComputeSpanFormattableTiers(stringType, null, false, fastContext)).IsEqualTo((false, false));
        await Assert.That(AnalyzerParser.ComputeSpanFormattableTiers(markerType, null, true, fastContext)).IsEqualTo((false, true));
        await Assert.That(AnalyzerParser.ComputeSpanFormattableTiers(intType, null, true, noSpanContext)).IsEqualTo((false, false));
    }

    /// <summary>Exercises direct and round-trip placeholder lookup paths in the analyzer-embedded parser.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmbeddedPathLocationsFindMatchesAndReturnEmptyForMisses()
    {
        var occurrences = new[]
        {
            new AnalyzerParser.PathPlaceholderOccurrence("other", new RefitAnalyzers::System.Range(0, PlaceholderEnd)),
            new AnalyzerParser.PathPlaceholderOccurrence("**id", new RefitAnalyzers::System.Range(PlaceholderEnd, PlaceholderEnd)),
        };
        var locations = new AnalyzerParser.PathParameterLocations(occurrences, hasRoundTrip: true, hasDotted: false);
        await Assert.That(locations.TryGetDirectLocations("id", out var direct)).IsFalse();
        await Assert.That(direct).IsEmpty();
        await Assert.That(locations.TryGetRoundTripLocations("id", out var roundTrip)).IsTrue();
        await Assert.That(roundTrip.Count).IsEqualTo(1);
        await Assert.That(AnalyzerParser.PathParameterLocations.Empty.TryGetRoundTripLocations("id", out var emptyRoundTrip)).IsFalse();
        await Assert.That(emptyRoundTrip).IsEmpty();

        var dottedLocations = new AnalyzerParser.PathParameterLocations(occurrences, hasRoundTrip: true, hasDotted: true);
        await Assert.That(AnalyzerParser.HasDottedPlaceholderFor(dottedLocations, "id")).IsFalse();
    }

    /// <summary>Exercises extern-alias qualification directly against aliased Roslyn metadata references.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmbeddedAliasQualificationHandlesGlobalAndAliasOnlyReferences()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText("public sealed class LocalType { }"));
        var originalReference = compilation.References.Single(
            static reference => Path.GetFileName(reference.Display) == "Newtonsoft.Json.dll");
        var aliasOnlyReference = originalReference.WithProperties(
            MetadataReferenceProperties.Assembly.WithAliases([ExternalAlias]));
        var aliasCompilation = compilation.ReplaceReference(originalReference, aliasOnlyReference);
        var aliasAssembly = (IAssemblySymbol)(aliasCompilation.GetAssemblyOrModuleSymbol(aliasOnlyReference)
            ?? throw new InvalidOperationException("Missing aliased assembly symbol."));
        var jsonConvert = aliasAssembly.GetTypeByMetadataName("Newtonsoft.Json.JsonConvert")
            ?? throw new InvalidOperationException("Missing JsonConvert symbol.");
        var jsonToken = aliasAssembly.GetTypeByMetadataName("Newtonsoft.Json.JsonToken")
            ?? throw new InvalidOperationException("Missing JsonToken symbol.");
        var context = CreateAliasContext(aliasCompilation);

        await Assert.That(AnalyzerParser.ResolveExternAlias(aliasAssembly, aliasCompilation)).IsEqualTo(ExternalAlias);
        await Assert.That(AnalyzerParser.GetExternAlias(jsonConvert, context)).IsEqualTo(ExternalAlias);
        await Assert.That(AnalyzerParser.GetExternAlias(jsonConvert, context)).IsEqualTo(ExternalAlias);
        await Assert.That(AnalyzerParser.ContainsAliasedType(jsonConvert, context)).IsTrue();
        await Assert.That(AnalyzerParser.ContainsAliasedType(aliasCompilation.CreateArrayTypeSymbol(jsonConvert), context)).IsTrue();
        await Assert.That(AnalyzerParser.QualifyType(jsonConvert, context)).IsEqualTo("external::Newtonsoft.Json.JsonConvert");
        await Assert.That(context.ExternAliases).Contains(ExternalAlias);

        var stringType = aliasCompilation.GetSpecialType(SpecialType.System_String);
        await Assert.That(AnalyzerParser.ContainsAliasedType(stringType, context)).IsFalse();
        await Assert.That(AnalyzerParser.QualifyType(stringType, context)).IsEqualTo(StringTypeName);
        await Assert.That(AnalyzerParser.QualifyType(stringType, context)).IsEqualTo(StringTypeName);

        var listDefinition = aliasCompilation.GetTypeByMetadataName("System.Collections.Generic.List`1")
            ?? throw new InvalidOperationException("Missing List symbol.");
        var aliasedList = listDefinition.Construct(jsonConvert);
        await Assert.That(AnalyzerParser.ContainsAliasedType(aliasedList, context)).IsTrue();
        await Assert.That(AnalyzerParser.AliasedDisplay(aliasedList, context))
            .IsEqualTo("global::System.Collections.Generic.List<external::Newtonsoft.Json.JsonConvert>");
        await Assert.That(AnalyzerParser.AliasedDisplay(aliasCompilation.CreateArrayTypeSymbol(jsonConvert), context))
            .IsEqualTo("external::Newtonsoft.Json.JsonConvert[]");

        var nullableDefinition = aliasCompilation.GetSpecialType(SpecialType.System_Nullable_T);
        var nullableToken = nullableDefinition.Construct(jsonToken);
        await Assert.That(AnalyzerParser.AliasedDisplay(nullableToken, context))
            .IsEqualTo("external::Newtonsoft.Json.JsonToken?");

        var localType = aliasCompilation.GetTypeByMetadataName("LocalType")
            ?? throw new InvalidOperationException("Missing LocalType symbol.");
        var genericSource = CSharpSyntaxTree.ParseText("public sealed class Generic<T> { }");
        var genericCompilation = Fixture.CreateLibrary(genericSource);
        var typeParameter = (genericCompilation.GetTypeByMetadataName("Generic`1")
            ?? throw new InvalidOperationException("Missing Generic symbol.")).TypeParameters[0];
        await Assert.That(AnalyzerParser.AliasedDisplay(typeParameter, CreateAliasContext(genericCompilation)))
            .IsEqualTo("T");
        await Assert.That(AnalyzerParser.ClassifyInlineReturnShape(typeParameter)).IsEqualTo(AnalyzerReturnTypeInfo.Return);
        await Assert.That(AnalyzerParser.ReferencesTypeParameter(genericCompilation.CreateArrayTypeSymbol(typeParameter))).IsTrue();
        var noCompilationContext = CreateAliasContext(null);
        await Assert.That(AnalyzerParser.GetExternAlias(localType, noCompilationContext)).IsNull();
        await Assert.That(AnalyzerParser.ResolveExternAlias(aliasCompilation.Assembly, aliasCompilation)).IsNull();

        var globalReference = originalReference.WithProperties(
            MetadataReferenceProperties.Assembly.WithAliases(["global", ExternalAlias]));
        var globalCompilation = compilation.ReplaceReference(originalReference, globalReference);
        var globalAssembly = (IAssemblySymbol)(globalCompilation.GetAssemblyOrModuleSymbol(globalReference)
            ?? throw new InvalidOperationException("Missing global assembly symbol."));
        await Assert.That(AnalyzerParser.ResolveExternAlias(globalAssembly, globalCompilation)).IsNull();
    }

    /// <summary>Exercises equality and hash behavior for embedded path-location values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PathParameterLocationsUseBackingOccurrenceIdentity()
    {
        var occurrences = new[]
        {
            new AnalyzerParser.PathPlaceholderOccurrence("id", new RefitAnalyzers::System.Range(0, PlaceholderEnd)),
        };
        var first = new AnalyzerParser.PathParameterLocations(occurrences, hasRoundTrip: false, hasDotted: false);
        var same = new AnalyzerParser.PathParameterLocations(occurrences, hasRoundTrip: false, hasDotted: false);
        var other = new AnalyzerParser.PathParameterLocations([occurrences[0]], hasRoundTrip: false, hasDotted: false);
        var empty = AnalyzerParser.PathParameterLocations.Empty;

        await Assert.That(first == same).IsTrue();
        await Assert.That(first != other).IsTrue();
        await Assert.That(first.Equals((object)same)).IsTrue();
        await Assert.That(first.Equals("not locations")).IsFalse();
        await Assert.That(first.GetHashCode()).IsEqualTo(same.GetHashCode());
        await Assert.That(empty.GetHashCode()).IsEqualTo(0);
    }

    /// <summary>Creates one analyzer-embedded request parameter model.</summary>
    /// <param name="kind">The request binding kind.</param>
    /// <param name="serializationMethod">The body serialization method.</param>
    /// <returns>The request parameter.</returns>
    private static AnalyzerRequestParameterModel CreateParameter(
        AnalyzerRequestParameterKind kind,
        string serializationMethod) =>
        new(
            "parameter",
            StringTypeName,
            null,
            AnalyzerFactory.Empty<AnalyzerParameterAttributeModel>(),
            kind,
            CanBeNull: false,
            string.Empty,
            string.Empty,
            serializationMethod,
            AnalyzerBodyBufferMode.Buffered);

    /// <summary>Asserts that every compiled property parameter is parsed by the embedded analyzer parser.</summary>
    /// <param name="method">The method containing property parameters.</param>
    /// <param name="context">The analyzer parser context.</param>
    /// <returns>A task representing the asynchronous assertions.</returns>
    private static async Task AssertPropertyParametersParse(
        IMethodSymbol method,
        AnalyzerInterfaceGenerationContext context)
    {
        for (var i = 0; i < method.Parameters.Length; i++)
        {
            await Assert.That(AnalyzerParser.TryParsePropertyParameter(method.Parameters[i], "string", context, out _)).IsTrue();
        }
    }

    /// <summary>Creates an analyzer-embedded generation context for alias qualification.</summary>
    /// <param name="compilation">The compilation used to resolve metadata aliases, or null.</param>
    /// <param name="generatedRequestBuilding">Whether generated request construction is enabled.</param>
    /// <param name="supportsSpanEscape">Whether span-based URL escaping is available.</param>
    /// <param name="includeSpanFormattable">Whether the span-formattable symbol is available.</param>
    /// <returns>The generation context.</returns>
    private static AnalyzerInterfaceGenerationContext CreateAliasContext(
        CSharpCompilation? compilation,
        bool generatedRequestBuilding = true,
        bool supportsSpanEscape = false,
        bool includeSpanFormattable = true)
    {
        var symbolCompilation = compilation ?? Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(string.Empty));
        var httpMethodAttribute = symbolCompilation.GetTypeByMetadataName("Refit.HttpMethodAttribute")
            ?? throw new InvalidOperationException("Missing HttpMethodAttribute symbol.");
        return new(
            [],
            string.Empty,
            string.Empty,
            null,
            httpMethodAttribute,
            symbolCompilation.GetTypeByMetadataName(FormattableMetadataName),
            includeSpanFormattable
                ? symbolCompilation.GetTypeByMetadataName("System.ISpanFormattable")
                : null,
            SupportsSpanEscape: supportsSpanEscape,
            GeneratedRequestBuilding: generatedRequestBuilding,
            EmitGeneratedCodeMarkers: false,
            SupportsNullable: false,
            SupportsStaticLambdas: false,
            SupportsCollectionExpressions: false,
            compilation,
            null,
            [],
            [],
            new(SymbolEqualityComparer.Default),
            new(SymbolEqualityComparer.Default),
            new(SymbolEqualityComparer.Default));
    }
}
