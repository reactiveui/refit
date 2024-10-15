using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Text;

using Refit.Generator.Configuration;

namespace Refit.Generator;

static class EmitRefitBody
{
    // TODO: replace default with CancellationToken.None
    // TODO: should I make this an instance and store information as properties
    // Alternatively I could use a context object passed to each method :thinking:
    const string InnerMethodName = "Inner";
    const string RequestName = "request";
    const string SettingsExpression = "this.requestBuilder.Settings";

    // TODO: use UniqueNameBuilder
    public static void WriteRefitBody(StringBuilder source, MethodModel methodModel, UniqueNameBuilder uniqueNames)
    {
        var methodScope = uniqueNames.NewScope();
        var innerMethodName = methodScope.New(InnerMethodName);

        WriteCreateRequestMethod(source, methodModel.RefitBody!, methodScope, innerMethodName);

        var requestName = methodScope.New(RequestName);

        source.AppendLine(
            $"""

                          var {requestName} = {innerMethodName}();
              """);
        WriteReturn(source, methodModel, uniqueNames, requestName);
    }

    static void WriteCreateRequestMethod(StringBuilder source, RefitBodyModel model, UniqueNameBuilder uniqueNames,
        string innerMethodName)
    {
        uniqueNames = uniqueNames.NewScope();

        var requestName = uniqueNames.New(RequestName);
        source.Append(
            $$"""

                          global::System.Net.Http.HttpRequestMessage {{innerMethodName}}()
                          {
                              var {{requestName}} = new global::System.Net.Http.HttpRequestMessage() { Method = {{HttpMethodToEnumString(model.HttpMethod)}} };

              """);

        // TryWriteMultiPartInit(source, model, requestName);
        TryWriteBody(source, model, requestName);
        // need to run multi part attachment here.
        TryWriteHeaders(source, model, requestName);

        WriteProperties(source, model, requestName);
        WriteVersion(source, model, requestName);

        WriteBuildUrl(source, model, requestName, uniqueNames);
        source.AppendLine(
            $$"""

                              return {{requestName}};
                          }
              """);
    }

    static string HttpMethodToEnumString(HttpMethod method)
    {
        if (method == HttpMethod.Get)
        {
            return "global::System.Net.Http.HttpMethod.Get";
        }
        else if (method == HttpMethod.Post)
        {
            return "global::System.Net.Http.HttpMethod.Post";
        }
        else if (method == HttpMethod.Put)
        {
            return "global::System.Net.Http.HttpMethod.Put";
        }
        else if (method == HttpMethod.Delete)
        {
            return "global::System.Net.Http.HttpMethod.Delete";
        }
        else if (method == new HttpMethod("PATCH"))
        {
            return "global::Refit.RefitHelper.Patch";
        }
        else if (method == HttpMethod.Options)
        {
            return "global::System.Net.Http.HttpMethod.Options";
        }
        else if (method == HttpMethod.Head)
        {
            return "global::System.Net.Http.HttpMethod.Head";
        }

        throw new NotImplementedException();
    }

    // TODO: make into scope names
    static string? WriteParameterInfoArray(StringBuilder source, RefitBodyModel model,
        UniqueNameBuilder uniqueNames)
    {
        throw new NotImplementedException();
        // if no usage of ParameterInfo then exit early
        if (model.QueryParameters.Count == 0 && model.UrlFragments.OfType<ConstantFragmentModel>().Count() == model.UrlFragments.Count)
        {
            return null;
        }

        // TODO: implement
        var fieldName = uniqueNames.New("__parameterInfo");
        // source.Append($"global::System.Reflection.ParameterInfo[] {fieldName} = {model.}")
        return null;
    }

    static void TryWriteMultiPartInit(StringBuilder source, RefitBodyModel model, string requestName)
    {
        if(model.MultipartBoundary is null)
            return;

        source.Append(
$$"""
                    throw new NotImplementedException("MultiPart");
                  {{requestName}}.Content = new global::System.Net.Http.MultipartFormDataContent({{model.MultipartBoundary}});
  """);
    }

