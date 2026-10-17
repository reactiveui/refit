// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

extern alias RefitAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using AnalyzerAssemblyAliasQualification = RefitAnalyzers::Refit.Generator.AssemblyAliasQualification;
using AnalyzerBodyBufferMode = RefitAnalyzers::Refit.Generator.BodyBufferMode;
using AnalyzerHeader = RefitAnalyzers::Refit.Generator.HeaderModel;
using AnalyzerInlineRequestClassification = RefitAnalyzers::Refit.Generator.InlineRequestClassification;
using AnalyzerInterfaceGenerationContext = RefitAnalyzers::Refit.Generator.InterfaceGenerationContext;
using AnalyzerQueryParameterParsing = RefitAnalyzers::Refit.Generator.QueryParameterParsing;
using AnalyzerRequestBodyParsing = RefitAnalyzers::Refit.Generator.RequestBodyParsing;
using AnalyzerRequestModel = RefitAnalyzers::Refit.Generator.RequestModel;
using AnalyzerRequestParameterKind = RefitAnalyzers::Refit.Generator.RequestParameterKind;
using AnalyzerRequestParameterKindParsing = RefitAnalyzers::Refit.Generator.RequestParameterKindParsing;
using AnalyzerRequestParameterModel = RefitAnalyzers::Refit.Generator.RequestParameterModel;
using AnalyzerRequestParameterParsingHelpers = RefitAnalyzers::Refit.Generator.RequestParameterParsingHelpers;
using AnalyzerRequestParsing = RefitAnalyzers::Refit.Generator.RequestParsing;
using AnalyzerReturnTypeAdapterParsing = RefitAnalyzers::Refit.Generator.ReturnTypeAdapterParsing;
using AnalyzerReturnTypeInfo = RefitAnalyzers::Refit.Generator.ReturnTypeInfo;
using AnalyzerStringArrayComparer = RefitAnalyzers::Refit.Generator.ArraySequenceComparer<string>;
using AnalyzerTypeExtensions = RefitAnalyzers::Refit.Generator.ITypeSymbolExtensions;
using GenerationCachesModel = RefitAnalyzers::Refit.Generator.GenerationCachesModel;
using GenerationCapabilitiesModel = RefitAnalyzers::Refit.Generator.GenerationCapabilitiesModel;
using GenerationOptionsModel = RefitAnalyzers::Refit.Generator.GenerationOptionsModel;
using GenerationSymbolsModel = RefitAnalyzers::Refit.Generator.GenerationSymbolsModel;
using RequestBindingsModel = RefitAnalyzers::Refit.Generator.RequestBindingsModel;
using RequestBodyModel = RefitAnalyzers::Refit.Generator.RequestBodyModel;
using RequestGenerationModel = RefitAnalyzers::Refit.Generator.RequestGenerationModel;
using RequestParameterBodyModel = RefitAnalyzers::Refit.Generator.RequestParameterBodyModel;
using RequestParameterHeaderModel = RefitAnalyzers::Refit.Generator.RequestParameterHeaderModel;
using RequestParameterIdentityModel = RefitAnalyzers::Refit.Generator.RequestParameterIdentityModel;
using RequestParameterPathModel = RefitAnalyzers::Refit.Generator.RequestParameterPathModel;
using RequestReturnModel = RefitAnalyzers::Refit.Generator.RequestReturnModel;

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

    /// <summary>Verifies analyzer-embedded raw arrays compare by contents.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
