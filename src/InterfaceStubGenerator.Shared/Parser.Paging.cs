// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>Reads the <c>[Paged]</c> and <c>[PageToken]</c> attributes of a method that returns <c>PagedEnumerable</c>.</content>
internal static partial class Parser
{
    /// <summary>The metadata name of <c>Refit.PagedAttribute</c>.</summary>
    private const string PagedAttributeDisplayName = "PagedAttribute";

    /// <summary>The metadata name of <c>Refit.PageTokenAttribute</c>.</summary>
    private const string PageTokenAttributeDisplayName = "PageTokenAttribute";

    /// <summary>Reads how a method pages when it returns <c>PagedEnumerable</c>; any other method has no paging.</summary>
    /// <param name="methodSymbol">The Refit method symbol.</param>
    /// <param name="returnTypeInfo">The classified return type shape.</param>
    /// <param name="context">The shared generation context, which collects the diagnostic.</param>
    /// <param name="isValid"><see langword="false"/> when the method returns <c>PagedEnumerable</c> and is misconfigured.</param>
    /// <returns>The paging model, or <see langword="null"/> for a method that does not page or is misconfigured.</returns>
    internal static PagingModel? ParsePagingForReturn(
        IMethodSymbol methodSymbol,
        ReturnTypeInfo returnTypeInfo,
        in InterfaceGenerationContext context,
        out bool isValid)
    {
        if (returnTypeInfo != ReturnTypeInfo.Paged)
        {
            if (FindMethodRefitAttribute(methodSymbol, PagedAttributeDisplayName) is not null)
            {
                context.Diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidPagedMethod,
                    methodSymbol.Locations[0],
                    methodSymbol.Name,
                    "[Paged] applies only to a method that returns PagedEnumerable<TPage, TItem>."));
            }