    static void TryWriteBody(StringBuilder source, RefitBodyModel model, string requestName)
    {
        if(model.BodyParameter is null)
            return;
        var isBuffered = WriteBool(model.BodyParameter.Buffered);
        var serializationMethod = model.BodyParameter.SerializationMethod switch
        {
            BodySerializationMethod.Default => "global::Refit.BodySerializationMethod.Default",
            BodySerializationMethod.Json => "global::Refit.BodySerializationMethod.Json",
            BodySerializationMethod.UrlEncoded => "global::Refit.BodySerializationMethod.UrlEncoded",
            BodySerializationMethod.Serialized => "global::Refit.BodySerializationMethod.Serialized",
        };

        // TODO: use full alias for type
        source.Append(
            $$"""

                              global::Refit.RefitHelper.AddBody({{requestName}}, {{SettingsExpression}}, {{model.BodyParameter.Parameter}}, {{isBuffered}}, {{serializationMethod}});
              """);
    }

    static void TryWriteHeaders(StringBuilder source, RefitBodyModel model, string requestName)
    {
        if (model.HeaderPs.Count == 0)
        {
            return;
        }

        source.AppendLine(
            $$"""

                              {{requestName}}.Content = new global::System.Net.Http.ByteArrayContent([]);
              """);

        foreach (var headerPs in model.HeaderPs)
        {
            if (headerPs.Type == HeaderType.Static)
            {
                source.AppendLine(
                 $$"""
                                   global::Refit.RefitHelper.AddHeader({{requestName}}, {{headerPs.Static.Value.Key}}, {{headerPs.Static.Value.Value}}.ToString());
                   """);
            }
            else if (headerPs.Type == HeaderType.Collection)
            {
                source.AppendLine(
                    $$"""
                                      global::Refit.RefitHelper.AddHeaderCollection({{requestName}}, {{model.HeaderCollectionParam}});
                      """);
            }
            else if(headerPs.Type == HeaderType.Authorise)
            {
                source.AppendLine(
                    $$"""
                                      global::Refit.RefitHelper.AddHeader({{requestName}}, "Authorization", $"{{headerPs.Authorise.Value.Scheme}} {{{headerPs.Authorise.Value.Parameter}}.ToString()}");
                      """);
            }
        }
//         // TODO: implement
//         // if no method headers, parameter headers or header collections don't emit
//         if (model.Headers.Count == 0 && model.HeaderParameters.Count == 0)
//         {
//             if(model.HeaderCollectionParam is null)
//                 return;
//
//             // TODO: ensure that AddHeaderCollection adds content
//             source.AppendLine(
//                 $$"""
//                                   global::Refit.RefitHelper.AddHeaderCollection({{requestName}}, {{model.HeaderCollectionParam}});
//                   """);
//             return;
//         }
//
//         // TODO: only emit if http method can have a body
//         source.AppendLine(
//             $$"""
//                               global::Refit.RefitHelper.SetContentForHeaders({{requestName}}, );
//               """);
//
//         foreach (var methodHeader in model.Headers)
//         {
//             source.AppendLine(
//                 $$"""
//                                   global::Refit.RefitHelper.AddHeader({{requestName}}, {{methodHeader.Key}}, {{methodHeader.Value}});
//                   """);
//         }
//
//         foreach (var parameterHeader in model.HeaderParameters)
//         {
//             source.AppendLine(
//                 $$"""
//                                   global::Refit.RefitHelper.AddHeader({{requestName}}, {{parameterHeader.HeaderKey}}, {{parameterHeader.Parameter}});
//                   """);
//         }
    }


    static void WriteProperties(StringBuilder source, RefitBodyModel refitModel, string requestName)
    {
        // add refit settings properties
        source.AppendLine(
$"""

                  global::Refit.RefitHelper.WriteRefitSettingsProperties({requestName}, {SettingsExpression});
  """);

        // add each property
        foreach (var property in refitModel.Properties)
        {
            source.AppendLine(
                $"""                global::Refit.RefitHelper.WriteProperty({requestName}, "{property.Key}", {property.Parameter});""");
        }

        // TODO: implement add top level types
        // TODO: what is a top level type?????? What was I talking about?
        // TODO: need to pass down interface type name and create a proprety for the method info :(
        // I could prolly create a static instance for the latter
        source.AppendLine(
            $"""
                              global::Refit.RefitHelper.AddTopLevelTypes({requestName}, null, null);
              """);
    }

