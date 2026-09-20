// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Refit.Generator;

/// <summary>Emits inline request-construction source for generated Refit method implementations.</summary>
/// <content>Emits a method that returns <c>PagedEnumerable</c>: a member that builds one page's request, and the method
/// that hands it to the paging runtime.</content>
internal static partial class Emitter
{
    /// <summary>The fully qualified request message type returned by a page-request member.</summary>
    private const string HttpRequestMessageType = "global::System.Net.Http.HttpRequestMessage";

    /// <summary>The opening of a generated <c>PagedEnumerable</c> factory call.</summary>
    private const string PagedEnumerableFactory = "global::Refit.PagedEnumerable.";

    /// <summary>The opening of a generated <c>PageContinuation.To</c> call up to the type argument.</summary>
    private const string PageContinuationTo = "global::Refit.PageContinuation.To<";

    /// <summary>The opening of a generated <c>GeneratedPaging</c> helper call.</summary>
    private const string GeneratedPagingHelper = "global::Refit.GeneratedPaging.";

    /// <summary>Builds a method that returns <c>PagedEnumerable</c>: a sibling member that builds each page's request, and
    /// the method itself, which sends one request per page.</summary>
    /// <param name="builder">The buffer accumulating the interface's generated method source.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="isExplicit">Whether the method is emitted as an explicit interface implementation.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="enumFormatterScope">The enum formatter scope for the interface.</param>
    /// <remarks>The sibling reuses the standard request construction, so a paged method binds path, query, header and body
    /// parameters exactly like any other method. Each page calls it again, because a request cannot be sent twice.</remarks>
    internal static void BuildInlinePagedRefitMethod(
        PooledStringBuilder builder,
        in MethodModel methodModel,
        InterfaceModel interfaceModel,
        bool isExplicit,
        string settingsFieldName,
        UniqueNameBuilder uniqueNames,
        EnumFormatterScope enumFormatterScope)
    {
        var requestMethodName = uniqueNames.New($"BuildRefit{StripExplicitInterfacePrefix(methodModel.Name)}PageRequest");
        var requestModel = methodModel with
        {
            ReturnType = HttpRequestMessageType,
            ReturnTypeMetadata = ReturnTypeInfo.PageRequest,
            DeclaredMethod = requestMethodName,
            IsExplicitInterface = false,
        };

        BuildInlineRefitMethod(builder, requestModel, interfaceModel, true, settingsFieldName, uniqueNames, enumFormatterScope);
        AppendPagedMethod(builder, methodModel, interfaceModel, isExplicit, settingsFieldName, requestMethodName);
    }

    /// <summary>Appends the statement that returns a built request from a page-request member.</summary>
    /// <param name="builder">The buffer accumulating the interface's generated method source.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void AppendInlinePageRequestReturn(PooledStringBuilder builder, string requestLocal) =>
        builder.Append(Indent(MethodBodyIndentation)).Append(ReturnStatementPrefix).Append(requestLocal).AppendLine(";");

    /// <summary>Appends the method that returns <c>PagedEnumerable</c> and sends one request per page.</summary>
    /// <param name="builder">The buffer accumulating the interface's generated method source.</param>
    /// <param name="methodModel">The paged method model.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="isExplicit">Whether the method is emitted as an explicit interface implementation.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    /// <param name="requestMethodName">The sibling member that builds one page's request.</param>
    internal static void AppendPagedMethod(
        PooledStringBuilder builder,
        in MethodModel methodModel,
        InterfaceModel interfaceModel,
        bool isExplicit,
        string settingsFieldName,
        string requestMethodName)
    {
        var paging = methodModel.Request.Paging!.Value;
        var locals = CreateMethodLocalNameBuilder(methodModel.Parameters);
        var names = new PagedNames(
            locals.New("refitSettings"),
            locals.New(paging.Kind == PagingKind.Link ? "refitPageLink" : "refitPageToken"),
            locals.New("refitPageCancellation"),
            locals.New("refitPage"),
            locals.New("refitRequest"),
            interfaceModel.SupportsStaticLambdas ? "static " : string.Empty);
        var bodyIndent = Indent(MethodBodyIndentation);
        var argumentIndent = Indent(MethodBodyIndentation + 1);

        AppendMethodOpening(builder, methodModel, isExplicit, isExplicit, interfaceModel.SupportsNullable);
        _ = builder
            .Append(bodyIndent).Append("var ").Append(names.Settings).Append(" = ").Append(settingsFieldName).AppendLine(";")
            .Append(bodyIndent).Append(ReturnStatementPrefix).Append(PagedEnumerableFactory);
        if (paging.Kind == PagingKind.Token)
        {
            _ = builder.Append("Create<").Append(paging.PageType).Append(", ").Append(paging.ItemType).Append(", ").Append(paging.TokenType).AppendLine(">(")
                .Append(argumentIndent).Append('@').Append(paging.TokenParameter).AppendLine(",");
        }
        else
        {
            _ = builder.Append("FromLinks<").Append(paging.PageType).Append(", ").Append(paging.ItemType).AppendLine(">(")
                .Append(argumentIndent).Append(BuildOriginPolicyExpression(paging)).AppendLine(",");
        }

        _ = builder
            .Append(argumentIndent).Append(BuildPageFetchExpression(methodModel, paging, names, requestMethodName, argumentIndent)).AppendLine(",")
            .Append(argumentIndent).Append(names.LambdaModifier).Append(names.Page).Append(" => ").Append(names.Page).Append(paging.ItemsAccess).AppendLine(",")
            .Append(argumentIndent).Append(BuildPageContinuationExpression(paging, names)).AppendLine(");")
            .Append(Indent(MethodMemberIndentation)).AppendLine("}");
    }

