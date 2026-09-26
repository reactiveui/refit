// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Names the first limitation that stops generated request building from constructing a method's request.</summary>
/// <remarks>The generator ignores the value; the RF006 analyzer turns it into a located, specific explanation.</remarks>
internal enum InlineFallbackReason
{
    /// <summary>The request is generated inline.</summary>
    None = 0,

    /// <summary>The method has no Refit HTTP method attribute.</summary>
    MissingHttpMethodAttribute = 1,

    /// <summary>The return type is not a shape generated requests produce, and no return-type adapter surfaces it.</summary>
    UnsupportedReturnType = 2,

    /// <summary>A custom HTTP method attribute's verb cannot be read at compile time.</summary>
    UnreadableHttpMethod = 3,

    /// <summary>The path template contains a backslash, a line break, or an unterminated placeholder.</summary>
    UnsupportedPathTemplate = 4,

    /// <summary>A <c>[Url]</c> parameter is neither a <c>string</c> nor a <c>Uri</c>.</summary>
    UrlParameterType = 5,

    /// <summary>A <c>[Url]</c> parameter is combined with a path template, a path parameter or another <c>[Url]</c>.</summary>
    UrlParameterWithPath = 6,

    /// <summary>A form-url-encoded body's type references a method type parameter.</summary>
    GenericUrlEncodedBody = 7,

    /// <summary>A <c>[Body]</c> attribute names a serialization method the generator does not know.</summary>
    UnknownBodySerialization = 8,

    /// <summary>More than one <c>[Body]</c> parameter.</summary>
    MultipleBodies = 9,

    /// <summary>A second un-attributed complex parameter would also become the implicit request body.</summary>
    SecondImplicitBody = 10,

    /// <summary>More than one <c>CancellationToken</c> parameter.</summary>
    MultipleCancellationTokens = 11,

    /// <summary>More than one <c>[HeaderCollection]</c> parameter.</summary>
    MultipleHeaderCollections = 12,

    /// <summary>More than one <c>[Authorize]</c> parameter.</summary>
    MultipleAuthorizeParameters = 13,

    /// <summary>A <c>[Multipart]</c> method also declares a <c>[Body]</c> parameter.</summary>
    MultipartWithBody = 14,

    /// <summary>A <c>{name}</c> path parameter is <c>object</c>, an interface or an unconstrained type parameter.</summary>
    UnsupportedPathParameterType = 15,

    /// <summary>An <c>[Encoded]</c> round-trip <c>{**name}</c> parameter is not a <c>string</c>.</summary>
    EncodedRoundTripNotString = 16,

    /// <summary>A query parameter's declared type cannot be flattened at compile time.</summary>
    UnsupportedQueryType = 17,

    /// <summary>A <c>[QueryConverter]</c> attribute does not name a converter type.</summary>
    UnreadableQueryConverter = 18,

    /// <summary>A dotted <c>{param.Prop}</c> placeholder does not resolve to a simple readable property.</summary>
    UnresolvedPathProperty = 19,

    /// <summary>A path-bound object has a property left for the query string that cannot be flattened.</summary>
    UnsupportedPathObjectQuery = 20,

    /// <summary>A multipart parameter's declared type is not a part type the generator can dispatch statically.</summary>
    UnsupportedMultipartPart = 21,

    /// <summary>The <c>[Paged]</c> configuration is invalid (reported as RF013).</summary>
    InvalidPagedMethod = 22,

    /// <summary>A <c>JsonTypeInfo&lt;T&gt;</c> parameter is unusable (reported as RF014).</summary>
    InvalidJsonTypeInfoParameter = 23,

    /// <summary>A multipart parameter carries <c>[FormObject]</c>, whose per-property flattening only the reflection builder performs.</summary>
    FormObjectMultipartPart = 24,
}
