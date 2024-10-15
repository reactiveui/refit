using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

using DefaultNamespace;

using Microsoft.CodeAnalysis;

using Refit.Generator.Configuration;

namespace Refit.Generator;

 /// <summary>
/// RestMethodInfo
/// </summary>
public record RestMethodSymbol(
    string Name,
    Type HostingType,
    IMethodSymbol MethodSymbol,
    string RelativePath,
    ITypeSymbol ReturnType
);

// TODO: most files in Configuration are not needed
[DebuggerDisplay("{MethodInfo}")]
internal class RestMethodSymbolInternal
{
    static readonly QueryConfiguration DefaultQueryAttribute = new ();

    private int HeaderCollectionParameterIndex { get; set; }
    public string Name { get; set; }
    // public Type Type { get; set; }
    public IMethodSymbol MethodInfo { get; set; }
    public HttpMethod HttpMethod { get; set; }
    public string RelativePath { get; set; }
    public bool IsMultipart { get; private set; }
    public string MultipartBoundary { get; private set; }

    // TODO: ensure that an off by one error does not occur because of cancellation token
    public IParameterSymbol? CancellationToken { get; set; }
    public UriFormat QueryUriFormat { get; set; }
    public Dictionary<string, string?> Headers { get; set; }
    public Dictionary<int, string> HeaderParameterMap { get; set; }
    public Dictionary<int, string> PropertyParameterMap { get; set; }
    public Tuple<BodySerializationMethod, bool, int>? BodyParameterInfo { get; set; }
    public Tuple<string, int>? AuthorizeParameterInfo { get; set; }
    public Dictionary<int, string> QueryParameterMap { get; set; }
    public List<QueryModel> QueryModels { get; set; }
    public Dictionary<int, Tuple<string, string>> AttachmentNameMap { get; set; }
    public IParameterSymbol[] ParameterSymbolArray { get; set; }
    public Dictionary<int, RestMethodParameterInfo> ParameterMap { get; set; }
    public List<ParameterFragment> PathFragments { get; set; }
    public ITypeSymbol ReturnType { get; set; }
    public ITypeSymbol ReturnResultType { get; set; }
    public ITypeSymbol DeserializedResultType { get; set; }

    // TODO: logic associated with RefitSettings has to be moved into runtime
    // public RefitSettings RefitSettings { get; set; }
    public bool IsApiResponse { get; }
    public bool ShouldDisposeResponse { get; private set; }

    static readonly Regex ParameterRegex = new(@"{(.*?)}");
    static readonly Regex ParameterRegex2 = new(@"{(([^/?\r\n])*?)}");
    static readonly HttpMethod PatchMethod = new("PATCH");

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public RestMethodSymbolInternal(
        IMethodSymbol methodSymbol,
        WellKnownTypes knownTypes
    )
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        // RefitSettings = refitSettings ?? new RefitSettings();
        // Type = targetInterface ?? throw new ArgumentNullException(nameof(targetInterface));
        Name = methodSymbol.Name;
        MethodInfo = methodSymbol ?? throw new ArgumentNullException(nameof(methodSymbol));

        var hma = GetHttpMethod(methodSymbol, knownTypes);

        HttpMethod = hma.Method;
        RelativePath = hma.Path;

        var multiPartSymbol = knownTypes.TryGet("Refit.MultipartAttribute");
        var multiPartsAttribute = methodSymbol.AccessFirstOrDefault<MulitpartConfiguration>(multiPartSymbol, knownTypes);
        IsMultipart = multiPartsAttribute is not null;

        // TODO: default boundary
        // MultipartBoundary = IsMultipart
        //     ? multiPartsAttribute?.BoundaryText
        //       ?? new MultipartAttribute().BoundaryText
        //     : null;
        MultipartBoundary = IsMultipart
            ? multiPartsAttribute?.BoundaryText
              ?? "----MyGreatBoundary"
            : null;

        VerifyUrlPathIsSane(RelativePath);
        DetermineReturnTypeInfo(methodSymbol, knownTypes);
        DetermineIfResponseMustBeDisposed(knownTypes);

        // Exclude cancellation token parameters from this list
        var cancellationToken = knownTypes.Get<CancellationToken>();
        ParameterSymbolArray = methodSymbol
            .Parameters
            .Where(p => !SymbolEqualityComparer.Default.Equals(cancellationToken, p))
            .ToArray();
        (ParameterMap, PathFragments) = BuildParameterMap2(RelativePath, ParameterSymbolArray, knownTypes);
        BodyParameterInfo = FindBodyParameter(ParameterSymbolArray, IsMultipart, hma.Method, knownTypes);
        AuthorizeParameterInfo = FindAuthorizationParameter(ParameterSymbolArray, knownTypes);

        // TODO: make pseudo enum header to represent the 3 types of headers
        // initialise the same way refit does
        Headers = ParseHeaders(methodSymbol, knownTypes);
        HeaderParameterMap = BuildHeaderParameterMap(ParameterSymbolArray, knownTypes);
        HeaderCollectionParameterIndex = RestMethodSymbolInternal.GetHeaderCollectionParameterIndex(
            ParameterSymbolArray, knownTypes
        );
        PropertyParameterMap = BuildRequestPropertyMap(ParameterSymbolArray, knownTypes);

        // get names for multipart attachments
        Dictionary<int, Tuple<string, string>>? attachmentDict = null;
        if (IsMultipart)
        {
            for (var i = 0; i < ParameterSymbolArray.Length; i++)
            {
                if (
                    ParameterMap.ContainsKey(i)
                    || HeaderParameterMap.ContainsKey(i)
                    || PropertyParameterMap.ContainsKey(i)
                    || HeaderCollectionAt(i)
                )
                {
                    continue;
                }

                var attachmentName = GetAttachmentNameForParameter(ParameterSymbolArray[i], knownTypes);
                if (attachmentName == null)
                    continue;

                attachmentDict ??= [];
                attachmentDict[i] = Tuple.Create(
                    attachmentName,
                    GetUrlNameForParameter(ParameterSymbolArray[i], knownTypes)
                );
            }
        }

        AttachmentNameMap = attachmentDict ?? new Dictionary<int, Tuple<string, string>>();

        Dictionary<int, string>? queryDict = null;
        for (var i = 0; i < ParameterSymbolArray.Length; i++)
        {
            if (
                ParameterMap.ContainsKey(i)
                || HeaderParameterMap.ContainsKey(i)
                || PropertyParameterMap.ContainsKey(i)
                || HeaderCollectionAt(i)
                || (BodyParameterInfo != null && BodyParameterInfo.Item3 == i)
                || (AuthorizeParameterInfo != null && AuthorizeParameterInfo.Item2 == i)
            )
            {
                continue;
            }

            queryDict ??= [];
            queryDict.Add(i, GetUrlNameForParameter(ParameterSymbolArray[i], knownTypes));
        }