    /// <summary>Builds the lambda that requests one page.</summary>
    /// <param name="methodModel">The paged method model.</param>
    /// <param name="paging">The paging model.</param>
    /// <param name="names">The lambda parameter and local names.</param>
    /// <param name="requestMethodName">The sibling member that builds one page's request.</param>
    /// <param name="argumentIndent">The indentation of the enclosing argument.</param>
    /// <returns>The lambda, which takes the page's token or link and a cancellation token.</returns>
    /// <remarks>A link names the whole URI of the page, so it replaces the URI of the request that the method's own
    /// parameters built; the request is otherwise built the same way for the first page.</remarks>
    internal static string BuildPageFetchExpression(
        in MethodModel methodModel,
        in PagingModel paging,
        in PagedNames names,
        string requestMethodName,
        string argumentIndent)
    {
        var isLink = paging.Kind == PagingKind.Link;
        var requestCall = $"this.{requestMethodName}({BuildPageRequestArguments(methodModel.Parameters, paging, isLink ? null : names.Token)})";
        var parameters = $"({names.Token}, {names.Cancellation}) =>";
        if (!isLink)
        {
            return $"{parameters} {BuildPageSend(methodModel.Request, requestCall, names, argumentIndent + Indent(1))}";
        }

        var bodyIndent = argumentIndent + Indent(1);
        return string.Concat(
            parameters,
            "\n",
            argumentIndent,
            "{\n",
            bodyIndent,
            "var ",
            names.Request,
            " = ",
            requestCall,
            ";\n",
            bodyIndent,
            "if (",
            names.Token,
            " != null)\n",
            bodyIndent,
            "{\n",
            bodyIndent,
            Indent(1),
            names.Request,
            ".RequestUri = ",
            names.Token,
            ";\n",
            bodyIndent,
            "}\n\n",
            bodyIndent,
            ReturnStatementPrefix,
            BuildPageSend(methodModel.Request, names.Request, names, bodyIndent + Indent(1)),
            ";\n",
            argumentIndent,
            "}");
    }

    /// <summary>Builds the call that sends one page's request and deserializes the page.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="requestExpression">The expression that yields the request to send.</param>
    /// <param name="names">The lambda parameter and local names.</param>
    /// <param name="argumentIndent">The indentation of the call's arguments.</param>
    /// <returns>The <c>SendAsync</c> call.</returns>
    internal static string BuildPageSend(in RequestModel request, string requestExpression, in PagedNames names, string argumentIndent)
    {
        var arguments = new[]
        {
            "this.Client",
            requestExpression,
            names.Settings,
            ToLowerInvariantString(request.IsApiResponse),
            ToLowerInvariantString(request.ShouldDisposeResponse),
            BuildBufferBodyExpression(FindRequestParameter(request, RequestParameterKind.Body), names.Settings),
            names.Cancellation,
        };
        return $"global::Refit.GeneratedRequestRunner.SendAsync<{request.ResultType}, {request.DeserializedResultType}>(\n{argumentIndent}{string.Join($",\n{argumentIndent}", arguments)})";
    }

    /// <summary>Builds the argument list that calls the sibling request member.</summary>
    /// <param name="parameters">The method parameters.</param>
    /// <param name="paging">The paging model.</param>
    /// <param name="tokenLocal">The lambda parameter that replaces the token parameter, or <see langword="null"/> when there is none.</param>
    /// <returns>The comma-separated arguments.</returns>
    internal static string BuildPageRequestArguments(ImmutableEquatableArray<ParameterModel> parameters, in PagingModel paging, string? tokenLocal)
    {
        var arguments = new List<string>();
        foreach (var parameter in parameters)
        {
            arguments.Add(tokenLocal is not null && parameter.MetadataName == paging.TokenParameter ? tokenLocal : $"@{parameter.MetadataName}");
        }

        return string.Join(ArgumentSeparator, arguments);
    }

