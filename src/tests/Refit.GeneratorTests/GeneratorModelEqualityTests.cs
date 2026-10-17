// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Verifies raw model arrays retain structural incremental-cache equality.</summary>
public class GeneratorModelEqualityTests
{
    /// <summary>The request header compared in nested model tests.</summary>
    private const string HeaderName = "Accept";

    /// <summary>The scalar type used in query model bindings.</summary>
    private const string StringTypeName = "string";

    /// <summary>The prefix used in nested query bindings.</summary>
    private const string QueryPrefix = "prefix.";

    /// <summary>The enum member used in formatting bindings.</summary>
    private const string AscendingMember = "Ascending";

    /// <summary>The attribute type used in parameter bindings.</summary>
    private const string AttributeTypeExpression = "global::Attribute";

    /// <summary>The source expression used for a constructor argument.</summary>
    private const string AttributeValueExpression = "\"value\"";

    /// <summary>The constraint type used in generic method bindings.</summary>
    private const string DisposableTypeName = "global::System.IDisposable";

    /// <summary>The parameter name used in method bindings.</summary>
    private const string ParameterName = "value";

    /// <summary>The header value used in request bindings.</summary>
    private const string JsonMediaType = "application/json";

    /// <summary>The changed method or property name.</summary>
    private const string OtherMemberName = "Other";

    /// <summary>The changed query or parameter name.</summary>
    private const string OtherParameterName = "other";

    /// <summary>The member access used in paging continuations.</summary>
    private const string NextAccess = ".Next";

    /// <summary>The changed attribute argument expression.</summary>
    private const string OtherAttributeValueExpression = "\"other\"";

    /// <summary>Verifies equal sequences in different arrays have matching comparisons and hashes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Equals_SameSequence_MatchesHash()
    {
        string[] left = ["a", "b"];
        string[] right = ["a", "b"];
        var comparer = ArraySequenceComparer<string>.Instance;

        await Assert.That(comparer.Equals(left, right)).IsTrue();
        await Assert.That(comparer.GetHashCode(left)).IsEqualTo(comparer.GetHashCode(right));
        await Assert.That(comparer.Equals(left, left)).IsTrue();
    }

    /// <summary>Verifies changed elements, lengths and optional-array presence invalidate equality.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Equals_DifferentSequence_IsFalse()
    {
        string[] values = ["a", "b"];
        var comparer = ArraySequenceComparer<string>.Instance;

        await Assert.That(comparer.Equals(values, ["a", "c"])).IsFalse();
        await Assert.That(comparer.Equals(values, ["a"])).IsFalse();
        await Assert.That(comparer.Equals(values, null)).IsFalse();
        await Assert.That(comparer.Equals(null, values)).IsFalse();
        await Assert.That(comparer.Equals(null, [])).IsFalse();
        await Assert.That(comparer.Equals(null, null)).IsTrue();
    }

    /// <summary>Verifies default required arrays compare and hash like empty arrays.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequiredArrays_DefaultAndEmpty_AreEqual()
    {
        await Assert.That(GeneratorModelEquality.SequenceEquals<string>(null, [])).IsTrue();
        await Assert.That(GeneratorModelEquality.SequenceEquals<string>([], null)).IsTrue();
        await Assert.That(GeneratorModelEquality.SequenceEquals(null, [ParameterName])).IsFalse();
        await Assert.That(GeneratorModelEquality.GetSequenceHashCode<string>(null)).IsEqualTo(GeneratorModelEquality.GetSequenceHashCode<string>([]));
        await Assert.That(GeneratorModelEquality.GetHashCode<string?>(null)).IsEqualTo(0);
    }

    /// <summary>Verifies nested request bindings compare header contents instead of array identities.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestBindings_CompareNestedContents()
    {
        var left = new RequestBindingsModel([new(HeaderName, JsonMediaType)], []);
        var right = new RequestBindingsModel([new(HeaderName, JsonMediaType)], []);
        var changed = new RequestBindingsModel([new(HeaderName, "text/plain")], []);

        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
        await Assert.That(left.Equals(changed)).IsFalse();
        await Assert.That(default(RequestBindingsModel).Equals(new([], []))).IsTrue();
    }