    static void WriteVersion(StringBuilder source, RefitBodyModel model, string requestName)
    {
        source.AppendLine(
            $"""
                              global::Refit.RefitHelper.AddVersionToRequest({requestName}, {SettingsExpression});
              """);
    }

    static void WriteBuildUrl(StringBuilder source, RefitBodyModel model, string requestName, UniqueNameBuilder uniqueName)
    {
        // TODO: why is this assertion here
        // Debug.Assert(model.UrlFragments.Count > 1);
        if (model.UrlFragments.Count == 1 && model.QueryParameters.Count == 0)
        {
            Debug.Assert(model.UrlFragments[0] is ConstantFragmentModel);
            var constant = model.UrlFragments[0] as ConstantFragmentModel;

            // TODO: emit static reusable uri for constant uris
            // TODO: uri stripping logic could be improved
            // TODO: consider base addresses with path and queries, does it break this
            // TODO: do urls containing " break this?
            // TODO: get queryUriFormat
              source.AppendLine(
                  $$"""
                                    var basePath = Client.BaseAddress.AbsolutePath == "/" ? string.Empty : Client.BaseAddress.AbsolutePath;
                                    var uri = new UriBuilder(new Uri(global::Refit.RefitHelper.BaseUri, $"{basePath}{{constant!.Value}}"));

                                    {{requestName}}.RequestUri = new Uri(
                                        uri.Uri.GetComponents(global::System.UriComponents.PathAndQuery, global::System.UriFormat.{{model.UriFormat.ToString()}}),
                                        UriKind.Relative
                                    );
                    """);
              return;
        }

        // TODO: uniqueName for vsb & RefitSettings
        // add version to request
        source.AppendLine(
            $"""

                              var vsb = new ValueStringBuilder(stackalloc char[256]);
                              vsb.Append(Client.BaseAddress.AbsolutePath == "/" ? string.Empty : Client.BaseAddress.AbsolutePath);
              """);
        // TODO: add initial section to url
        // TODO: add get static info

        foreach (var fragment in model.UrlFragments)
        {
            if (fragment is ConstantFragmentModel constant)
            {
                source.AppendLine(
                    $$"""
                                      vsb.Append("{{constant.Value}}");
                      """);
                continue;
            }

            if (fragment is DynamicFragmentModel dynamic)
            {
                source.AppendLine(
                    $$"""
                                      global::Refit.RefitHelper.AddUrlFragment(ref vsb, {{dynamic.Access}}, {{SettingsExpression}}, typeof({{dynamic.TypeDeclaration}}));
                      """);
                continue;
            }

            if (fragment is DynamicRoundTripFragmentModel roundTrip)
            {
                source.AppendLine(
                    $$"""
                                      global::Refit.RefitHelper.AddRoundTripUrlFragment(ref vsb, {{SettingsExpression}}, typeof({{roundTrip.TypeDeclaration}}));
                      """);
                continue;
            }

            if (fragment is DynamicPropertyFragmentModel dynamicProperty)
            {
                source.AppendLine(
                    $$"""
                                      global::Refit.RefitHelper.AddPropertyFragment(ref vsb, {{SettingsExpression}}, typeof({{dynamicProperty.TypeDeclaration}}));
                      """);
                continue;
            }
        }

        if (model.QueryParameters.Count > 0)
        {
            source.AppendLine(
                  """                vsb.Append("?");""");
        }

        for (int i = 0; i < model.QueryParameters.Count; i++)
        {
            var query = model.QueryParameters[i];

            if (i > 0)
            {
                source.AppendLine(
                    """                vsb.Append("&");""");
            }

            // TODO: create overload for a default or non existent QueryAttribute?
            // TODO: escape params
            source.AppendLine(
                $$"""
                                  global::Refit.RefitHelper.AddQueryObject(ref vsb, {{SettingsExpression}}, "{{query.Parameter}}", {{query.Parameter}});
                  """);
        }

        source.AppendLine(
            $$"""

                              var uri = new UriBuilder(new Uri(global::Refit.RefitHelper.BaseUri, vsb.ToString()));

                              {{requestName}}.RequestUri = new Uri(
                                  uri.Uri.GetComponents(global::System.UriComponents.PathAndQuery, global::System.UriFormat.{{model.UriFormat.ToString()}}),
                                  UriKind.Relative
                              );
              """);

    }