            isValid = true;
            return null;
        }

        var paging = ParsePaging(methodSymbol, context);
        isValid = paging is not null;
        return paging;
    }

    /// <summary>Reads how a method that returns <c>PagedEnumerable</c> pages, reporting why it cannot when the attributes are invalid.</summary>
    /// <param name="methodSymbol">The method whose return type is <c>PagedEnumerable</c>.</param>
    /// <param name="context">The shared generation context, which collects the diagnostic.</param>
    /// <returns>The paging model, or <see langword="null"/> when the method is misconfigured.</returns>
    internal static PagingModel? ParsePaging(IMethodSymbol methodSymbol, in InterfaceGenerationContext context)
    {
        var error = TryParsePaging(methodSymbol, context, out var paging);
        if (error is null)
        {
            return paging;
        }

        context.Diagnostics.Add(Diagnostic.Create(
            DiagnosticDescriptors.InvalidPagedMethod,
            methodSymbol.Locations[0],
            methodSymbol.Name,
            error));
        return null;
    }

    /// <summary>Resolves the paging model of a method.</summary>
    /// <param name="methodSymbol">The method whose return type is <c>PagedEnumerable</c>.</param>
    /// <param name="context">The shared generation context, used to qualify types.</param>
    /// <param name="paging">The paging model when the method is valid.</param>
    /// <returns>The reason the method is invalid, or <see langword="null"/> when it is valid.</returns>
    internal static string? TryParsePaging(IMethodSymbol methodSymbol, in InterfaceGenerationContext context, out PagingModel paging)
    {
        paging = default;
        var attribute = FindMethodRefitAttribute(methodSymbol, PagedAttributeDisplayName);
        if (attribute is null)
        {
            return "add a [Paged] attribute that names how each page is read.";
        }

        var settings = ReadPagedSettings(attribute);
        var returnType = (INamedTypeSymbol)methodSymbol.ReturnType;
        var pageType = returnType.TypeArguments[0];
        var itemType = returnType.TypeArguments[1];
        string? itemsAccess = null;
        var error = ValidatePagedSignature(methodSymbol, out var tokenParameter);
        error ??= ResolveItems(pageType, itemType, settings.Items, out itemsAccess);

        var continuation = default(PagingContinuation);
        error ??= tokenParameter is null
            ? ResolveLinkContinuation(pageType, settings, out continuation)
            : ResolveTokenContinuation(pageType, tokenParameter, settings, out continuation);

        if (error is not null)
        {
            return error;
        }

        paging = new(
            tokenParameter is null ? PagingKind.Link : PagingKind.Token,
            QualifyType(pageType, context),
            QualifyType(itemType, context),
            tokenParameter is null ? string.Empty : QualifyType(tokenParameter.Type, context),
            tokenParameter?.MetadataName ?? string.Empty,
            itemsAccess!,
            continuation.Source,
            continuation.Access,
            continuation.IsUri,
            continuation.Origin,
            continuation.Origins);
        return null;
    }

    /// <summary>Finds an attribute on a method by its Refit metadata name.</summary>
    /// <param name="methodSymbol">The method to inspect.</param>
    /// <param name="attributeMetadataName">The attribute's metadata name inside the <c>Refit</c> namespace.</param>
    /// <returns>The attribute data, or null when absent.</returns>
    internal static AttributeData? FindMethodRefitAttribute(IMethodSymbol methodSymbol, string attributeMetadataName)
    {
        foreach (var attribute in methodSymbol.GetAttributes())
        {
            if (IsRefitAttribute(attribute.AttributeClass, attributeMetadataName))
            {
                return attribute;
            }
        }

        return null;
    }

    /// <summary>Reads the named arguments of a <c>[Paged]</c> attribute.</summary>
    /// <param name="attribute">The attribute data.</param>
    /// <returns>The settings, with blank names treated as absent.</returns>
    internal static PagedSettings ReadPagedSettings(AttributeData attribute) =>
        new(
            ReadName(FindNamedArgument(attribute, "Items")),
            ReadName(FindNamedArgument(attribute, "Next")),
            ReadName(FindNamedArgument(attribute, "NextHeader")),
            ReadName(FindNamedArgument(attribute, "Total")),
            ReadOrigins(FindNamedArgument(attribute, "Origins")),
            FindNamedArgument(attribute, "SameOrigin").Value is true,
            FindNamedArgument(attribute, "AnyOrigin").Value is true);

    /// <summary>Finds a named argument of an attribute.</summary>
    /// <param name="attribute">The attribute data.</param>
    /// <param name="name">The property the argument sets.</param>
    /// <returns>The argument, or a default constant when the attribute does not set the property.</returns>
    internal static TypedConstant FindNamedArgument(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name)
            {
                return argument.Value;
            }
        }

        return default;
    }

    /// <summary>Reads a string attribute argument, treating a blank value as absent.</summary>
    /// <param name="value">The attribute argument.</param>
    /// <returns>The trimmed text, or <see langword="null"/>.</returns>
    internal static string? ReadName(TypedConstant value) =>
        value.Value is string text && !string.IsNullOrWhiteSpace(text) ? text.Trim() : null;

    /// <summary>Reads the origins array of a <c>[Paged]</c> attribute.</summary>
    /// <param name="value">The attribute argument.</param>
    /// <returns>The non-blank origins, or <see langword="null"/> when the attribute does not set them.</returns>
    internal static string[]? ReadOrigins(TypedConstant value)
    {
        if (value.Kind != TypedConstantKind.Array)
        {
            return null;
        }

        var origins = new List<string>();
        foreach (var element in value.Values)
        {
            if (ReadName(element) is { } origin)
            {
                origins.Add(origin);
            }
        }

        return [.. origins];
    }

    /// <summary>Checks the parts of a paged method's signature that are independent of the page type.</summary>
    /// <param name="methodSymbol">The method to check.</param>
    /// <param name="tokenParameter">The parameter marked <c>[PageToken]</c>, or <see langword="null"/> when there is none.</param>
    /// <returns>The reason the signature is invalid, or <see langword="null"/>.</returns>
    internal static string? ValidatePagedSignature(IMethodSymbol methodSymbol, out IParameterSymbol? tokenParameter)
    {
        tokenParameter = null;
        if (!methodSymbol.TypeParameters.IsEmpty)
        {
            return "the method cannot be generic.";
        }

        foreach (var parameter in methodSymbol.Parameters)
        {
            if (parameter.Type is INamedTypeSymbol { Name: "CancellationToken" } cancellation
                && IsInNamespace(cancellation, "System.Threading"))
            {
                return "the method cannot declare a CancellationToken, because the token passed to the enumeration is used for every page.";
            }

            if (!HasParameterAttribute(parameter, PageTokenAttributeDisplayName))
            {
                continue;
            }

            if (tokenParameter is not null)
            {
                return "only one parameter can be marked [PageToken].";
            }

            tokenParameter = parameter;
        }

        return null;
    }

    /// <summary>Resolves the member of the page that holds the items.</summary>
    /// <param name="pageType">The page type.</param>
    /// <param name="itemType">The item type.</param>
    /// <param name="itemsName">The member path named by <c>Items</c>, or <see langword="null"/> to infer it.</param>
    /// <param name="access">The member access that reads the items from a page expression.</param>
    /// <returns>The reason the items cannot be resolved, or <see langword="null"/>.</returns>
    internal static string? ResolveItems(ITypeSymbol pageType, ITypeSymbol itemType, string? itemsName, out string? access)
    {
        if (itemsName is null)
        {
            return InferItems(pageType, itemType, out access);
        }

        var error = TryResolveMemberPath(pageType, itemsName, out var resolved, out var itemsType);
        access = resolved;
        if (error is not null)
        {
            return error;
        }

        return IsSequenceOf(itemsType, itemType)
            ? null
            : $"'{itemsName}' is not a sequence of '{itemType.ToDisplayString()}'.";
    }

    /// <summary>Finds the one member of the page that is a sequence of the item type.</summary>
    /// <param name="pageType">The page type; an <c>ApiResponse&lt;T&gt;</c> is searched through its content.</param>
    /// <param name="itemType">The item type.</param>
    /// <param name="access">The member access that reads the items from a page expression.</param>
    /// <returns>The reason no single member was found, or <see langword="null"/>.</returns>
    internal static string? InferItems(ITypeSymbol pageType, ITypeSymbol itemType, out string? access)
    {
        access = null;
        var holder = pageType;
        var prefix = string.Empty;
        if (pageType is INamedTypeSymbol { TypeArguments.Length: 1 } response && IsApiResponseType(response))
        {
            holder = response.TypeArguments[0];
            prefix = ".@Content";

            // A response whose content is itself the list of items, such as a bare JSON array, is read through Content.
            if (IsSequenceOf(holder, itemType))
            {
                access = prefix;
                return null;
            }
        }

        ISymbol? found = null;
        foreach (var member in EnumerateReadableMembers(holder))
        {
            if (!IsSequenceOf(GetMemberType(member), itemType))
            {
                continue;
            }

            if (found is not null)
            {
                return $"'{holder.ToDisplayString()}' has more than one member that is a sequence of '{itemType.ToDisplayString()}'; name the one to read with Items.";
            }

            found = member;
        }

        if (found is null)
        {
            return $"'{holder.ToDisplayString()}' has no member that is a sequence of '{itemType.ToDisplayString()}'; name the member with Items.";
        }

        access = $"{prefix}{(prefix.Length > 0 ? "?." : ".")}@{found.Name}";
        return null;
    }

    /// <summary>Resolves a dotted member path against a type.</summary>
    /// <param name="root">The type the path starts from.</param>
    /// <param name="path">The dotted path, such as <c>Content.Documents</c>.</param>
    /// <param name="access">The member access that reads the path from an expression of the root type.</param>
    /// <param name="memberType">The type of the last member.</param>
    /// <returns>The reason the path cannot be resolved, or <see langword="null"/>.</returns>
    internal static string? TryResolveMemberPath(ITypeSymbol root, string path, out string access, out ITypeSymbol memberType)
    {
        var builder = new StringBuilder();
        var current = root;
        var isFirst = true;
        foreach (var segment in path.Split('.'))
        {
            var name = segment.Trim();
            var member = FindReadableMember(current, name);
            if (member is null)
            {
                access = string.Empty;
                memberType = root;
                return $"'{name}' is not a readable public or internal property or field of '{current.ToDisplayString()}'.";
            }

            // Members reached through a reference or a nullable value are read with ?. so an absent parent yields null.
            _ = builder.Append(!isFirst && CanBeNull(current) ? "?." : ".").Append('@').Append(member.Name);
            current = GetMemberType(member);
            isFirst = false;
        }

        access = builder.ToString();
        memberType = current;
        return null;
    }

    /// <summary>Resolves the continuation of a method whose <c>[PageToken]</c> parameter carries it.</summary>
    /// <param name="pageType">The page type.</param>
    /// <param name="tokenParameter">The parameter marked <c>[PageToken]</c>.</param>
    /// <param name="settings">The <c>[Paged]</c> settings.</param>
    /// <param name="continuation">The resolved continuation.</param>
    /// <returns>The reason the continuation cannot be resolved, or <see langword="null"/>.</returns>
    internal static string? ResolveTokenContinuation(
        ITypeSymbol pageType,
        IParameterSymbol tokenParameter,
        in PagedSettings settings,
        out PagingContinuation continuation)
    {
        continuation = default;
        if (settings.HasOriginOptions)
        {
            return "Origins, SameOrigin and AnyOrigin apply only to a method that follows links, which has no [PageToken] parameter.";
        }

        if (settings.SourceCount != 1)
        {
            return "name exactly one of Next, NextHeader or Total.";
        }

        if (settings.NextHeader is { } header)
        {
            continuation = new(PagingNextSource.Header, header, false, PagingOrigin.None, ImmutableEquatableArray<string>.Empty);
            return RequireResponsePage(pageType)
                   ?? (tokenParameter.Type.SpecialType == SpecialType.System_String
                       ? null
                       : "a [PageToken] parameter that reads a header must be a string.");
        }

        return settings.Total is { } total
            ? ResolveTotal(pageType, tokenParameter, total, out continuation)
            : ResolveTokenProperty(pageType, tokenParameter, settings.Next!, out continuation);
    }

    /// <summary>Resolves an offset method whose next offset is computed from the total item count.</summary>
    /// <param name="pageType">The page type.</param>
    /// <param name="tokenParameter">The parameter marked <c>[PageToken]</c>.</param>
    /// <param name="total">The member path of the total.</param>
    /// <param name="continuation">The resolved continuation.</param>
    /// <returns>The reason the total cannot be resolved, or <see langword="null"/>.</returns>
    internal static string? ResolveTotal(
        ITypeSymbol pageType,
        IParameterSymbol tokenParameter,
        string total,
        out PagingContinuation continuation)
    {
        continuation = default;
        if (tokenParameter.Type.SpecialType != SpecialType.System_Int32)
        {
            return "a [PageToken] parameter used with Total must be an int offset.";
        }

        var error = TryResolveMemberPath(pageType, total, out var access, out var totalType);
        if (error is not null)
        {
            return error;
        }

        if (UnwrapNullable(totalType).SpecialType is not (SpecialType.System_Int32 or SpecialType.System_Int64))
        {
            return $"'{total}' must be an int or a long.";
        }

        continuation = new(PagingNextSource.Total, access, false, PagingOrigin.None, ImmutableEquatableArray<string>.Empty);
        return null;
    }

    /// <summary>Resolves a token method whose continuation is a member of the page.</summary>
    /// <param name="pageType">The page type.</param>
    /// <param name="tokenParameter">The parameter marked <c>[PageToken]</c>.</param>
    /// <param name="next">The member path of the continuation.</param>
    /// <param name="continuation">The resolved continuation.</param>
    /// <returns>The reason the member cannot be used, or <see langword="null"/>.</returns>
    internal static string? ResolveTokenProperty(
        ITypeSymbol pageType,
        IParameterSymbol tokenParameter,
        string next,
        out PagingContinuation continuation)
    {
        continuation = default;
        var error = TryResolveMemberPath(pageType, next, out var access, out var nextType);
        if (error is not null)
        {
            return error;
        }

        if (!CanBeNull(nextType))
        {
            return $"'{next}' must be nullable so that the last page can end the sequence.";
        }

        if (!SymbolEqualityComparer.Default.Equals(UnwrapNullable(nextType), UnwrapNullable(tokenParameter.Type)))
        {
            return $"'{next}' is a '{nextType.ToDisplayString()}', but the [PageToken] parameter is a '{tokenParameter.Type.ToDisplayString()}'.";
        }

        var isNullableValue = nextType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                              && tokenParameter.Type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T;
        var source = isNullableValue ? PagingNextSource.NullableProperty : PagingNextSource.Property;
        continuation = new(source, access, false, PagingOrigin.None, ImmutableEquatableArray<string>.Empty);
        return null;
    }

    /// <summary>Resolves the continuation and origin policy of a method that follows links.</summary>
    /// <param name="pageType">The page type.</param>
    /// <param name="settings">The <c>[Paged]</c> settings.</param>
    /// <param name="continuation">The resolved continuation.</param>
    /// <returns>The reason the link cannot be resolved, or <see langword="null"/>.</returns>
    internal static string? ResolveLinkContinuation(ITypeSymbol pageType, in PagedSettings settings, out PagingContinuation continuation)
    {
        continuation = default;
        if (settings.Total is not null || settings.SourceCount != 1)
        {
            return "a method that follows links names exactly one of Next or NextHeader.";
        }

        var originError = ResolveOrigin(settings, out var origin, out var origins);
        if (originError is not null)
        {
            return originError;
        }

        if (settings.NextHeader is { } header)
        {
            continuation = new(PagingNextSource.Header, header, false, origin, origins);
            return RequireResponsePage(pageType);
        }

        var error = TryResolveMemberPath(pageType, settings.Next!, out var access, out var nextType);
        if (error is not null)
        {
            return error;
        }

        var target = UnwrapNullable(nextType);
        var isUri = target is INamedTypeSymbol { Name: "Uri" } uri && IsInNamespace(uri, SystemNamespace);
        if (!isUri && target.SpecialType != SpecialType.System_String)
        {
            return $"'{settings.Next}' must be a string or a Uri.";
        }

        continuation = new(PagingNextSource.Property, access, isUri, origin, origins);
        return null;
    }

    /// <summary>Resolves which origins a followed link may point at.</summary>
    /// <param name="settings">The <c>[Paged]</c> settings.</param>
    /// <param name="origin">The origin restriction.</param>
    /// <param name="origins">The listed origins, when the restriction is a list.</param>
    /// <returns>The reason the restriction is invalid, or <see langword="null"/>.</returns>
    internal static string? ResolveOrigin(in PagedSettings settings, out PagingOrigin origin, out ImmutableEquatableArray<string> origins)
    {
        origin = PagingOrigin.None;
        origins = ImmutableEquatableArray<string>.Empty;
        if (settings.OriginCount != 1)
        {
            return "a method that follows links states which origins a link may point at with exactly one of Origins, SameOrigin or AnyOrigin.";
        }

        if (settings.SameOrigin || settings.AnyOrigin)
        {
            origin = settings.SameOrigin ? PagingOrigin.SameAsClient : PagingOrigin.Any;
            return null;
        }

        var listed = settings.Origins!;
        var invalid = FindInvalidOrigin(listed);
        if (invalid is not null)
        {
            return $"'{invalid}' is not an absolute http or https origin.";
        }

        origin = PagingOrigin.Listed;
        origins = ImmutableEquatableArrayFactory.FromArray(listed);
        return null;
    }

    /// <summary>Finds the first origin that is not an absolute http or https URI.</summary>
    /// <param name="origins">The origins named by the attribute.</param>
    /// <returns>The invalid origin, or <see langword="null"/> when every origin is valid.</returns>
    internal static string? FindInvalidOrigin(string[] origins)
    {
        foreach (var origin in origins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                return origin;
            }
        }

        return null;
    }

    /// <summary>Requires that a header can be read from the page, which holds only for a response wrapper.</summary>
    /// <param name="pageType">The page type.</param>
    /// <returns>The reason the page has no headers, or <see langword="null"/>.</returns>
    internal static string? RequireResponsePage(ITypeSymbol pageType) =>
        IsApiResponseType(pageType)
            ? null
            : "reading a header needs a page type that is an ApiResponse<T> or an IApiResponse.";

    /// <summary>Determines whether a value of the type can be null, so a page can signal that no page follows.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> for a reference type or a <c>Nullable&lt;T&gt;</c>.</returns>
    internal static bool CanBeNull(ITypeSymbol type) =>
        type.IsReferenceType || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    /// <summary>Removes a <c>Nullable&lt;T&gt;</c> wrapper.</summary>
    /// <param name="type">The type to unwrap.</param>
    /// <returns>The underlying type of a nullable value type; otherwise the type itself.</returns>
    internal static ITypeSymbol UnwrapNullable(ITypeSymbol type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            ? nullable.TypeArguments[0]
            : type;

    /// <summary>Determines whether a type is an array or <c>IEnumerable&lt;T&gt;</c> of an item type.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="itemType">The item type.</param>
    /// <returns><see langword="true"/> when the type enumerates the item type.</returns>
    internal static bool IsSequenceOf(ITypeSymbol type, ITypeSymbol itemType)
    {
        if (type is IArrayTypeSymbol array)
        {
            return SymbolEqualityComparer.Default.Equals(array.ElementType, itemType);
        }

        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        if (IsEnumerableOf(type, itemType))
        {
            return true;
        }

        foreach (var implemented in type.AllInterfaces)
        {
            if (IsEnumerableOf(implemented, itemType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a type is exactly <c>IEnumerable&lt;T&gt;</c> of an item type.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="itemType">The item type.</param>
    /// <returns><see langword="true"/> when the type is <c>IEnumerable&lt;itemType&gt;</c>.</returns>
    internal static bool IsEnumerableOf(ITypeSymbol type, ITypeSymbol itemType) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Collections_Generic_IEnumerable_T } enumerable
        && SymbolEqualityComparer.Default.Equals(enumerable.TypeArguments[0], itemType);

    /// <summary>Finds an instance property or field a generated client can read.</summary>
    /// <param name="type">The type to search, including its base types and interfaces.</param>
    /// <param name="name">The member name.</param>
    /// <returns>The member, or <see langword="null"/> when none is readable.</returns>
    internal static ISymbol? FindReadableMember(ITypeSymbol type, string name)
    {
        foreach (var member in EnumerateReadableMembers(type))
        {
            if (member.Name == name)
            {
                return member;
            }
        }

        return null;
    }

    /// <summary>Enumerates the instance properties and fields a generated client can read, most derived first.</summary>
    /// <param name="type">The type to search, including its base types and interfaces.</param>
    /// <returns>The readable members.</returns>
    internal static IEnumerable<ISymbol> EnumerateReadableMembers(ITypeSymbol type)
    {
        var names = new HashSet<string>();
        foreach (var candidate in EnumerateTypeHierarchy(type))
        {
            foreach (var member in candidate.GetMembers())
            {
                if (IsReadableMember(member) && names.Add(member.Name))
                {
                    yield return member;
                }
            }
        }
    }

    /// <summary>Enumerates a type, its base types, and its interfaces.</summary>
    /// <param name="type">The type to walk.</param>
    /// <returns>The type followed by everything it derives from or implements.</returns>
    internal static IEnumerable<ITypeSymbol> EnumerateTypeHierarchy(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            yield return current;
        }

        foreach (var implemented in type.AllInterfaces)
        {
            yield return implemented;
        }
    }

    /// <summary>Determines whether a member is an instance property or field a generated client can read.</summary>
    /// <param name="member">The member to inspect.</param>
    /// <returns><see langword="true"/> for a readable, non-static, non-indexer property or field that is public or internal.</returns>
    internal static bool IsReadableMember(ISymbol member) =>
        !member.IsStatic
        && member.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal
        && member switch
        {
            IPropertySymbol property => !property.IsIndexer && property.GetMethod is not null,
            IFieldSymbol field => !field.IsImplicitlyDeclared,
            _ => false
        };

    /// <summary>Gets the type of a property or field.</summary>
    /// <param name="member">A member accepted by <see cref="IsReadableMember"/>.</param>
    /// <returns>The member's type.</returns>
    internal static ITypeSymbol GetMemberType(ISymbol member) =>
        member is IPropertySymbol property ? property.Type : ((IFieldSymbol)member).Type;

    /// <summary>The named arguments of a <c>[Paged]</c> attribute.</summary>
    /// <param name="Items">The member path that holds the items, or <see langword="null"/>.</param>
    /// <param name="Next">The member path that holds the continuation, or <see langword="null"/>.</param>
    /// <param name="NextHeader">The response header that holds the continuation, or <see langword="null"/>.</param>
    /// <param name="Total">The member path that holds the total item count, or <see langword="null"/>.</param>
    /// <param name="Origins">The listed origins; <see langword="null"/> when none were named.</param>
    /// <param name="SameOrigin">Whether links are restricted to the client's origin.</param>
    /// <param name="AnyOrigin">Whether links may point at any origin.</param>
    internal readonly record struct PagedSettings(
        string? Items,
        string? Next,
        string? NextHeader,
        string? Total,
        string[]? Origins,
        bool SameOrigin,
        bool AnyOrigin)
    {
        /// <summary>Gets the number of continuation sources named.</summary>
        internal int SourceCount => (Next is null ? 0 : 1) + (NextHeader is null ? 0 : 1) + (Total is null ? 0 : 1);

        /// <summary>Gets the number of link origin options set.</summary>
        internal int OriginCount => (Origins is { Length: > 0 } ? 1 : 0) + (SameOrigin ? 1 : 0) + (AnyOrigin ? 1 : 0);

        /// <summary>Gets a value indicating whether any link origin option is set.</summary>
        internal bool HasOriginOptions => OriginCount > 0;
    }

    /// <summary>How a paged method reads the continuation from a page.</summary>
    /// <param name="Source">Where the continuation is read from.</param>
    /// <param name="Access">The member access, header name or total member access.</param>
    /// <param name="IsUri">Whether a link property holds a <c>Uri</c> rather than text.</param>
    /// <param name="Origin">Which origins a followed link may point at.</param>
    /// <param name="Origins">The listed origins.</param>
    internal readonly record struct PagingContinuation(
        PagingNextSource Source,
        string Access,
        bool IsUri,
        PagingOrigin Origin,
        ImmutableEquatableArray<string> Origins);
}