    /// <summary>Builds the lambda that reads the continuation from a page.</summary>
    /// <param name="paging">The paging model.</param>
    /// <param name="names">The lambda parameter and local names.</param>
    /// <returns>The lambda, whose result is a <c>PageContinuation</c> for a token and a <c>Uri</c> for a link.</returns>
    internal static string BuildPageContinuationExpression(in PagingModel paging, in PagedNames names) =>
        paging.Kind == PagingKind.Link
            ? $"{names.LambdaModifier}{names.Page} => {BuildLinkExpression(paging, names.Page)}"
            : BuildTokenContinuationExpression(paging, names);

    /// <summary>Builds the lambda that reads the continuation of a token-based method from a page.</summary>
    /// <param name="paging">The paging model of a method that carries a token.</param>
    /// <param name="names">The lambda parameter and local names.</param>
    /// <returns>The lambda, whose result is a <c>PageContinuation</c>.</returns>
    internal static string BuildTokenContinuationExpression(in PagingModel paging, in PagedNames names)
    {
        var page = names.Page;
        return paging.NextSource switch
        {
            PagingNextSource.Total =>
                $"{names.LambdaModifier}({page}, {names.Token}) => {GeneratedPagingHelper}NextOffset<{paging.ItemType}>({names.Token}, {page}{paging.ItemsAccess}, {page}{paging.NextAccess})",
            PagingNextSource.Header =>
                $"{names.LambdaModifier}({page}, _) => {PageContinuationTo}{paging.TokenType}>({GeneratedPagingHelper}HeaderValue({page}, {ToCSharpStringLiteral(paging.NextAccess)}))",
            PagingNextSource.NullableProperty =>
                $"{names.LambdaModifier}({page}, _) => {GeneratedPagingHelper}NextValue({page}{paging.NextAccess})",
            _ =>
                $"{names.LambdaModifier}({page}, _) => {PageContinuationTo}{paging.TokenType}>({page}{paging.NextAccess})"
        };
    }

    /// <summary>Builds the expression that reads the link to the next page from a page.</summary>
    /// <param name="paging">The paging model of a method that follows links.</param>
    /// <param name="page">The page expression.</param>
    /// <returns>An expression of type <c>Uri</c> that is <see langword="null"/> when no page follows.</returns>
    internal static string BuildLinkExpression(in PagingModel paging, string page)
    {
        if (paging.NextSource == PagingNextSource.Header)
        {
            return $"{GeneratedPagingHelper}NextLink({page}, {ToCSharpStringLiteral(paging.NextAccess)})";
        }

        return paging.NextIsUri
            ? $"{page}{paging.NextAccess}"
            : $"{GeneratedPagingHelper}ToLink({page}{paging.NextAccess})";
    }

    /// <summary>Builds the expression that creates the policy deciding which links may be followed.</summary>
    /// <param name="paging">The paging model.</param>
    /// <returns>The policy expression.</returns>
    internal static string BuildOriginPolicyExpression(in PagingModel paging)
    {
        if (paging.Origin == PagingOrigin.SameAsClient)
        {
            return $"{GeneratedPagingHelper}SameOrigin(this.Client)";
        }

        if (paging.Origin == PagingOrigin.Any)
        {
            return "global::Refit.NextLinkOriginPolicy.Unrestricted";
        }

        var origins = new List<string>();
        foreach (var origin in paging.Origins)
        {
            origins.Add($"new global::System.Uri({ToCSharpStringLiteral(origin)}, global::System.UriKind.Absolute)");
        }

        return $"global::Refit.NextLinkOriginPolicy.Allow({string.Join(ArgumentSeparator, origins)})";
    }

    /// <summary>The names used inside a generated paged method.</summary>
    /// <param name="Settings">The settings local.</param>
    /// <param name="Token">The lambda parameter that carries the page's token or link.</param>
    /// <param name="Cancellation">The lambda parameter that carries the cancellation token.</param>
    /// <param name="Page">The lambda parameter that carries a fetched page.</param>
    /// <param name="Request">The local that holds a built request.</param>
    /// <param name="LambdaModifier">The modifier that makes a capture-free lambda static, or an empty string.</param>
    internal readonly record struct PagedNames(
        string Settings,
        string Token,
        string Cancellation,
        string Page,
        string Request,
        string LambdaModifier);
}