        QueryParameterMap = queryDict ?? new Dictionary<int, string>();

        var ctParamEnumerable = methodSymbol
            .Parameters
            .Where(p => SymbolEqualityComparer.Default.Equals(p.Type, cancellationToken))
            .ToArray();
        if (ctParamEnumerable.Length > 1)
        {
            throw new ArgumentException(
                $"Argument list to method \"{methodSymbol.Name}\" can only contain a single CancellationToken"
            );
        }

        QueryModels = BuildQueryParameterList(knownTypes);

        CancellationToken = ctParamEnumerable.FirstOrDefault();

        var queryUriAttribute = knownTypes.TryGet("Refit.QueryUriFormatAttribute")!;
        QueryUriFormat =  methodSymbol.AccessFirstOrDefault<QueryUriFormatConfiguration>(queryUriAttribute, knownTypes)?.UriFormat
                          ?? UriFormat.UriEscaped;

        var apiResponse = knownTypes.TryGet("Refit.ApiResponse`1");
        var unboundIApiResponse = knownTypes.TryGet("Refit.IApiResponse`1");
        var iApiResponse = knownTypes.TryGet("Refit.IApiResponse");

        IsApiResponse =
            ReturnResultType is INamedTypeSymbol {IsGenericType: true } namedTypeSymbol
                && (
                    SymbolEqualityComparer.Default.Equals(namedTypeSymbol.OriginalDefinition, apiResponse)
                    || namedTypeSymbol.OriginalDefinition.InheritsFromOrEquals(unboundIApiResponse)
                )
            || SymbolEqualityComparer.Default.Equals(ReturnResultType, iApiResponse);
    }

    public bool HasHeaderCollection => HeaderCollectionParameterIndex >= 0;

    public bool HeaderCollectionAt(int index) => HeaderCollectionParameterIndex >= 0 && HeaderCollectionParameterIndex == index;

    // TODO: this should be moved to a new class, along with most model logic
    public RefitBodyModel ToRefitBodyModel()
    {
        // TODO: Add Authorise
        // // // TODO: should ParseHeaders already add this?
        // if (AuthorizeParameterInfo is not null)
        // {
        //     Headers[AuthorizeParameterInfo.]
        // }

        // TODO: headercollectionParam is broken
        // TODO: Is query model logic correct?

        return new RefitBodyModel(HttpMethod,
            ReturnResultType?.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat),
            DeserializedResultType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            IsApiResponse,
            CancellationToken?.Name, MultipartBoundary,
            PathFragments.ToImmutableEquatableArray(),
            BuildHeaderPsModel().ToImmutableEquatableArray(),
            Headers.Select(kp => new HeaderModel(kp.Key, kp.Value)).ToImmutableEquatableArray(),
            HeaderParameterMap.Select(kp => new HeaderParameterModel(ParameterSymbolArray[kp.Key].Name, kp.Value))
                .ToImmutableEquatableArray(),
            HeaderCollectionParameterIndex < 0 ? null : ParameterSymbolArray[HeaderCollectionParameterIndex].Name,
            ImmutableEquatableArray<AuthoriseModel>.Empty,
            PropertyParameterMap.Select(kp => new PropertyModel(ParameterSymbolArray[kp.Key].Name, kp.Value))
                .ToImmutableEquatableArray(),
            QueryModels.ToImmutableEquatableArray(),
            BodyParameterInfo is null ? null : new BodyModel(ParameterSymbolArray[BodyParameterInfo.Item3].Name, BodyParameterInfo.Item2,BodyParameterInfo.Item1),
            QueryUriFormat);

    }

    static HttpMethodConfiguration GetHttpMethod(IMethodSymbol methodSymbol, WellKnownTypes knownTypes)
    {
        var attributeSymbol = knownTypes.TryGet("Refit.HttpMethodAttribute")!;
        var attribute = methodSymbol.GetAttributesFor(attributeSymbol).FirstOrDefault()!;
        var hma = attribute?.MapToType<HttpMethodConfiguration>(knownTypes)!;

        var getAttribute = knownTypes.TryGet("Refit.GetAttribute");
        var postAttribute = knownTypes.TryGet("Refit.PostAttribute");
        var putAttribute = knownTypes.TryGet("Refit.PutAttribute");
        var deleteAttribute = knownTypes.TryGet("Refit.DeleteAttribute");
        var patchAttribute = knownTypes.TryGet("Refit.PatchAttribute");
        var optionsAttribute = knownTypes.TryGet("Refit.OptionsAttribute");
        var headAttribute = knownTypes.TryGet("Refit.HeadAttribute");

        if (attribute.AttributeClass.InheritsFromOrEquals(getAttribute))
        {
            hma.Method = HttpMethod.Get;
        }
        else if (attribute.AttributeClass.InheritsFromOrEquals(postAttribute))
        {
            hma.Method = HttpMethod.Post;
        }
        else if (attribute.AttributeClass.InheritsFromOrEquals(putAttribute))
        {
            hma.Method = HttpMethod.Put;
        }
        else if (attribute.AttributeClass.InheritsFromOrEquals(deleteAttribute))
        {
            hma.Method = HttpMethod.Delete;
        }
        else if (attribute.AttributeClass.InheritsFromOrEquals(patchAttribute))
        {
            hma.Method = PatchMethod;
        }
        else if (attribute.AttributeClass.InheritsFromOrEquals(optionsAttribute))
        {
            hma.Method = HttpMethod.Options;
        }
        else if (attribute.AttributeClass.InheritsFromOrEquals(headAttribute))
        {
            hma.Method = HttpMethod.Head;
        }
        else
        {
            // TODO: need to emit a diagnostic here
            // I don't think I can support custom HttpMethodAttributes
        }

        return hma;
    }

    static int GetHeaderCollectionParameterIndex(IParameterSymbol[] parameterArray, WellKnownTypes knownTypes)
    {
        var headerIndex = -1;

        // TODO: convert this into a real string string dictionary
        var dictionaryOpenType  = knownTypes.Get(typeof(IDictionary<,>));
        var stringType = knownTypes.Get<string>();
        var genericDictionary  = dictionaryOpenType.Construct(stringType, stringType);

        for (var i = 0; i < parameterArray.Length; i++)
        {
            var param = parameterArray[i];
            var headerCollectionSymbol = knownTypes.TryGet("Refit.HeaderCollectionAttribute")!;
            var headerCollection = param.AccessFirstOrDefault<HeaderCollectionConfiguration>(headerCollectionSymbol, knownTypes);

            if (headerCollection == null) continue;

            // TODO: this check may not work in nullable contexts.
            //opted for IDictionary<string, string> semantics here as opposed to the looser IEnumerable<KeyValuePair<string, string>> because IDictionary will enforce uniqueness of keys
            if (SymbolEqualityComparer.Default.Equals(param.Type, genericDictionary))
            {
                // throw if there is already a HeaderCollection parameter
                if(headerIndex >= 0)
                    throw new ArgumentException("Only one parameter can be a HeaderCollection parameter");

                headerIndex = i;
            }
            else
            {
                throw new ArgumentException(
                    $"HeaderCollection parameter of type {param.Type.Name} is not assignable from IDictionary<string, string>"
                );
            }
        }

        return headerIndex;
    }

    // public RestMethodSymbol ToRestMethodSymbol() =>
    //     new(Name, Type, MethodInfo, RelativePath, ReturnType);

    // TODO: need to escape strings line [Property("hey\"world")]
    static Dictionary<int, string> BuildRequestPropertyMap(IParameterSymbol[] parameterArray, WellKnownTypes knownTypes)
    {
        Dictionary<int, string>? propertyMap = null;

        for (var i = 0; i < parameterArray.Length; i++)
        {
            var param = parameterArray[i];
            var propertySymbol = knownTypes.TryGet("Refit.PropertyAttribute")!;
            var propertyAttribute = param
                .AccessFirstOrDefault<PropertyConfiguration>(propertySymbol, knownTypes);

            if (propertyAttribute != null)
            {
                var propertyKey = !string.IsNullOrEmpty(propertyAttribute.Key)
                    ? propertyAttribute.Key
                    : param.Name!;
                propertyMap ??= new Dictionary<int, string>();
                propertyMap[i] = propertyKey!;
            }
        }

        return propertyMap ?? new Dictionary<int, string>();
    }

    List<QueryModel> BuildQueryParameterList(WellKnownTypes knownTypes)
    {
        List<QueryModel> queryParamsToAdd = [];
        RestMethodParameterInfo? parameterInfo = null;

        for (var i = 0; i < ParameterSymbolArray.Length; i++)
        {
            var isParameterMappedToRequest = false;
            var param = ParameterSymbolArray[i];
            // if part of REST resource URL, substitute it in
            if (this.ParameterMap.TryGetValue(i, out var parameterMapValue))
            {
                parameterInfo = parameterMapValue;
                if (!parameterInfo.IsObjectPropertyParameter)
                {
                    // mark parameter mapped if not an object
                    // we want objects to fall through so any parameters on this object not bound here get passed as query parameters
                    isParameterMappedToRequest = true;
                }
            }

            // if marked as body, add to content
            if (
                this.BodyParameterInfo != null
                && this.BodyParameterInfo.Item3 == i
            )
            {
                // AddBodyToRequest(restMethod, param, ret);
                isParameterMappedToRequest = true;
            }

            // if header, add to request headers
            if (this.HeaderParameterMap.TryGetValue(i, out var headerParameterValue))
            {
                isParameterMappedToRequest = true;
            }

            //if header collection, add to request headers
            if (this.HeaderCollectionAt(i))
            {
                isParameterMappedToRequest = true;
            }

            //if authorize, add to request headers with scheme
            if (
                this.AuthorizeParameterInfo != null
                && this.AuthorizeParameterInfo.Item2 == i
            )
            {
                isParameterMappedToRequest = true;
            }

            //if property, add to populate into HttpRequestMessage.Properties
            if (this.PropertyParameterMap.ContainsKey(i))
            {
                isParameterMappedToRequest = true;
            }

            // ignore nulls and already processed parameters
            if (isParameterMappedToRequest || param == null)
                continue;

            // for anything that fell through to here, if this is not a multipart method add the parameter to the query string
            // or if is an object bound to the path add any non-path bound properties to query string
            // or if it's an object with a query attribute
            QueryConfiguration queryAttribute = null;

            // var queryAttribute = this
            //     .ParameterSymbolArray[i]
            //     .GetCustomAttribute<QueryAttribute>();
            if (
                !this.IsMultipart
                || this.ParameterMap.ContainsKey(i)
                    && this.ParameterMap[i].IsObjectPropertyParameter
                || queryAttribute != null
            )
            {
                queryParamsToAdd ??= [];
                AddQueryParameters(queryAttribute, param, queryParamsToAdd, i, parameterInfo, knownTypes);
                continue;
            }

            // AddMultiPart(restMethod, i, param, multiPartContent);
        }

        return queryParamsToAdd;
    }

    static void AddQueryParameters(QueryConfiguration? queryAttribute, IParameterSymbol param,
        List<QueryModel> queryParamsToAdd, int i, RestMethodParameterInfo? parameterInfo,
        WellKnownTypes knownTypes)
    {
        queryParamsToAdd.Add(new QueryModel(param.Name,0,CollectionFormat.Csv,"-", null, null));
    }

    // void AddQueryParameters(QueryConfiguration? queryAttribute, IParameterSymbol param,
    //     List<QueryModel> queryParamsToAdd, int i, RestMethodParameterInfo? parameterInfo,
    //     WellKnownTypes knownTypes)
    // {
    //     var attr = queryAttribute ?? DefaultQueryAttribute;
    //     if (DoNotConvertToQueryMap(param, knownTypes))
    //     {
    //         queryParamsToAdd.AddRange(
    //             ParseQueryParameter(
    //                 param,
    //                 this.ParameterInfoArray[i],
    //                 this.QueryParameterMap[i],
    //                 attr
    //             )
    //         );
    //     }
    //     else
    //     {
    //         foreach (var kvp in BuildQueryMap(param, attr.Delimiter, parameterInfo))
    //         {
    //             var path = !string.IsNullOrWhiteSpace(attr.Prefix)
    //                 ? $"{attr.Prefix}{attr.Delimiter}{kvp.Key}"
    //                 : kvp.Key;
    //             queryParamsToAdd.AddRange(
    //                 ParseQueryParameter(
    //                     kvp.Value,
    //                     this.ParameterInfoArray[i],
    //                     path,
    //                     attr
    //                 )
    //             );
    //         }
    //     }
    // }

    // TODO: add param nul check to runtime add
    static bool DoNotConvertToQueryMap(IParameterSymbol value, WellKnownTypes knownTypes)
    {
        var type = value.Type;

        // Bail out early & match string
        if (ShouldReturn(type, knownTypes))
            return true;

        var iEnumerableSymbol = knownTypes.Get<System.Collections.IEnumerable>();
        if (type.InheritsFromOrEquals(iEnumerableSymbol))
            return false;

        // Get the element type for enumerables
        var iEnumerableTSymbol = knownTypes.Get(typeof(IEnumerable<>));
        // We don't want to enumerate to get the type, so we'll just look for IEnumerable<T>

        foreach (var iface in type.AllInterfaces)
        {
            // TODO: could probably uncomment
            // if (iface.OriginalDefinition.SpecialType == SpecialType.System_Collections_IEnumerable)
            //     return false;

            if (iface.TypeArguments.Length == 1 && iface.OriginalDefinition.InheritsFromOrEquals(iEnumerableTSymbol))
            {
                return ShouldReturn(iface.TypeArguments[0], knownTypes);
            }
        }

        return false;

        // TODO: I assume that NullableGetUnderlyingType is nullable struct types and not all types
        // TODO: ensure that this works with char? and string?
        // Check if type is a simple string or IFormattable type, check underlying type if Nullable<T>
        static bool ShouldReturn(ITypeSymbol typeSymbol, WellKnownTypes knownTypes)
        {
            if (typeSymbol is INamedTypeSymbol namedType &&
                namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
                namedType.TypeArguments.Length == 1 &&
                namedType.TypeArguments[0].IsValueType)
            {
                return ShouldReturn(namedType.TypeArguments[0], knownTypes);
            }

            var iFormattableSymbol = knownTypes.Get<IFormattable>();
            var uriSymbol = knownTypes.Get<Uri>();

            return typeSymbol.SpecialType == SpecialType.System_String
                  || typeSymbol.SpecialType == SpecialType.System_Boolean
                  || typeSymbol.SpecialType == SpecialType.System_Char
                  || typeSymbol.InheritsFromOrEquals(iFormattableSymbol)
                  || SymbolEqualityComparer.Default.Equals(typeSymbol, uriSymbol);
        }
    }
    //
    // IEnumerable<KeyValuePair<string, string?>> ParseQueryParameter(
    //         ITypeSymbol param,
    //         ParameterInfo parameterInfo,
    //         string queryPath,
    //         QueryConfiguration queryAttribute,
    //         WellKnownTypes knownTypes
    //     )
    // {
    //     var iEnumerableSymbol = knownTypes.Get<System.Collections.IEnumerable>();
    //         if (param.SpecialType != SpecialType.System_String && param.InheritsFromOrEquals(iEnumerableSymbol))
    //         {
    //             foreach (
    //                 var value in ParseEnumerableQueryParameterValue(
    //                     param,
    //                     parameterInfo,
    //                     parameterInfo.ParameterType,
    //                     queryAttribute
    //                 )
    //             )
    //             {
    //                 yield return new KeyValuePair<string, string?>(queryPath, value);
    //             }
    //         }
    //         else
    //         {
    //             throw new NotImplementedException(nameof(ParseQueryParameter));
    //             yield return new KeyValuePair<string, string?>(
    //                 queryPath,
    //                 settings.UrlParameterFormatter.Format(
    //                     param,
    //                     parameterInfo,
    //                     parameterInfo.ParameterType
    //                 )
    //             );
    //         }
    //     }
    //
    //     IEnumerable<string?> ParseEnumerableQueryParameterValue(
    //         IEnumerable paramValues,
    //         ICustomAttributeProvider customAttributeProvider,
    //         Type type,
    //         QueryConfiguration? queryAttribute
    //     )
    //     {
    //         // TODO: collection
    //         var collectionFormat =
    //             queryAttribute != null && queryAttribute.IsCollectionFormatSpecified
    //                 ? queryAttribute.CollectionFormat
    //                 : settings.CollectionFormat;
    //
    //         switch (collectionFormat)
    //         {
    //             case CollectionFormat.Multi:
    //                 foreach (var paramValue in paramValues)
    //                 {
    //                     yield return settings.UrlParameterFormatter.Format(
    //                         paramValue,
    //                         customAttributeProvider,
    //                         type
    //                     );
    //                 }
    //
    //                 break;
    //
    //             default:
    //                 var delimiter =
    //                     collectionFormat switch
    //                     {
    //                         CollectionFormat.Ssv => " ",
    //                         CollectionFormat.Tsv => "\t",
    //                         CollectionFormat.Pipes => "|",
    //                         _ => ","
    //                     };
    //
    //                 // Missing a "default" clause was preventing the collection from serializing at all, as it was hitting "continue" thus causing an off-by-one error
    //                 var formattedValues = paramValues
    //                     .Cast<object>()
    //                     .Select(
    //                         v =>
    //                             settings.UrlParameterFormatter.Format(
    //                                 v,
    //                                 customAttributeProvider,
    //                                 type
    //                             )
    //                     );
    //
    //                 yield return string.Join(delimiter, formattedValues);
    //
    //                 break;
    //         }
    //     }


    static IEnumerable<IPropertySymbol> GetParameterProperties(IParameterSymbol parameter)
    {
        return parameter
            .Type.GetMembers().OfType<IPropertySymbol>()
            .Where(static p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
            .Where(static p => p.GetMethod is { DeclaredAccessibility: Accessibility.Public });
    }

    static void VerifyUrlPathIsSane(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return;

        if (!relativePath.StartsWith("/"))
            throw new ArgumentException(
                $"URL path {relativePath} must start with '/' and be of the form '/foo/bar/baz'"
            );

        // CRLF injection protection
        if (relativePath.Contains('\r') || relativePath.Contains('\n'))
            throw new ArgumentException(
                $"URL path {relativePath} must not contain CR or LF characters"
            );
    }

//     static Dictionary<int, RestMethodParameterInfo> BuildParameterMap(
//         string relativePath,
//         IParameterSymbol[] parameterSymbol,
//         WellKnownTypes knownTypes
//     )
//     {
//         var ret = new Dictionary<int, RestMethodParameterInfo>();
//
//         // This section handles pattern matching in the URL. We also need it to add parameter key/values for any attribute with a [Query]
//         var parameterizedParts = relativePath
//             .Split('/', '?')
//             .SelectMany(x => ParameterRegex.Matches(x).Cast<Match>())
//             .ToList();
//
//         if (parameterizedParts.Count > 0)
//         {
//             var paramValidationDict = parameterSymbol.ToDictionary(
//                 k => GetUrlNameForParameter(k, knownTypes).ToLowerInvariant(),
//                 v => v
//             );
//             //if the param is an lets make a dictionary for all it's potential parameters
//             var objectParamValidationDict = parameterSymbol
//                 .Where(x => x.Type.IsReferenceType)
//                 .SelectMany(x => GetParameterProperties(x).Select(p => Tuple.Create(x, p)))
//                 .GroupBy(
//                     i => $"{i.Item1.Name}.{GetUrlNameForProperty(i.Item2, knownTypes)}".ToLowerInvariant()
//                 )
//                 .ToDictionary(k => k.Key, v => v.First());
//             foreach (var match in parameterizedParts)
//             {
//                 var rawName = match.Groups[1].Value.ToLowerInvariant();
//                 var isRoundTripping = rawName.StartsWith("**");
//                 string name;
//                 if (isRoundTripping)
//                 {
//                     name = rawName.Substring(2);
//                 }
//                 else
//                 {
//                     name = rawName;
//                 }
//
//                 if (paramValidationDict.TryGetValue(name, out var value)) //if it's a standard parameter
//                 {
//                     var paramType = value.Type;
//                     if (isRoundTripping && paramType.SpecialType != SpecialType.System_String)
//                     {
//                         throw new ArgumentException(
//                             $"URL {relativePath} has round-tripping parameter {rawName}, but the type of matched method parameter is {paramType.Name}. It must be a string."
//                         );
//                     }
//                     var parameterType = isRoundTripping
//                         ? ParameterType.RoundTripping
//                         : ParameterType.Normal;
//                     var restMethodParameterInfo = new RestMethodParameterInfo(name, value)
//                     {
//                         Type = parameterType
//                     };
// #if NET6_0_OR_GREATER
//                     ret.TryAdd(
//                         Array.IndexOf(parameterInfo, restMethodParameterInfo.ParameterInfo),
//                         restMethodParameterInfo
//                     );
// #else
//                     var idx = Array.IndexOf(parameterSymbol, restMethodParameterInfo.ParameterInfo);
//                     if (!ret.ContainsKey(idx))
//                     {
//                         ret.Add(idx, restMethodParameterInfo);
//                     }
// #endif
//                 }
//                 //else if it's a property on a object parameter
//                 else if (
//                     objectParamValidationDict.TryGetValue(name, out var value1)
//                     && !isRoundTripping
//                 )
//                 {
//                     var property = value1;
//                     var parameterIndex = Array.IndexOf(parameterSymbol, property.Item1);
//                     //If we already have this parameter, add additional ParameterProperty
//                     if (ret.TryGetValue(parameterIndex, out var value2))
//                     {
//                         if (!value2.IsObjectPropertyParameter)
//                         {
//                             throw new ArgumentException(
//                                 $"Parameter {property.Item1.Name} matches both a parameter and nested parameter on a parameter object"
//                             );
//                         }
//
//                         value2.ParameterProperties.Add(
//                             new RestMethodParameterProperty(name, property.Item2)
//                         );
//                     }
//                     else
//                     {
//                         var restMethodParameterInfo = new RestMethodParameterInfo(
//                             true,
//                             property.Item1
//                         );
//                         restMethodParameterInfo.ParameterProperties.Add(
//                             new RestMethodParameterProperty(name, property.Item2)
//                         );
// #if NET6_0_OR_GREATER
//                         ret.TryAdd(
//                             Array.IndexOf(parameterInfo, restMethodParameterInfo.ParameterInfo),
//                             restMethodParameterInfo
//                         );
// #else
//                         // Do the contains check
//                         var idx = Array.IndexOf(parameterSymbol, restMethodParameterInfo.ParameterInfo);
//                         if (!ret.ContainsKey(idx))
//                         {
//                             ret.Add(idx, restMethodParameterInfo);
//                         }
// #endif
//                     }
//                 }
//                 else
//                 {
//                     throw new ArgumentException(
//                         $"URL {relativePath} has parameter {rawName}, but no method parameter matches"
//                     );
//                 }
//             }
//         }
//         return ret;
//     }

     static (Dictionary<int, RestMethodParameterInfo> ret, List<ParameterFragment> fragmentList) BuildParameterMap2(
            string relativePath,
            IParameterSymbol[] parameterSymbols,
            WellKnownTypes knownTypes
        )
        {
            var ret = new Dictionary<int, RestMethodParameterInfo>();

            // This section handles pattern matching in the URL. We also need it to add parameter key/values for any attribute with a [Query]
            var parameterizedParts = ParameterRegex2.Matches(relativePath).Cast<Match>().ToArray();

            if (parameterizedParts.Length == 0)
            {
                // TODO: does this handle cases where we start with round tripping?
                if(string.IsNullOrEmpty(relativePath))
                    return (ret, new List<ParameterFragment>());

                return (ret, new List<ParameterFragment>(){new ConstantFragmentModel(relativePath)});
            }

            var paramValidationDict = parameterSymbols.ToDictionary(
                k => GetUrlNameForParameter(k, knownTypes).ToLowerInvariant(),
                v => v
            );
            //if the param is an lets make a dictionary for all it's potential parameters
            var objectParamValidationDict = parameterSymbols
                .Where(x => x.Type.IsReferenceType)
                .SelectMany(x => GetParameterProperties(x).Select(p => Tuple.Create(x, p)))
                .GroupBy(
                    i => $"{i.Item1.Name}.{GetUrlNameForProperty(i.Item2, knownTypes)}".ToLowerInvariant()
                )
                .ToDictionary(k => k.Key, v => v.First());

            var fragmentList = new List<ParameterFragment>();
            var index = 0;
            foreach (var match in parameterizedParts)
            {
                // Add constant value from given http path and continue
                if (match.Index != index)
                {
                    fragmentList.Add(new ConstantFragmentModel(relativePath.Substring(index, match.Index - index)));
                }
                index = match.Index + match.Length;

                var rawName = match.Groups[1].Value.ToLowerInvariant();
                var isRoundTripping = rawName.StartsWith("**");
                var name = isRoundTripping ? rawName.Substring(2) : rawName;

                if (paramValidationDict.TryGetValue(name, out var value)) //if it's a standard parameter
                {
                    var paramType = value.Type;
                    if (isRoundTripping && paramType.SpecialType == SpecialType.System_String)
                    {
                        throw new ArgumentException(
                            $"URL {relativePath} has round-tripping parameter {rawName}, but the type of matched method parameter is {paramType.ToDisplayString(
                                SymbolDisplayFormat.FullyQualifiedFormat
                            )}. It must be a string."
                        );
                    }
                    var parameterType = isRoundTripping
                        ? ParameterType.RoundTripping
                        : ParameterType.Normal;
                    var restMethodParameterInfo = new RestMethodParameterInfo(name, value)
                    {
                        Type = parameterType
                    };

                    var paramSymbol = restMethodParameterInfo.ParameterInfo;
                    var parameterIndex = Array.IndexOf(parameterSymbols, restMethodParameterInfo.ParameterInfo);
                    var parameterTypeDeclaration = paramSymbol.Type.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    );
                    fragmentList.Add(new DynamicFragmentModel(paramSymbol.Name, paramSymbol.Ordinal, parameterTypeDeclaration));
#if NET6_0_OR_GREATER
                    ret.TryAdd(
                        parameterIndex,
                        restMethodParameterInfo
                    );
#else
                    if (!ret.ContainsKey(parameterIndex))
                    {
                        ret.Add(parameterIndex, restMethodParameterInfo);
                    }
#endif
                }
                //else if it's a property on a object parameter
                else if (
                    objectParamValidationDict.TryGetValue(name, out var value1)
                    && !isRoundTripping
                )
                {
                    var property = value1;
                    var parameterIndex = Array.IndexOf(parameterSymbols, property.Item1);
                    //If we already have this parameter, add additional ParameterProperty
                    if (ret.TryGetValue(parameterIndex, out var value2))
                    {
                        if (!value2.IsObjectPropertyParameter)
                        {
                            throw new ArgumentException(
                                $"Parameter {property.Item1.Name} matches both a parameter and nested parameter on a parameter object"
                            );
                        }

                        value2.ParameterProperties.Add(
                            new RestMethodParameterProperty(name, property.Item2)
                        );

                        var propertyAccessExpression = $"{property.Item1.Name}.{property.Item2.Name}";
                        var containingType = property.Item2.ContainingType.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        );
                        var typeDeclaration = property.Item2.Type.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        );

                        fragmentList.Add(new DynamicPropertyFragmentModel(propertyAccessExpression, property.Item2.Name, containingType, typeDeclaration));
                    }
                    else
                    {
                        var restMethodParameterInfo = new RestMethodParameterInfo(
                            true,
                            property.Item1
                        );
                        restMethodParameterInfo.ParameterProperties.Add(
                            new RestMethodParameterProperty(name, property.Item2)
                        );

                        var idx = Array.IndexOf(parameterSymbols, restMethodParameterInfo.ParameterInfo);
                        var propertyAccessExpression = $"{property.Item1.Name}.{property.Item2.Name}";
                        var containingType = property.Item2.ContainingType.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        );
                        var typeDeclaration = property.Item2.Type.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        );
                        fragmentList.Add(new DynamicPropertyFragmentModel(propertyAccessExpression, property.Item2.Name, containingType, typeDeclaration));