    /// <summary>Verifies every query-property binding participates in structural equality.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryObjectProperty_EqualsAndHashCode_CompareAllBindings()
    {
        var format = new InlineValueFormatModel(InlineFormatKind.String, null, StringTypeName, false, null);
        var nested = new QueryObjectPropertyModel(new("Child", null, null, null), false, true, null, format);
        var dictionary = new QueryDictionaryModel(StringTypeName, format, true, QueryPrefix, [nested]);
        var left = new QueryObjectPropertyModel(
            new("Name", "name", "jsonName", QueryPrefix),
            true,
            true,
            "G",
            format,
            new(null, true, "string[]"),
            [nested],
            true,
            dictionary);
        var right = left with
        {
            ValueFormat = format with { },
            Collection = left.Collection! with { },
            Nested = [nested with { ValueFormat = format with { } }],
            Dictionary = dictionary with { ValueProperties = [nested with { }] }
        };

        await VerifyEqualityAsync(
            left,
            right,
            [
            left with { Names = left.Names with { ClrName = OtherMemberName } },
            left with { SerializeNull = false },
            left with { CanBeNull = false },
            left with { PropertyFormat = null },
            left with { ValueFormat = format with { Kind = InlineFormatKind.Formattable } },
            left with { Collection = null },
            left with { Nested = [nested with { SerializeNull = true }] },
            left with { Nested = null },
            left with { Nested = [] },
            left with { NestedThroughValue = false },
            left with { Dictionary = null }
        ]);
    }

    /// <summary>Verifies dictionary value properties compare by contents rather than array identity.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryDictionary_EqualsAndHashCode_CompareAllBindings()
    {
        var format = new InlineValueFormatModel(InlineFormatKind.String, null, StringTypeName, false, null);
        var property = new QueryObjectPropertyModel(new("Value", null, null, null), false, true, null, format);
        var left = new QueryDictionaryModel(StringTypeName, format, true, QueryPrefix, [property]);
        var right = left with { KeyFormat = format with { }, ValueProperties = [property with { }] };

        await VerifyEqualityAsync(
            left,
            right,
            [
            left with { KeyTypeName = "int" },
            left with { KeyFormat = format with { Kind = InlineFormatKind.Formattable } },
            left with { ValueCanBeNull = false },
            left with { PrefixSegment = null },
            left with { ValueProperties = [property with { CanBeNull = false }] },
            left with { ValueProperties = null },
            left with { ValueProperties = [] }
        ]);
    }

    /// <summary>Verifies query parameters compare nested properties, dictionaries, converters and rendering options.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryParameter_EqualsAndHashCode_CompareAllBindings()
    {
        var format = new InlineValueFormatModel(InlineFormatKind.String, null, StringTypeName, false, null);
        var property = new QueryObjectPropertyModel(new("Name", null, null, null), false, true, null, format);
        var dictionary = new QueryDictionaryModel(StringTypeName, format, true, null, [property]);
        var converter = new QueryConverterModel("global::Converter", QueryPrefix);
        var left = new QueryParameterModel(
            "query",
            QueryParameterShape.Object,
            new(false, false, null, true, format),
            [property],
            dictionary,
            converter);
        var right = left with
        {
            Rendering = left.Rendering with { ValueFormat = format with { } },
            ObjectProperties = [property with { }],
            Dictionary = dictionary with { ValueProperties = [property with { }] },
            Converter = converter with { }
        };

        await VerifyEqualityAsync(
            left,
            right,
            [
            left with { Key = OtherParameterName },
            left with { Shape = QueryParameterShape.Scalar },
            left with { Rendering = left.Rendering with { PreEncoded = true } },
            left with { ObjectProperties = [property with { SerializeNull = true }] },
            left with { ObjectProperties = null },
            left with { ObjectProperties = [] },
            left with { Dictionary = null },
            left with { Converter = null },
            left with { NestingDelimiter = "/" }
        ]);
    }

    /// <summary>Verifies paging continuations retain origin-list contents in equality and hashing.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PagingContinuation_EqualsAndHashCode_CompareAllBindings()
    {
        var left = new PagingContinuationModel(PagingNextSource.Property, NextAccess, true, PagingOrigin.Listed, ["https://api.test"]);
        var right = left with { Origins = ["https://api.test"] };

        await VerifyEqualityAsync(
            left,
            right,
            [
            left with { NextSource = PagingNextSource.Header },
            left with { NextAccess = ".Other" },
            left with { NextIsUri = false },
            left with { Origin = PagingOrigin.Any },
            left with { Origins = ["https://other.test"] },
            left with { Origins = [] }
        ]);
        await VerifyEqualityAsync(default(PagingContinuationModel), default(PagingContinuationModel) with { Origins = [] }, []);
    }

