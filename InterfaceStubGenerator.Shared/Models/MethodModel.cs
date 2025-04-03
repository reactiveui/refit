namespace Refit.Generator;

internal sealed record MethodModel(
    string Name,
    string ReturnType,
    string ContainingType,
    string DeclaredMethod,
    ReturnTypeInfo ReturnTypeMetadata,
    ImmutableEquatableArray<ParameterModel> Parameters,
    ImmutableEquatableArray<TypeConstraint> Constraints,
    RefitBodyModel? RefitBody,
    string? Error
);

// TODO: maybe add RXFunc?
// TODO: Add inner return type aka T in Task<T>
internal enum ReturnTypeInfo : byte
{
    Return,
    AsyncVoid,
    AsyncResult,
}

internal record ThrowError(string errorExpression);

// TODO: rename generic inner
internal sealed record RefitBodyModel(
    HttpMethod HttpMethod,
    string? GenericInnerReturnType,
    string DeserializedResultType,
    bool IsApiResponse,
    string? CancellationTokenParam,
    string? MultipartBoundary,
    ImmutableEquatableArray<ParameterFragment> UrlFragments,
    ImmutableEquatableArray<HeaderPsModel> HeaderPs,
    ImmutableEquatableArray<HeaderModel> Headers,
    ImmutableEquatableArray<HeaderParameterModel> HeaderParameters,
    string? HeaderCollectionParam,
    ImmutableEquatableArray<AuthoriseModel> AuthoriseParameters,
    ImmutableEquatableArray<PropertyModel> Properties,
    ImmutableEquatableArray<QueryModel> QueryParameters,
    BodyModel? BodyParameter,
    UriFormat UriFormat
);

internal record struct HeaderModel(string Key, string Value);

internal record struct HeaderParameterModel(string Parameter, string HeaderKey);

internal record struct PropertyModel(string Parameter, string Key);

internal record struct AuthoriseModel(string Parameter, string Scheme);

internal record ConstantFragmentModel(string Value) : ParameterFragment;

internal record DynamicFragmentModel(string Access, int ParameterIndex, string TypeDeclaration)
    : ParameterFragment;

internal record DynamicRoundTripFragmentModel(
    string Access,
    int ParameterIndex,
    string TypeDeclaration
) : ParameterFragment;

internal record DynamicPropertyFragmentModel(
    string Access,
    string PropertyName,
    string ContainingType,
    string TypeDeclaration
) : ParameterFragment;

internal record QueryModel(
    string Parameter,
    int ParameterIndex,
    Refit.Generator.Configuration.CollectionFormat CollectionFormat,
    string Delimiter,
    string? Prefix,
    string? Format
);

internal record BodyModel(
    string Parameter,
    bool Buffered,
    Refit.Generator.Configuration.BodySerializationMethod SerializationMethod
);

// TODO: decide how to handle enum types
internal record HeaderPsModel(
    HeaderType Type,
    HeaderModel? Static,
    HeaderParameterModel? Dynamic,
    string? Collection,
    AuthoriseModel? Authorise
);

internal enum BodyParameterType
{
    Content,
    Stream,
    String,
}

internal enum HeaderType
{
    Static,
    Dynamic,
    Collection,
    Authorise,
}

internal record ParameterFragment { }