#if NET6_0_OR_GREATER
                        ret.TryAdd(
                            idx,
                            restMethodParameterInfo
                        );
#else
                        // Do the contains check
                        if (!ret.ContainsKey(idx))
                        {
                            ret.Add(idx, restMethodParameterInfo);
                        }
#endif
                    }
                }
                else
                {
                    throw new ArgumentException(
                        $"URL {relativePath} has parameter {rawName}, but no method parameter matches"
                    );
                }
            }

            // add trailing string
            if (index < relativePath.Length - 1)
            {
                var s = relativePath.Substring(index, relativePath.Length - index);
                fragmentList.Add(new ConstantFragmentModel(s));
            }
            return (ret, fragmentList);
        }

     // TODO: could these two methods be merged?
    static string GetUrlNameForParameter(IParameterSymbol paramSymbol, WellKnownTypes knownTypes)
    {
        var aliasAsSymbol = knownTypes.TryGet("Refit.AliasAsAttribute");
        var aliasAttr = paramSymbol.AccessFirstOrDefault<AliasAsConfiguration>(aliasAsSymbol, knownTypes);
        return aliasAttr != null ? aliasAttr.Name : paramSymbol.Name!;
    }

    static string GetUrlNameForProperty(IPropertySymbol propSymbol, WellKnownTypes knownTypes)
    {
        var aliasAsSymbol = knownTypes.TryGet("Refit.AliasAsAttribute");
        var aliasAttr = propSymbol.AccessFirstOrDefault<AliasAsConfiguration>(aliasAsSymbol, knownTypes);
        return aliasAttr != null ? aliasAttr.Name : propSymbol.Name;
    }

    static string GetAttachmentNameForParameter(IParameterSymbol paramSymbol, WellKnownTypes knownTypes)
    {
        var attachmentNameSymbol = knownTypes.TryGet("Refit.AttachmentNameAttribute");
        var aliasAsSymbol = knownTypes.TryGet("Refit.AliasAsAttribute");

#pragma warning disable CS0618 // Type or member is obsolete
        var nameAttr = paramSymbol
            .AccessFirstOrDefault<AttachmentNameConfiguration>(attachmentNameSymbol, knownTypes);
#pragma warning restore CS0618 // Type or member is obsolete

        // also check for AliasAs
        return nameAttr?.Name
            ?? paramSymbol.AccessFirstOrDefault<AliasAsConfiguration>(aliasAsSymbol, knownTypes)?.Name!;
    }

    Tuple<BodySerializationMethod, bool, int>? FindBodyParameter(
        IParameterSymbol[] parameterArray,
        bool isMultipart,
        HttpMethod method,
        WellKnownTypes knownTypes
    )
    {
        // The body parameter is found using the following logic / order of precedence:
        // 1) [Body] attribute
        // 2) POST/PUT/PATCH: Reference type other than string
        // 3) If there are two reference types other than string, without the body attribute, throw

        var bodySymbol = knownTypes.TryGet("Refit.BodyAttribute");
        var bodyParamEnumerable = parameterArray
            .Select(
                x =>
                (
                    Parameter: x,
                    BodyAttribute: x.AccessFirstOrDefault<BodyConfiguration>(bodySymbol, knownTypes)
                )
            )
            .Where(x => x.BodyAttribute != null)
            .ToArray();

        // multipart requests may not contain a body, implicit or explicit
        if (isMultipart)
        {
            if (bodyParamEnumerable.Length > 0)
            {
                throw new ArgumentException(
                    "Multipart requests may not contain a Body parameter"
                );
            }
            return null;
        }

        if (bodyParamEnumerable.Length > 1)
        {
            throw new ArgumentException("Only one parameter can be a Body parameter");
        }

        // #1, body attribute wins
        if (bodyParamEnumerable.Length == 1)
        {
            var bodyParam = bodyParamEnumerable.First();

            // TODO: move logic to runtime
            // return Tuple.Create(
            //     bodyParam!.BodyAttribute!.SerializationMethod,
            //     bodyParam.BodyAttribute.Buffered ?? RefitSettings.Buffered,
            //     Array.IndexOf(parameterArray, bodyParam.Parameter)
            // );
            // TODO: this default to false

            return Tuple.Create(
                bodyParam!.BodyAttribute!.SerializationMethod,
                bodyParam.BodyAttribute.Buffered ?? false,
                Array.IndexOf(parameterArray, bodyParam.Parameter)
            );
        }

        // TODO: no idea if this works with derived attributes
        // Not in post/put/patch? bail
        if (
            !method.Equals(HttpMethod.Post)
            && !method.Equals(HttpMethod.Put)
            && !method.Equals(PatchMethod)
        )
        {
            return null;
        }

        var querySymbol = knownTypes.TryGet("Refit.QueryAttribute");
        var headerCollectionSymbol = knownTypes.TryGet("Refit.HeaderCollectionAttribute");
        var propertySymbol = knownTypes.TryGet("Refit.PropertyAttribute");

        // see if we're a post/put/patch
        // explicitly skip [Query], [HeaderCollection], and [Property]-denoted params
        var refParamEnumerable = parameterArray
            .Where(
                pi =>
                    !pi.Type.IsValueType
                    && pi.Type.SpecialType != SpecialType.System_String
                    && pi.AccessFirstOrDefault<QueryConfiguration>(querySymbol, knownTypes) == null
                    && pi.AccessFirstOrDefault<HeaderCollectionConfiguration>(headerCollectionSymbol, knownTypes) == null
                    && pi.AccessFirstOrDefault<PropertyConfiguration>(propertySymbol, knownTypes) == null
            )
            .ToArray();

        // Check for rule #3
        if (refParamEnumerable.Length > 1)
        {
            throw new ArgumentException(
                "Multiple complex types found. Specify one parameter as the body using BodyAttribute"
            );
        }

        if (refParamEnumerable.Length == 1)
        {
            var refParam = refParamEnumerable.First();
            // TODO: move RefitSettings logic to runtime.
            // return Tuple.Create(
            //     BodySerializationMethod.Serialized,
            //     RefitSettings.Buffered,
            //     Array.IndexOf(parameterArray, refParam!)
            // );

            return Tuple.Create(
                BodySerializationMethod.Serialized,
                false,
                Array.IndexOf(parameterArray, refParam!)
            );
        }

        return null;
    }

    static Tuple<string, int>? FindAuthorizationParameter(IParameterSymbol[] parameterArray, WellKnownTypes knownTypes)
    {
        var authorizeSymbol = knownTypes.TryGet("Refit.AuthorizeAttribute");
        var authorizeParams = parameterArray
            .Select(
                x =>
                (
                    Parameter: x,
                    AuthorizeAttribute: x.AccessFirstOrDefault<AuthorizeConfiguration>(authorizeSymbol, knownTypes)
                )
            )
            .Where(x => x.AuthorizeAttribute != null)
            .ToArray();

        if (authorizeParams.Length > 1)
        {
            throw new ArgumentException("Only one parameter can be an Authorize parameter");
        }

        if (authorizeParams.Length == 1)
        {
            var authorizeParam = authorizeParams.First();
            return Tuple.Create(
                authorizeParam!.AuthorizeAttribute!.Scheme,
                    Array.IndexOf(parameterArray, authorizeParam.Parameter)
            );
        }

        return null;
    }

    static Dictionary<string, string?> ParseHeaders(IMethodSymbol methodSymbol, WellKnownTypes knownTypes)
    {
        var headersSymbol = knownTypes.TryGet("Refit.HeadersAttribute");
        var inheritedAttributes =
            methodSymbol.ContainingType != null
                ? methodSymbol
                    .ContainingType.AllInterfaces
                    .SelectMany(i => i.Access<HeadersConfiguration>(headersSymbol, knownTypes))
                    .Reverse()
                : [];

        var declaringTypeAttributes = methodSymbol.ContainingType!.Access<HeadersConfiguration>(headersSymbol,knownTypes);

        // Headers set on the declaring type have to come first,
        // so headers set on the method can replace them. Switching
        // the order here will break stuff.
        var headers = inheritedAttributes
            .Concat(declaringTypeAttributes)
            .Concat(methodSymbol.Access<HeadersConfiguration>(headersSymbol ,knownTypes))
            .SelectMany(ha => ha.Headers);

        var ret = new Dictionary<string, string?>();

        foreach (var header in headers)
        {
            if (string.IsNullOrWhiteSpace(header))
                continue;

            // NB: Silverlight doesn't have an overload for String.Split()
            // with a count parameter, but header values can contain
            // ':' so we have to re-join all but the first part to get the
            // value.

            var parsedHeader = EnsureSafe(header);
            var parts = parsedHeader.Split(':');
            ret[parts[0].Trim()] =
                parts.Length > 1 ? string.Join(":", parts.Skip(1)).Trim() : null;
        }

        return ret;
    }

    static Dictionary<int, string> BuildHeaderParameterMap(IParameterSymbol[] parameterArray, WellKnownTypes knownTypes)
    {
        var ret = new System.Collections.Generic.Dictionary<int, string>();
        var headerSymbol = knownTypes.TryGet("Refit.HeaderAttribute");

        for (var i = 0; i < parameterArray.Length; i++)
        {
            var headerAttribute = parameterArray[i]
                .AccessFirstOrDefault<HeaderConfiguration>(headerSymbol, knownTypes);

            var header = headerAttribute?.Header;

            if (!string.IsNullOrWhiteSpace(header))
            {
                ret[i] = header!.Trim();
            }
        }

        return ret;
    }

    // TODO: maybe merge with similar code for query
    // TODO: is it safe to use parameters by name without using @?
    // TODO: does parsing the Headers interfaces work the same as runtime reflection?
    // TODO: properly escape strings
    // TODO: adding " here is a bad idea
    // TODO: check overlapping strings

    List<HeaderPsModel> BuildHeaderPsModel()
    {
        var headersToAdd = new System.Collections.Generic.List<HeaderPsModel>();

        foreach (var pair in Headers)
        {
            headersToAdd.Add(
                new HeaderPsModel(HeaderType.Static, new HeaderModel($""" "{pair.Key}" """, $""" "{pair.Value}" """), null,
                    null, null));
        }

        for (var i = 0; i < ParameterSymbolArray.Length; i++)
        {
            var isParameterMappedToRequest = false;
            var param = ParameterSymbolArray[i];
            RestMethodParameterInfo? parameterInfo = null;

            // if part of REST resource URL, substitute it in
            if (this.ParameterMap.TryGetValue(i, out var parameterMapValue))
            {
                parameterInfo = parameterMapValue;
                if (!parameterInfo.IsObjectPropertyParameter)
                {
                    // mark parameter mapped if not an object
                    // we want objects to fall through so any parameters on this object not bound here get passed as query parameters
                    isParameterMappedToRequest = true;
                }
            }

            // if marked as body, add to content
            if (
                this.BodyParameterInfo != null
                && this.BodyParameterInfo.Item3 == i
            )
            {
                // AddBodyToRequest(restMethod, param, ret);
                isParameterMappedToRequest = true;
            }

            // if header, add to request headers
            if (this.HeaderParameterMap.TryGetValue(i, out var headerParameterValue))
            {
                headersToAdd.Add(
                    new HeaderPsModel(HeaderType.Static, new HeaderModel($""" "{headerParameterValue}" """, param.Name), null,
                        null, null));
                isParameterMappedToRequest = true;
            }

            //if header collection, add to request headers
            if (this.HeaderCollectionAt(i))
            {
                headersToAdd.Add(
                    new HeaderPsModel(HeaderType.Collection, null, null, param.Name, null));

                isParameterMappedToRequest = true;
            }

            //if authorize, add to request headers with scheme
            if (
                this.AuthorizeParameterInfo != null
                && this.AuthorizeParameterInfo.Item2 == i
            )
            {
                // headersToAdd["Authorization"] =
                //     $"{this.AuthorizeParameterInfo.Item1} {param}";
                //
                headersToAdd.Add(
                    new HeaderPsModel(HeaderType.Authorise, null, null,null, new AuthoriseModel(ParameterSymbolArray[this.AuthorizeParameterInfo.Item2].Name, this.AuthorizeParameterInfo.Item1)));

                isParameterMappedToRequest = true;
            }
        }

        return headersToAdd;

    }

    void DetermineReturnTypeInfo(IMethodSymbol methodInfo, WellKnownTypes knownTypes)
    {
        var unboundTaskSymbol = knownTypes.Get(typeof(Task<>));
        var valueTaskSymbol = knownTypes.Get(typeof(ValueTask<>));
        var observableSymbol = knownTypes.Get(typeof(IObservable<>));

        var taskSymbol = knownTypes.Get<Task>();

        var returnType = methodInfo.ReturnType;
        if (
            returnType is INamedTypeSymbol { IsGenericType: true } namedType
            && (
                SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, unboundTaskSymbol)
                || SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, valueTaskSymbol)
                || SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, observableSymbol)
            )
        )
        {
            ReturnType = returnType;
            ReturnResultType = namedType.TypeArguments[0];

            var unboundApiResponseSymbol = knownTypes.TryGet("Refit.ApiResponse`1");
            var unboundIApiResponseSymbol = knownTypes.TryGet("Refit.IApiResponse`1");

            var iApiResponseSymbol = knownTypes.TryGet("Refit.IApiResponse");

            // TODO: maybe use inherits from here?
            // Does refit support types inheriting from IApiResponse
            if (
                ReturnResultType is INamedTypeSymbol {IsGenericType: true} returnResultNamedType
                && (
                    SymbolEqualityComparer.Default.Equals(returnResultNamedType.OriginalDefinition, unboundApiResponseSymbol)
                    || SymbolEqualityComparer.Default.Equals(returnResultNamedType.OriginalDefinition, unboundIApiResponseSymbol)
                )
            )
            {
                DeserializedResultType = returnResultNamedType.TypeArguments[0];
            }
            else if (SymbolEqualityComparer.Default.Equals(ReturnResultType, iApiResponseSymbol))
            {
                DeserializedResultType = knownTypes.Get<HttpContent>();
            }
            else
                DeserializedResultType = ReturnResultType;
        }
        else if (SymbolEqualityComparer.Default.Equals(returnType, taskSymbol))
        {
            var voidSymbol = knownTypes.Get(typeof(void));
            ReturnType = methodInfo.ReturnType;
            ReturnResultType = voidSymbol;
            DeserializedResultType = voidSymbol;
        }
        else
            throw new ArgumentException(
                $"Method \"{methodInfo.Name}\" is invalid. All REST Methods must return either Task<T> or ValueTask<T> or IObservable<T>"
            );
    }

    void DetermineIfResponseMustBeDisposed(WellKnownTypes knownTypes)
    {
        // Rest method caller will have to dispose if it's one of those 3
        var httpResponseSymbol = knownTypes.Get<HttpResponseMessage>();
        var httpContentSymbol = knownTypes.Get<HttpContent>();
        var streamSymbol = knownTypes.Get<Stream>();

        ShouldDisposeResponse =
            (!SymbolEqualityComparer.Default.Equals(DeserializedResultType, httpResponseSymbol))
            && (!SymbolEqualityComparer.Default.Equals(DeserializedResultType, httpContentSymbol))
            && (!SymbolEqualityComparer.Default.Equals(DeserializedResultType, streamSymbol));
    }

    static string EnsureSafe(string value)
    {
        // Remove CR and LF characters
#pragma warning disable CA1307 // Specify StringComparison for clarity
        return value.Replace("\r", string.Empty).Replace("\n", string.Empty);
#pragma warning restore CA1307 // Specify StringComparison for clarity
    }
}