    /// <summary>Verifies paging models compare the page types, token bindings and nested continuation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Paging_EqualsAndHashCode_CompareAllBindings()
    {
        var continuation = new PagingContinuationModel(PagingNextSource.Property, NextAccess, false, PagingOrigin.None, []);
        var left = new PagingModel(PagingKind.Token, "Page", "Item", StringTypeName, "cursor", ".Items", continuation);
        var right = left with { Continuation = continuation with { Origins = [] } };

        await VerifyEqualityAsync(
            left,
            right,
            [
            left with { Kind = PagingKind.Link },
            left with { PageType = "OtherPage" },
            left with { ItemType = "OtherItem" },
            left with { TokenType = "int" },
            left with { TokenParameter = "offset" },
            left with { ItemsAccess = ".OtherItems" },
            left with { Continuation = continuation with { NextAccess = ".Other" } }
        ]);
    }

    /// <summary>Verifies parameter attributes compare constructor and named arguments structurally.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ParameterAttribute_EqualsAndHashCode_CompareAllArguments()
    {
        var left = new ParameterAttributeModel(AttributeTypeExpression, [AttributeValueExpression], [new("Name", "\"name\"")]);
        var right = new ParameterAttributeModel(AttributeTypeExpression, [AttributeValueExpression], [new("Name", "\"name\"")]);

        await VerifyEqualityAsync(
            left,
            right,
            [
            left with { TypeExpression = "global::OtherAttribute" },
            left with { ConstructorArguments = [OtherAttributeValueExpression] },
            left with { NamedArguments = [new("Name", OtherAttributeValueExpression)] }
        ]);
        await VerifyEqualityAsync(
            default(ParameterAttributeModel),
            default(ParameterAttributeModel) with
        {
            ConstructorArguments = [],
            NamedArguments = []
        },
            []);
    }

    /// <summary>Verifies formatting models include enum contents and span-formatting capabilities.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InlineValueFormat_EqualsAndHashCode_CompareAllBindings()
    {
        var left = new InlineValueFormatModel(InlineFormatKind.Enum, "G", "Sort", true, [new(AscendingMember, "asc")]);
        var right = left with { EnumMembers = [new(AscendingMember, "asc")] };

        await VerifyEqualityAsync(
            left,
            right,
            [
            left with { Kind = InlineFormatKind.Formattable },
            left with { Format = null },
            left with { TypeName = "OtherSort" },
            left with { IsNullableValueType = false },
            left with { EnumMembers = [new(AscendingMember, "ascending")] },
            left with { EnumMembers = null },
            left with { EnumMembers = [] },
            left with { IsUrlSafeSpanFormattable = true },
            left with { IsSpanFormattableEscapable = true }
        ]);
    }

    /// <summary>Verifies request parameters include attributes, URL policy and JSON metadata in cache keys.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestParameter_EqualsAndHashCode_CompareAllBindings()
    {
        var attribute = new ParameterAttributeModel(AttributeTypeExpression, [AttributeValueExpression], []);
        var format = new InlineValueFormatModel(InlineFormatKind.String, null, StringTypeName, false, null);
        var query = new QueryParameterModel("query", QueryParameterShape.Scalar, new(false, false, null, false, format));
        var left = new RequestParameterModel(
            new(ParameterName, StringTypeName, true),
            default,
            [attribute],
            RequestParameterKind.Query,
            default,
            "option",
            default)
        { Query = query, JsonTypeInfoTarget = StringTypeName };
        var right = left with
        {
            Attributes = [attribute with { ConstructorArguments = [AttributeValueExpression], NamedArguments = [] }],
            Query = query with { Rendering = query.Rendering with { ValueFormat = format with { } } }
        };

        await VerifyEqualityAsync(
            left,
            right,
            [
            left with { Identity = left.Identity with { Name = OtherParameterName } },
            left with { Path = left.Path with { PreEncoded = true } },
            left with { Attributes = [attribute with { ConstructorArguments = [OtherAttributeValueExpression] }] },
            left with { Kind = RequestParameterKind.Body },
            left with { Header = left.Header with { HeaderName = HeaderName } },
            left with { PropertyKey = "otherOption" },
            left with { Body = left.Body with { IsAsyncJsonLines = true } },
            left with { AllowRelativeUrl = true },
            left with { Query = query with { Key = OtherParameterName } },
            left with { Query = null },
            left with { JsonTypeInfoTarget = "int" }
        ]);
        await VerifyEqualityAsync(default(RequestParameterModel), default(RequestParameterModel) with { Attributes = [] }, []);
    }