    static void WriteReturn(StringBuilder source, MethodModel model,
        UniqueNameBuilder uniqueNames,
        string requestExpression)
    {
        var refitModel = model.RefitBody!;

        // TODO: return type needs to support the inner type of Task<T>
        var responseExpression = uniqueNames.New("response");
        var cacellationTokenExpression = refitModel.CancellationTokenParam ?? "default";

        if (model.ReturnTypeMetadata == ReturnTypeInfo.AsyncVoid)
        {
            source.AppendLine(
                $"""
                             await global::Refit.RefitHelper.SendVoidTaskAsync({requestExpression}, Client, {SettingsExpression}, {cacellationTokenExpression});
                 """);
        }
        else if (model.ReturnTypeMetadata == ReturnTypeInfo.AsyncResult && !refitModel.IsApiResponse)
        {
            source.AppendLine(
                $"""
                             return await global::Refit.RefitHelper.SendTaskResultAsync<{refitModel.GenericInnerReturnType}>({requestExpression}, Client, {SettingsExpression}, {WriteBool(refitModel.BodyParameter?.Buffered)}, {cacellationTokenExpression});
                 """);
        }
        else if (model.ReturnTypeMetadata == ReturnTypeInfo.AsyncResult && refitModel.IsApiResponse)
        {
            source.AppendLine(
                $"""
                             return await global::Refit.RefitHelper.SendTaskIApiResultAsync<{refitModel.GenericInnerReturnType}, {refitModel.DeserializedResultType}>({requestExpression}, Client, {SettingsExpression}, {WriteBool(refitModel.BodyParameter?.Buffered)}, {cacellationTokenExpression});
                 """);
        }
        else
        {
                  // TODO: this should be an extracted
                  // TODO: is ReturnTypeMetadata broken?
                  // TODO: if return insert throw? should this be done in refitmodel and just emit fail
                  // TODO: use uniqueNameBuilder on all identifiers
                  // TODO: emit TaskToObservab
                  source.AppendLine(
                  $$"""
                             return new global::Refit.RequestBuilderImplementation.TaskToObservable<{{refitModel.GenericInnerReturnType}}>(ct =>
                             {
                  """);

            var ctToken = refitModel.CancellationTokenParam is null ? "ct" : "cts";
            if (refitModel.CancellationTokenParam is not null)
            {
                source.AppendLine(
                    $$"""
                                     var cts = global::System.Threading.CancellationTokenSource.CreateLinkedTokenSource(methodCt, {{refitModel.CancellationTokenParam}});
                      """);
            }

            if (refitModel.IsApiResponse)
            {
                source.AppendLine(
                    $"""
                                 return global::Refit.RefitHelper.SendTaskIApiResultAsync<{refitModel.GenericInnerReturnType}, {refitModel.DeserializedResultType}>({requestExpression}, Client, {SettingsExpression}, {WriteBool(refitModel.BodyParameter?.Buffered)}, {ctToken});
                     """);
            }
            else
            {
                source.AppendLine(
                    $"""
                                 return global::Refit.RefitHelper.SendTaskResultAsync<{refitModel.GenericInnerReturnType}>({requestExpression}, Client, {SettingsExpression}, {WriteBool(refitModel.BodyParameter?.Buffered)}, {ctToken});
                     """);
            }


            source.AppendLine(
                """           });""");
        }
    }

    static string WriteBool(bool? value)
    {
        return value is null ? "false" : value.Value ? "true" : "false";
    }
}