/// <summary>
/// RestMethodParameterInfo.
/// </summary>
public class RestMethodParameterInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestMethodParameterInfo"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="parameterInfo">The parameter information.</param>
    public RestMethodParameterInfo(string name, IParameterSymbol parameterInfo)
    {
        Name = name;
        ParameterInfo = parameterInfo;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestMethodParameterInfo"/> class.
    /// </summary>
    /// <param name="isObjectPropertyParameter">if set to <c>true</c> [is object property parameter].</param>
    /// <param name="parameterInfo">The parameter information.</param>
    public RestMethodParameterInfo(bool isObjectPropertyParameter, IParameterSymbol parameterInfo)
    {
        IsObjectPropertyParameter = isObjectPropertyParameter;
        ParameterInfo = parameterInfo;
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the parameter information.
    /// </summary>
    /// <value>
    /// The parameter information.
    /// </value>
    public IParameterSymbol ParameterInfo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is object property parameter.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is object property parameter; otherwise, <c>false</c>.
    /// </value>
    public bool IsObjectPropertyParameter { get; set; }

    /// <summary>
    /// Gets or sets the parameter properties.
    /// </summary>
    /// <value>
    /// The parameter properties.
    /// </value>
    public List<RestMethodParameterProperty> ParameterProperties { get; set; } = [];

    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    /// <value>
    /// The type.
    /// </value>
    public ParameterType Type { get; set; } = ParameterType.Normal;
}

/// <summary>
/// RestMethodParameterProperty.
/// </summary>
public class RestMethodParameterProperty
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestMethodParameterProperty"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="propertyInfo">The property information.</param>
    public RestMethodParameterProperty(string name, IPropertySymbol propertyInfo)
    {
        Name = name;
        PropertyInfo = propertyInfo;
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the property information.
    /// </summary>
    /// <value>
    /// The property information.
    /// </value>
    public IPropertySymbol PropertyInfo { get; set; }
}

/// <summary>
/// ParameterType.
/// </summary>
public enum ParameterType
{
    /// <summary>
    /// The normal
    /// </summary>
    Normal,

    /// <summary>
    /// The round tripping
    /// </summary>
    RoundTripping
}