    /// <summary>Verifies request cache keys retain paging, URI formatting and timeout metadata.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Request_EqualsAndHashCode_CompareAllBindings()
    {
        const int TimeoutMilliseconds = 1000;
        var continuation = new PagingContinuationModel(PagingNextSource.Property, NextAccess, false, PagingOrigin.None, []);
        var paging = new PagingModel(PagingKind.Token, "Page", "Item", StringTypeName, "cursor", ".Items", continuation);
        var left = RequestModel.Empty with
        {
            HttpMethod = "GET",
            Path = "/items",
            Bindings = new([new(HeaderName, JsonMediaType)], []),
            Paging = paging,
            QueryUriFormat = (int)UriFormat.UriEscaped,
            TimeoutMilliseconds = TimeoutMilliseconds
        };
        var right = left with
        {
            Bindings = new([new(HeaderName, JsonMediaType)], []),
            Paging = paging with { Continuation = continuation with { Origins = [] } }
        };

        await VerifyEqualityAsync(
            left,
            right,
            [
            left with { HttpMethod = "POST" },
            left with { Path = "/other" },
            left with { Return = left.Return with { ShouldDisposeResponse = false } },
            left with { Generation = left.Generation with { CanGenerateInline = true } },
            left with { Bindings = new([], []) },
            left with { Body = left.Body with { MultipartBoundary = "boundary" } },
            left with { QueryUriFormat = null },
            left with { Paging = paging with { TokenParameter = "offset" } },
            left with { Paging = null },
            left with { TimeoutMilliseconds = 0 }
        ]);
    }

    /// <summary>Verifies method cache keys compare parameter and generic-constraint contents.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Method_EqualsAndHashCode_CompareAllBindings()
    {
        var constraint = new TypeConstraint("T", "T", KnownTypeConstraint.Class, [DisposableTypeName]);
        var left = new MethodModel(
            "Get",
            "Task<T>",
            "IApi",
            "Get",
            ReturnTypeInfo.AsyncResult,
            RequestModel.Empty,
            [new(ParameterName, "T", false, true)],
            [constraint],
            default);
        var right = left with
        {
            Parameters = [new(ParameterName, "T", false, true)],
            Constraints = [constraint with { Constraints = [DisposableTypeName] }],
            Request = RequestModel.Empty with { Bindings = new([], []) }
        };

        await VerifyEqualityAsync(
            left,
            right,
            [
            left with { Name = OtherMemberName },
            left with { ReturnType = "Task<string>" },
            left with { ContainingType = "IOtherApi" },
            left with { DeclaredMethod = OtherMemberName },
            left with { ReturnTypeMetadata = ReturnTypeInfo.AsyncVoid },
            left with { Request = left.Request with { Path = "/other" } },
            left with { Parameters = [new(OtherParameterName, "T", false, true)] },
            left with { Constraints = [constraint with { Constraints = [] }] },
            left with { Annotations = new(true, false, false) }
        ]);
        await VerifyEqualityAsync(default(MethodModel), default(MethodModel) with { Parameters = [], Constraints = [] }, []);
    }

    /// <summary>Verifies generic constraints include every name, flag and textual constraint.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TypeConstraint_EqualsAndHashCode_CompareAllBindings()
    {
        var left = new TypeConstraint("T", "T", KnownTypeConstraint.Class, [DisposableTypeName]);
        var right = left with { Constraints = [DisposableTypeName] };

        await VerifyEqualityAsync(
            left,
            right,
            [
            left with { TypeName = "U" },
            left with { DeclaredName = "U" },
            left with { KnownTypeConstraint = KnownTypeConstraint.Struct },
            left with { Constraints = [] }
        ]);
        await VerifyEqualityAsync(default(TypeConstraint), default(TypeConstraint) with { Constraints = [] }, []);
    }