public async Task RawArraysUseStructuralEquality()
{
    string[] first = ["one", "two"];
    string[] equal = ["one", "two"];
    var comparer = AnalyzerStringArrayComparer.Instance;
    await Assert.That(comparer.Equals(first, equal)).IsTrue();
    await Assert.That(comparer.GetHashCode(first)).IsEqualTo(comparer.GetHashCode(equal));
    await Assert.That(comparer.Equals(first, ["one", "three"])).IsFalse();
    await Assert.That(comparer.Equals(first, ["one"])).IsFalse();
    await Assert.That(comparer.Equals([], [])).IsTrue();
}

    /// <summary>Exercises the embedded request records and their initialized properties.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestModelsRetainAllValues()
    {
        var header = new AnalyzerHeader("X-Test", "value");
        AnalyzerHeader[] headers = [header];
        AnalyzerRequestParameterModel[] parameters = [];
        var model = new AnalyzerRequestModel(
            "GET",
            "/items",
            new RequestReturnModel(StringTypeName, StringTypeName, true, false),
            new RequestGenerationModel(true, "Adapter", default),
            new RequestBindingsModel(headers, parameters),
            new RequestBodyModel(true, "boundary")) { QueryUriFormat = 1, TimeoutMilliseconds = TwoElements };
        await Assert.That(header.Name).IsEqualTo("X-Test");
        await Assert.That(header.Value).IsEqualTo("value");
        await Assert.That(model.HttpMethod).IsEqualTo("GET");
        await Assert.That(model.Path).IsEqualTo("/items");
        await Assert.That(model.Return.ResultType).IsEqualTo(StringTypeName);
        await Assert.That(model.Return.DeserializedResultType).IsEqualTo(StringTypeName);
        await Assert.That(model.Return.IsApiResponse).IsTrue();
        await Assert.That(model.Return.ShouldDisposeResponse).IsFalse();
        await Assert.That(model.Generation.CanGenerateInline).IsTrue();
        await Assert.That(model.Generation.AdapterTypeExpression).IsEqualTo("Adapter");
        await Assert.That(model.Bindings.StaticHeaders[0]).IsEqualTo(header);
        await Assert.That(model.Bindings.Parameters).IsEmpty();
        await Assert.That(model.Body.IsMultipart).IsTrue();
        await Assert.That(model.Body.MultipartBoundary).IsEqualTo("boundary");
        await Assert.That(model.QueryUriFormat).IsEqualTo(1);
        await Assert.That(model.TimeoutMilliseconds).IsEqualTo(TwoElements);
        await Assert.That(AnalyzerRequestModel.Empty.Path).IsEmpty();
    }

    /// <summary>Exercises embedded type hierarchy checks with base classes, interfaces, and unrelated types.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    /// <exception cref = "InvalidOperationException">The test compilation is missing the <c>Derived</c>, <c>Base</c>, or <c>IMarker</c> symbol.</exception>
    [Test]
    public async Task TypeHierarchyChecksIncludeInterfacesOnlyWhenRequested()
    {
        var compilation = CSharpCompilation.Create("hierarchy", [CSharpSyntaxTree.ParseText("interface IFirst { } interface IMarker { } class Base { } class Derived : Base, IFirst, IMarker { }")]);
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
    /// <exception cref = "InvalidOperationException">The test compilation is missing the <c>Marker</c> or <c>Root</c> symbol.</exception>
    [Test]
    public async Task EmbeddedParserHelpersClassifyPathsBodiesAndHeaders()
    {
        const string path = "/path";
        const int whitespaceLength = 2;
        List<AnalyzerHeader> headers = [];
        await Assert.That(AnalyzerRequestParameterParsingHelpers.NormalizeConstantPathForInline(path)).IsEqualTo(path);
        await Assert.That(AnalyzerRequestParameterParsingHelpers.NormalizeConstantPathForInline("/path?")).IsEqualTo(path);
        await Assert.That(AnalyzerRequestParameterParsingHelpers.NormalizeConstantPathForInline("/path?one=1&&two=2#fragment")).IsEqualTo("/path?one=1&two=2");
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsPathSupported(string.Empty)).IsTrue();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsPathSupported("/{id}")).IsTrue();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsPathSupported("/id}")).IsFalse();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsPathSupported("/line\nbreak")).IsFalse();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsPathSupported("/line\rbreak")).IsFalse();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsWhiteSpace(" \t", 0, whitespaceLength)).IsTrue();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsWhiteSpace(" a", 0, whitespaceLength)).IsFalse();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.CombinePathPrefix(string.Empty, UsersRoute)).IsEqualTo(UsersRoute);
        await Assert.That(AnalyzerRequestParameterParsingHelpers.CombinePathPrefix("/", UsersRoute)).IsEqualTo(UsersRoute);
        await Assert.That(AnalyzerRequestParameterParsingHelpers.CombinePathPrefix("/api/", UsersRoute)).IsEqualTo("/api/users");
        await Assert.That(AnalyzerRequestParameterParsingHelpers.CombinePathPrefix("/api", string.Empty)).IsEqualTo("/api");
        AnalyzerRequestParameterParsingHelpers.AddStaticHeader(headers, " ");
        AnalyzerRequestParameterParsingHelpers.AddStaticHeader(headers, "X-One");
        AnalyzerRequestParameterParsingHelpers.AddStaticHeader(headers, "X-Two: two");
        AnalyzerRequestParameterParsingHelpers.AddStaticHeader(headers, "X-One: replaced");
        await Assert.That(headers.Count).IsEqualTo(TwoElements);
        await Assert.That(headers[0].Value).IsEqualTo("replaced");
        await Assert.That(AnalyzerRequestParameterParsingHelpers.GetBodySerializationMethodName(0)).IsEqualTo("Default");
        await Assert.That(AnalyzerRequestParameterParsingHelpers.GetBodySerializationMethodName(1)).IsEqualTo("Json");
        await Assert.That(AnalyzerRequestParameterParsingHelpers.GetBodySerializationMethodName(TwoElements)).IsEqualTo(UrlEncodedSerialization);
        await Assert.That(AnalyzerRequestParameterParsingHelpers.GetBodySerializationMethodName(SerializedSerialization)).IsEqualTo("Serialized");
        await Assert.That(AnalyzerRequestParameterParsingHelpers.GetBodySerializationMethodName(PlaceholderEnd)).IsEqualTo("JsonLines");
        await Assert.That(AnalyzerRequestParameterParsingHelpers.GetBodySerializationMethodName(UnsupportedSerialization)).IsEmpty();
        var headerParameter = CreateParameter(AnalyzerRequestParameterKind.Header, string.Empty);
        var unsupportedBody = CreateParameter(AnalyzerRequestParameterKind.Body, string.Empty);
        var supportedBody = CreateParameter(AnalyzerRequestParameterKind.Body, UrlEncodedSerialization);
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsSupportedInlineBody([])).IsTrue();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsSupportedInlineBody((AnalyzerRequestParameterModel[])[headerParameter])).IsTrue();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsSupportedInlineBody((AnalyzerRequestParameterModel[])[unsupportedBody])).IsFalse();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsSupportedInlineBody((AnalyzerRequestParameterModel[])[supportedBody])).IsTrue();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.ShouldDisposeResponse("global::System.Net.Http.HttpResponseMessage")).IsFalse();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.ShouldDisposeResponse("global::System.Net.Http.HttpContent")).IsFalse();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.ShouldDisposeResponse("global::System.IO.Stream")).IsFalse();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.ShouldDisposeResponse(GlobalStringTypeName)).IsTrue();
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText("namespace System.Threading.Tasks { public sealed class Marker { } } public sealed class Root { }"));
        var marker = compilation.GetTypeByMetadataName("System.Threading.Tasks.Marker") ?? throw new InvalidOperationException("Missing Marker symbol.");
        var root = compilation.GetTypeByMetadataName("Root") ?? throw new InvalidOperationException("Missing Root symbol.");
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsInNamespace(null, "System")).IsFalse();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsInNamespace(marker, "System.Threading.Tasks")).IsTrue();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsInNamespace(marker, "System.Collections.Generic")).IsFalse();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsInNamespace(root, "System")).IsFalse();
    }

    /// <summary>Exercises analyzer-embedded parsing paths using real Roslyn symbols and attribute data.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    /// <exception cref = "InvalidOperationException">The test compilation is missing the <c>IApi</c> or <c>HttpMethodAttribute</c> symbol.</exception>
    [Test]
    public async Task EmbeddedParserConsumesCompiledMethodsAndAttributesDirectly()
    {
        const string source = """
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
        var httpMethodAttribute = compilation.GetTypeByMetadataName("Refit.HttpMethodAttribute") ?? throw new InvalidOperationException("Missing HttpMethodAttribute symbol.");
        var formattable = compilation.GetTypeByMetadataName(FormattableMetadataName);
        await Assert.That(AnalyzerInlineRequestClassification.CanBuildRequestInline(get, httpMethodAttribute, formattable)).IsTrue();
        await Assert.That(AnalyzerReturnTypeAdapterParsing.DiscoverReturnTypeAdapters(compilation, null, CancellationToken.None)).IsEmpty();
        await Assert.That(AnalyzerRequestParsing.ParseRequest(get, AnalyzerReturnTypeInfo.AsyncResult, CreateAliasContext(compilation, generatedRequestBuilding: false)))
            .IsEqualTo(AnalyzerRequestModel.Empty);
        var headers = AnalyzerRequestParsing.ParseStaticHeaders(get);
        await Assert.That(headers.Count).IsEqualTo(TwoElements);
        var buffered = AnalyzerRequestBodyParsing.ParseBodyAttribute(get.Parameters[0].GetAttributes().Single());
        var streaming = AnalyzerRequestBodyParsing.ParseBodyAttribute(stream.Parameters[0].GetAttributes().Single());
        var serialized = AnalyzerRequestBodyParsing.ParseBodyAttribute(serialize.Parameters[0].GetAttributes().Single());
        var boolArgument = get.Parameters[0].GetAttributes().Single().ConstructorArguments[0];
        var enumArgument = serialize.Parameters[0].GetAttributes().Single().ConstructorArguments[0];
        await Assert.That(buffered.BufferMode).IsEqualTo(AnalyzerBodyBufferMode.Buffered);
        await Assert.That(streaming.BufferMode).IsEqualTo(AnalyzerBodyBufferMode.Streaming);
        await Assert.That(serialized.SerializationMethod).IsEqualTo("Json");
        await Assert.That(AnalyzerRequestBodyParsing.TryGetBodySerializationMethodName(boolArgument, out var noMethod)).IsFalse();
        await Assert.That(noMethod).IsEmpty();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.TryGetBodyBufferedValue(enumArgument, out var noBuffered)).IsFalse();
        await Assert.That(noBuffered).IsFalse();
        await Assert.That(AnalyzerInlineRequestClassification.ClassifyInlineReturnShape(serialize.ReturnType)).IsEqualTo(AnalyzerReturnTypeInfo.AsyncEnumerable);
        await Assert.That(AnalyzerRequestParsing.GetReturnResultType(serialize.ReturnType).ToDisplayString()).IsEqualTo("string");
        var context = CreateAliasContext(compilation);
        await AssertPropertyParametersParse(property, context);
    }

    /// <summary>Exercises non-generic API response and empty constant-query parsing paths.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    /// <exception cref = "InvalidOperationException">The test compilation is missing the <c>IApiResponse</c> or <c>Marker</c> symbol.</exception>
    [Test]
    public async Task EmbeddedParserHandlesNonGenericApiResponseAndEmptyQueryParts()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText("public sealed class Marker { }"));
        var apiResponse = compilation.GetTypeByMetadataName("Refit.IApiResponse") ?? throw new InvalidOperationException("Missing IApiResponse symbol.");
        await Assert.That(AnalyzerRequestParsing.GetDeserializedResultTypeName(apiResponse, isApiResponse: true, CreateAliasContext(compilation)))
            .IsEqualTo("global::System.Net.Http.HttpContent");
        await Assert.That(AnalyzerRequestParameterParsingHelpers.NormalizeConstantPathForInline("/path? &=one&&")).IsEqualTo("/path");
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsWhiteSpace(string.Empty, 0, 0)).IsTrue();
        await Assert.That(AnalyzerRequestParameterParsingHelpers.IsPathTemplateValid("/{id/x".AsSpan())).IsFalse();
        var intType = compilation.GetSpecialType(SpecialType.System_Int32);
        var decimalType = compilation.GetSpecialType(SpecialType.System_Decimal);
        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        var markerType = compilation.GetTypeByMetadataName("Marker") ?? throw new InvalidOperationException("Missing Marker symbol.");
        var formattable = compilation.GetTypeByMetadataName(FormattableMetadataName);
        var spanFormattable = compilation.GetTypeByMetadataName("System.ISpanFormattable");
        await Assert.That(AnalyzerQueryParameterParsing.ClassifyFormattable(intType, formattable, spanFormattable)).IsEqualTo((true, true));
        var fastContext = CreateAliasContext(compilation, supportsSpanEscape: true);
        var noSpanContext = CreateAliasContext(compilation, includeSpanFormattable: false);
        await Assert.That(AnalyzerQueryParameterParsing.ComputeSpanFormattableTiers(intType, null, true, fastContext)).IsEqualTo((true, true));
        await Assert.That(AnalyzerQueryParameterParsing.ComputeSpanFormattableTiers(intType, "X", true, fastContext)).IsEqualTo((false, true));
        await Assert.That(AnalyzerQueryParameterParsing.ComputeSpanFormattableTiers(decimalType, null, true, fastContext)).IsEqualTo((false, true));
        await Assert.That(AnalyzerQueryParameterParsing.ComputeSpanFormattableTiers(stringType, null, false, fastContext)).IsEqualTo((false, false));
        await Assert.That(AnalyzerQueryParameterParsing.ComputeSpanFormattableTiers(markerType, null, true, fastContext)).IsEqualTo((false, true));
        await Assert.That(AnalyzerQueryParameterParsing.ComputeSpanFormattableTiers(intType, null, true, noSpanContext)).IsEqualTo((false, false));
    }

    /// <summary>Exercises direct and round-trip placeholder lookup paths in the analyzer-embedded parser.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmbeddedPathLocationsFindMatchesAndReturnEmptyForMisses()
    {
        AnalyzerRequestParsing.PathPlaceholderOccurrence[] occurrences =
        [
            new AnalyzerRequestParsing.PathPlaceholderOccurrence("other", new RefitAnalyzers::System.Range(0, PlaceholderEnd)),
            new AnalyzerRequestParsing.PathPlaceholderOccurrence("**id", new RefitAnalyzers::System.Range(PlaceholderEnd, PlaceholderEnd)),
        ];
        var locations = new AnalyzerRequestParsing.PathParameterLocations(occurrences, hasRoundTrip: true, hasDotted: false);
        await Assert.That(locations.TryGetDirectLocations("id", out var direct)).IsFalse();
        await Assert.That(direct).IsEmpty();
        await Assert.That(locations.TryGetRoundTripLocations("id", out var roundTrip)).IsTrue();
        await Assert.That(roundTrip.Length).IsEqualTo(1);
        await Assert.That(AnalyzerRequestParsing.PathParameterLocations.Empty.TryGetRoundTripLocations("id", out var emptyRoundTrip)).IsFalse();
        await Assert.That(emptyRoundTrip).IsEmpty();
        var dottedLocations = new AnalyzerRequestParsing.PathParameterLocations(occurrences, hasRoundTrip: true, hasDotted: true);
        await Assert.That(AnalyzerQueryParameterParsing.HasDottedPlaceholderFor(dottedLocations, "id")).IsFalse();
    }

    /// <summary>Exercises extern-alias qualification directly against aliased Roslyn metadata references.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    /// <exception cref = "InvalidOperationException">A required symbol is missing from the test compilation.</exception>
    [Test]
    public async Task EmbeddedAliasQualificationHandlesGlobalAndAliasOnlyReferences()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText("public sealed class LocalType { }"));
        var originalReference = compilation.References.Single(static reference => Path.GetFileName(reference.Display) == "Newtonsoft.Json.dll");
        var aliasOnlyReference = originalReference.WithProperties(MetadataReferenceProperties.Assembly.WithAliases([ExternalAlias]));
        var aliasCompilation = compilation.ReplaceReference(originalReference, aliasOnlyReference);
        var aliasAssembly = (IAssemblySymbol)(aliasCompilation.GetAssemblyOrModuleSymbol(aliasOnlyReference) ?? throw new InvalidOperationException("Missing aliased assembly symbol."));
        var jsonConvert = aliasAssembly.GetTypeByMetadataName("Newtonsoft.Json.JsonConvert") ?? throw new InvalidOperationException("Missing JsonConvert symbol.");
        var jsonToken = aliasAssembly.GetTypeByMetadataName("Newtonsoft.Json.JsonToken") ?? throw new InvalidOperationException("Missing JsonToken symbol.");
        var context = CreateAliasContext(aliasCompilation);
        await Assert.That(AnalyzerAssemblyAliasQualification.ResolveExternAlias(aliasAssembly, aliasCompilation)).IsEqualTo(ExternalAlias);
        await Assert.That(AnalyzerAssemblyAliasQualification.GetExternAlias(jsonConvert, context)).IsEqualTo(ExternalAlias);
        await Assert.That(AnalyzerAssemblyAliasQualification.GetExternAlias(jsonConvert, context)).IsEqualTo(ExternalAlias);
        await Assert.That(AnalyzerAssemblyAliasQualification.ContainsAliasedType(jsonConvert, context)).IsTrue();
        await Assert.That(AnalyzerAssemblyAliasQualification.ContainsAliasedType(aliasCompilation.CreateArrayTypeSymbol(jsonConvert), context)).IsTrue();
        await Assert.That(AnalyzerAssemblyAliasQualification.QualifyType(jsonConvert, context)).IsEqualTo("external::Newtonsoft.Json.JsonConvert");
        await Assert.That(context.Caches.ExternAliases).Contains(ExternalAlias);
        var stringType = aliasCompilation.GetSpecialType(SpecialType.System_String);
        await Assert.That(AnalyzerAssemblyAliasQualification.ContainsAliasedType(stringType, context)).IsFalse();
        await Assert.That(AnalyzerAssemblyAliasQualification.QualifyType(stringType, context)).IsEqualTo(StringTypeName);
        await Assert.That(AnalyzerAssemblyAliasQualification.QualifyType(stringType, context)).IsEqualTo(StringTypeName);
        var listDefinition = aliasCompilation.GetTypeByMetadataName("System.Collections.Generic.List`1") ?? throw new InvalidOperationException("Missing List symbol.");
        var aliasedList = listDefinition.Construct(jsonConvert);
        await Assert.That(AnalyzerAssemblyAliasQualification.ContainsAliasedType(aliasedList, context)).IsTrue();
        await Assert.That(AnalyzerAssemblyAliasQualification.AliasedDisplay(aliasedList, context)).IsEqualTo("global::System.Collections.Generic.List<external::Newtonsoft.Json.JsonConvert>");
        await Assert.That(AnalyzerAssemblyAliasQualification.AliasedDisplay(aliasCompilation.CreateArrayTypeSymbol(jsonConvert), context)).IsEqualTo("external::Newtonsoft.Json.JsonConvert[]");
        var nullableDefinition = aliasCompilation.GetSpecialType(SpecialType.System_Nullable_T);
        var nullableToken = nullableDefinition.Construct(jsonToken);
        await Assert.That(AnalyzerAssemblyAliasQualification.AliasedDisplay(nullableToken, context)).IsEqualTo("external::Newtonsoft.Json.JsonToken?");
        var localType = aliasCompilation.GetTypeByMetadataName("LocalType") ?? throw new InvalidOperationException("Missing LocalType symbol.");
        var genericSource = CSharpSyntaxTree.ParseText("public sealed class Generic<T> { }");
        var genericCompilation = Fixture.CreateLibrary(genericSource);
        var typeParameter = (genericCompilation.GetTypeByMetadataName("Generic`1") ?? throw new InvalidOperationException("Missing Generic symbol.")).TypeParameters[0];
        await Assert.That(AnalyzerAssemblyAliasQualification.AliasedDisplay(typeParameter, CreateAliasContext(genericCompilation))).IsEqualTo("T");
        await Assert.That(AnalyzerInlineRequestClassification.ClassifyInlineReturnShape(typeParameter)).IsEqualTo(AnalyzerReturnTypeInfo.Return);
        await Assert.That(AnalyzerRequestParameterKindParsing.ReferencesTypeParameter(genericCompilation.CreateArrayTypeSymbol(typeParameter))).IsTrue();
        var noCompilationContext = CreateAliasContext(null);
        await Assert.That(AnalyzerAssemblyAliasQualification.GetExternAlias(localType, noCompilationContext)).IsNull();
        await Assert.That(AnalyzerAssemblyAliasQualification.ResolveExternAlias(aliasCompilation.Assembly, aliasCompilation)).IsNull();
        var globalReference = originalReference.WithProperties(MetadataReferenceProperties.Assembly.WithAliases(["global", ExternalAlias]));
        var globalCompilation = compilation.ReplaceReference(originalReference, globalReference);
        var globalAssembly = (IAssemblySymbol)(globalCompilation.GetAssemblyOrModuleSymbol(globalReference) ?? throw new InvalidOperationException("Missing global assembly symbol."));
        await Assert.That(AnalyzerAssemblyAliasQualification.ResolveExternAlias(globalAssembly, globalCompilation)).IsNull();
    }

    /// <summary>Exercises equality and hash behavior for embedded path-location values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PathParameterLocationsUseBackingOccurrenceIdentity()
    {
        AnalyzerRequestParsing.PathPlaceholderOccurrence[] occurrences =
        [
            new AnalyzerRequestParsing.PathPlaceholderOccurrence("id", new RefitAnalyzers::System.Range(0, PlaceholderEnd)),
        ];
        var first = new AnalyzerRequestParsing.PathParameterLocations(occurrences, hasRoundTrip: false, hasDotted: false);
        var same = new AnalyzerRequestParsing.PathParameterLocations(occurrences, hasRoundTrip: false, hasDotted: false);
        var other = new AnalyzerRequestParsing.PathParameterLocations([occurrences[0]], hasRoundTrip: false, hasDotted: false);
        var empty = AnalyzerRequestParsing.PathParameterLocations.Empty;
        await Assert.That(first == same).IsTrue();
        await Assert.That(first != other).IsTrue();
        await Assert.That(first.Equals((object)same)).IsTrue();
        await Assert.That(first.Equals("not locations")).IsFalse();
        await Assert.That(first.GetHashCode()).IsEqualTo(same.GetHashCode());
        await Assert.That(empty.GetHashCode()).IsEqualTo(0);
    }

    /// <summary>Creates one analyzer-embedded request parameter model.</summary>
    /// <param name = "kind">The request binding kind.</param>
    /// <param name = "serializationMethod">The body serialization method.</param>
    /// <returns>The request parameter.</returns>
    private static AnalyzerRequestParameterModel CreateParameter(AnalyzerRequestParameterKind kind, string serializationMethod) =>new(
        new RequestParameterIdentityModel("parameter", StringTypeName, false),
        new RequestParameterPathModel(null, default, default, default, default),
        [],
        kind,
        new RequestParameterHeaderModel(string.Empty, null),
        string.Empty,
        new RequestParameterBodyModel(serializationMethod, AnalyzerBodyBufferMode.Buffered, default, default, default, default, default, default));

    /// <summary>Asserts that every compiled property parameter is parsed by the embedded analyzer parser.</summary>
    /// <param name = "method">The method containing property parameters.</param>
    /// <param name = "context">The analyzer parser context.</param>
    /// <returns>A task representing the asynchronous assertions.</returns>
    private static async Task AssertPropertyParametersParse(IMethodSymbol method, AnalyzerInterfaceGenerationContext context)
    {
        for (var i = 0; i < method.Parameters.Length; i++)
        {
            await Assert.That(AnalyzerRequestParameterKindParsing.TryParsePropertyParameter(method.Parameters[i], "string", context, out _)).IsTrue();
        }
    }

    /// <summary>Creates an analyzer-embedded generation context for alias qualification.</summary>
    /// <param name = "compilation">The compilation used to resolve metadata aliases, or null.</param>
    /// <param name = "generatedRequestBuilding">Whether generated request construction is enabled.</param>
    /// <param name = "supportsSpanEscape">Whether span-based URL escaping is available.</param>
    /// <param name = "includeSpanFormattable">Whether the span-formattable symbol is available.</param>
    /// <returns>The generation context.</returns>
    /// <exception cref = "InvalidOperationException"><paramref name = "compilation"/> (or the empty fallback compilation) does not reference the <c>Refit.HttpMethodAttribute</c> symbol.</exception>
    private static AnalyzerInterfaceGenerationContext CreateAliasContext(
        CSharpCompilation? compilation,
        bool generatedRequestBuilding = true,
        bool supportsSpanEscape = false,
        bool includeSpanFormattable = true)
    {
        var symbolCompilation = compilation ?? Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(string.Empty));
        var httpMethodAttribute = symbolCompilation.GetTypeByMetadataName("Refit.HttpMethodAttribute") ?? throw new InvalidOperationException("Missing HttpMethodAttribute symbol.");
        return new(
            [],
            new GenerationOptionsModel(string.Empty, string.Empty, generatedRequestBuilding, false),
            new GenerationSymbolsModel(
                null,
                httpMethodAttribute,
                symbolCompilation.GetTypeByMetadataName(FormattableMetadataName),
                includeSpanFormattable ? symbolCompilation.GetTypeByMetadataName("System.ISpanFormattable") : null,
                compilation,
                null,
                [],
                null),
            new GenerationCapabilitiesModel(supportsSpanEscape, false, false, false),
            new GenerationCachesModel([], new(comparer: SymbolEqualityComparer.Default), new(comparer: SymbolEqualityComparer.Default), new(comparer: SymbolEqualityComparer.Default)),
            string.Empty);
    }
}