    /// <summary>Verifies each interface member array contributes its contents to equality and hashing.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InterfaceMembers_EqualsAndHashCode_CompareAllBindings()
    {
        var constraint = new TypeConstraint("T", "T", KnownTypeConstraint.Class, [DisposableTypeName]);
        var property = new InterfacePropertyModel(ParameterName, StringTypeName, false, "IApi", string.Empty, true, false, false, false);
        var method = new MethodModel("Get", "Task<T>", "IApi", "Get", ReturnTypeInfo.AsyncResult, RequestModel.Empty, [], [], default);
        var left = new InterfaceMembersModel([constraint], [ParameterName], [property], [method], [method], [method]);
        var right = new InterfaceMembersModel(
            [constraint with { Constraints = [DisposableTypeName] }],
            [ParameterName],
            [property with { }],
            [method with { Parameters = [], Constraints = [] }],
            [method with { Parameters = [], Constraints = [] }],
            [method with { Parameters = [], Constraints = [] }]);

        await VerifyEqualityAsync(
            left,
            right,
            [
                left with { Constraints = [constraint with { KnownTypeConstraint = KnownTypeConstraint.Struct }] },
                left with { MemberNames = [OtherParameterName] },
                left with { Properties = [property with { Name = OtherParameterName }] },
                left with { NonRefitMethods = [method with { Name = OtherMemberName }] },
                left with { RefitMethods = [method with { Name = OtherMemberName }] },
                left with { DerivedRefitMethods = [method with { Name = OtherMemberName }] }
            ]);
    }

    /// <summary>Verifies interface metadata includes names, consumer capabilities, members and extern aliases.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Interface_EqualsAndHashCode_CompareAllBindings()
    {
        var left = new InterfaceModel(
            "IApi.g.cs",
            new("Generated", "Api", "Consumer", "class Api", "global::Consumer.IApi", string.Empty),
            new("global::Preserve", true, true, true, true, true),
            new([], [ParameterName], [], [], [], []),
            Nullability.Enabled,
            false,
            ["dependency"]);
        var right = left with
        {
            Members = new([], [ParameterName], [], [], [], []),
            ExternAliases = ["dependency"]
        };

        await VerifyEqualityAsync(
            left,
            right,
            [
                left with { FileName = "IOtherApi.g.cs" },
                left with { Names = left.Names with { ClassName = OtherMemberName } },
                left with { Options = left.Options with { GeneratedRequestBuilding = false } },
                left with { Members = left.Members with { MemberNames = [OtherParameterName] } },
                left with { Nullability = Nullability.Disabled },
                left with { DisposeMethod = true },
                left with { ExternAliases = ["otherDependency"] }
            ]);
    }

    /// <summary>Verifies generation contexts compare nested interfaces and all consumer options.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ContextGeneration_EqualsAndHashCode_CompareAllBindings()
    {
        var model = new InterfaceModel("IApi.g.cs", default, default, new([], [], [], [], [], []), Nullability.Disabled, false, []);
        var left = new ContextGenerationModel("Consumer", "global::Preserve", "Generated", true, true, [model]);
        var right = left with { Interfaces = [model with { Members = new([], [], [], [], [], []), ExternAliases = [] }] };

        await VerifyEqualityAsync(
            left,
            right,
            [
                left with { RefitInternalNamespace = "OtherConsumer" },
                left with { PreserveAttributeDisplayName = "global::OtherPreserve" },
                left with { GeneratedClassName = OtherMemberName },
                left with { GeneratedRequestBuilding = false },
                left with { EmitGeneratedCodeMarkers = false },
                left with { Interfaces = [model with { DisposeMethod = true }] },
                left with { Interfaces = [] }
            ]);
    }

    /// <summary>Checks symmetric equality, consistent hashes and distinct cache keys for changed metadata.</summary>
    /// <typeparam name="T">The metadata type.</typeparam>
    /// <param name="left">The original metadata.</param>
    /// <param name="right">An independently allocated equivalent value.</param>
    /// <param name="different">Values that each change one binding.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    private static async Task VerifyEqualityAsync<T>(T left, T right, T[] different)
        where T : notnull, IEquatable<T>
    {
        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(right.Equals(left)).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
        if (typeof(T).IsClass)
        {
            await Assert.That(left.Equals(default)).IsFalse();
        }

        foreach (var value in different)
        {
            await Assert.That(left.Equals(value)).IsFalse();
            await Assert.That(value.Equals(left)).IsFalse();
        }

        HashSet<T> keys = [left, right, .. different];
        await Assert.That(keys.Count).IsEqualTo(different.Length + 1);
    }
}
