// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Refit.Generator;

namespace Refit.Analyzers;

/// <summary>Turns the shared classifier's <see cref="InlineFallback"/> into the located, specific RF006 explanation.</summary>
/// <remarks>This only words and locates the reason the generator's own classification produced; it never re-decides
/// eligibility, so a shape the generator learns to build loses its RF006 without any change here. Texts carry no
/// trailing period because the RF006 message format supplies it.</remarks>
internal static class FallbackExplanation
{
    /// <summary>The advice for a limitation the reflection request builder can handle.</summary>
    internal const string ReflectionAdvice =
        "If reflection is acceptable, reference the Refit.Reflection package and create the client with RestService.For; "
        + "the method still fails under Native AOT and through generated-only registration (AddRefitGeneratedClient, RestService.ForGenerated)";

    /// <summary>The advice for a declaration the reflection request builder also rejects.</summary>
    internal const string InvalidDeclarationAdvice =
        "Refit.Reflection rejects this declaration at runtime too, so the declaration must change";

    /// <summary>The advice for a feature only generated request building supports.</summary>
    internal const string GeneratedOnlyAdvice =
        "Refit.Reflection does not support this feature, so the declaration must change";

    /// <summary>Describes why a method falls back and what to change.</summary>
    /// <param name="method">The Refit method.</param>
    /// <param name="fallback">The classifier's fallback for the method.</param>
    /// <returns>The explanation.</returns>
    internal static Explanation Describe(IMethodSymbol method, in InlineFallback fallback)
    {
        var parameter = GetParameter(method, fallback);
        var subject = new Subject(parameter?.Name ?? string.Empty, parameter is null ? string.Empty : Display(parameter.Type));
        return DescribeMethodShape(method, fallback.Reason)
            ?? DescribeInvalidBinding(fallback.Reason, subject)
            ?? DescribeUnsupportedValue(fallback.Reason, subject)
            ?? new(
                "its request cannot be generated inline",
                "Change the method to use only features supported by generated request building",
                ReflectionAdvice);
    }

    /// <summary>Finds the most precise source location for a method's fallback.</summary>
    /// <param name="method">The Refit method.</param>
    /// <param name="fallback">The classifier's fallback for the method.</param>
    /// <param name="httpMethodAttribute">The method's HTTP method attribute.</param>
    /// <returns>The parameter, return type or attribute declaration responsible, or the method's own location.</returns>
    internal static Location? Locate(IMethodSymbol method, in InlineFallback fallback, AttributeData? httpMethodAttribute)
    {
        if (GetParameter(method, fallback) is { } parameter)
        {
            return parameter.Locations.FirstOrDefault();
        }

        var located = fallback.Reason switch
        {
            InlineFallbackReason.UnsupportedReturnType => GetReturnTypeLocation(method),
            InlineFallbackReason.UnreadableHttpMethod or InlineFallbackReason.UnsupportedPathTemplate =>
                httpMethodAttribute?.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
            _ => null,
        };
        return located ?? method.Locations.FirstOrDefault();
    }

    /// <summary>Gets the parameter a fallback blames, when it blames one.</summary>
    /// <param name="method">The Refit method.</param>
    /// <param name="fallback">The classifier's fallback for the method.</param>
    /// <returns>The parameter, or <see langword="null"/> for a method-level fallback.</returns>
    internal static IParameterSymbol? GetParameter(IMethodSymbol method, in InlineFallback fallback) =>
        fallback.ParameterOrdinal >= 0 && fallback.ParameterOrdinal < method.Parameters.Length
            ? method.Parameters[fallback.ParameterOrdinal]
            : null;

    /// <summary>Describes a limitation of the method as a whole.</summary>
    /// <param name="method">The Refit method.</param>
    /// <param name="reason">The fallback reason.</param>
    /// <returns>The explanation, or <see langword="null"/> when the reason is not method-level.</returns>
    private static Explanation? DescribeMethodShape(IMethodSymbol method, InlineFallbackReason reason) =>
        reason switch
        {
            InlineFallbackReason.UnsupportedReturnType => new(
                $"its return type '{Display(method.ReturnType)}' is not a shape generated requests produce, and no IReturnTypeAdapter here surfaces it",
                "Return Task, Task<T>, ValueTask<T>, IObservable<T>, IAsyncEnumerable<T> or PagedEnumerable<TPage, TItem>, "
                + "or declare an IReturnTypeAdapter<TReturn, TResult> for this type in the project",
                ReflectionAdvice),
            InlineFallbackReason.UnreadableHttpMethod => new(
                "its custom HTTP method attribute does not return its verb as new HttpMethod(\"VERB\") with a string literal, "
                + "so the verb cannot be read at compile time",
                "Override Method as an expression that constructs HttpMethod from a string literal",
                ReflectionAdvice),
            InlineFallbackReason.UnsupportedPathTemplate => new(
                "its path template contains a backslash, a line break or unbalanced braces",
                "Use '/' separators and close every '{' placeholder within its segment",
                InvalidDeclarationAdvice),
            InlineFallbackReason.InvalidPagedMethod => new(
                "its [Paged] configuration is invalid",
                "Fix the RF013 error reported for this method",
                GeneratedOnlyAdvice),
            InlineFallbackReason.InvalidJsonTypeInfoParameter => new(
                "a JsonTypeInfo<T> parameter cannot be used",
                "Fix the RF014 error reported for this method",
                GeneratedOnlyAdvice),
            _ => null,
        };

    /// <summary>Describes an invalid combination of bindings, which the reflection builder rejects too.</summary>
    /// <param name="reason">The fallback reason.</param>
    /// <param name="subject">The responsible parameter's name and type.</param>
    /// <returns>The explanation, or <see langword="null"/> when the reason is not an invalid binding.</returns>
    private static Explanation? DescribeInvalidBinding(InlineFallbackReason reason, in Subject subject) =>
        reason switch
        {
            InlineFallbackReason.UrlParameterType => new(
                $"[Url] parameter '{subject.Name}' is of type '{subject.Type}'",
                "Declare the [Url] parameter as string or System.Uri",
                InvalidDeclarationAdvice),
            InlineFallbackReason.UrlParameterWithPath => new(
                $"[Url] parameter '{subject.Name}' is combined with a path template, a path parameter or another [Url] parameter",
                "Give the HTTP method attribute an empty path and keep a single [Url] parameter; it supplies the full absolute URI",
                InvalidDeclarationAdvice),
            InlineFallbackReason.MultipleBodies => new(
                $"parameter '{subject.Name}' is a second [Body] parameter",
                "Keep one [Body] parameter and send the other values as query, header or path values",
                InvalidDeclarationAdvice),
            InlineFallbackReason.SecondImplicitBody => new(
                $"parameter '{subject.Name}' of type '{subject.Type}' would be a second implicit request body",
                $"Mark the intended body with [Body], and mark '{subject.Name}' with [Query] or give it a [QueryConverter]",
                InvalidDeclarationAdvice),
            InlineFallbackReason.MultipleCancellationTokens => new(
                $"parameter '{subject.Name}' is a second CancellationToken",
                "Keep one CancellationToken parameter",
                InvalidDeclarationAdvice),
            InlineFallbackReason.MultipleHeaderCollections => new(
                $"parameter '{subject.Name}' is a second [HeaderCollection] parameter",
                "Keep one [HeaderCollection] parameter",
                InvalidDeclarationAdvice),
            InlineFallbackReason.MultipleAuthorizeParameters => new(
                $"parameter '{subject.Name}' is a second [Authorize] parameter",
                "Keep one [Authorize] parameter",
                InvalidDeclarationAdvice),
            InlineFallbackReason.MultipartWithBody => new(
                $"[Body] parameter '{subject.Name}' is declared on a [Multipart] method",
                "Remove [Body]; every parameter of a multipart method becomes a form part",
                InvalidDeclarationAdvice),
            _ => null,
        };

    /// <summary>Describes a parameter whose value the generator cannot render at compile time.</summary>
    /// <param name="reason">The fallback reason.</param>
    /// <param name="subject">The responsible parameter's name and type.</param>
    /// <returns>The explanation, or <see langword="null"/> when the reason is not a value limitation.</returns>
    private static Explanation? DescribeUnsupportedValue(InlineFallbackReason reason, in Subject subject) =>
        DescribeUnsupportedPathOrBody(reason, subject) ?? DescribeUnsupportedQueryOrPart(reason, subject);

    /// <summary>Describes a path or body value the generator cannot render at compile time.</summary>
    /// <param name="reason">The fallback reason.</param>
    /// <param name="subject">The responsible parameter's name and type.</param>
    /// <returns>The explanation, or <see langword="null"/> when the reason is not a path or body limitation.</returns>
    private static Explanation? DescribeUnsupportedPathOrBody(InlineFallbackReason reason, in Subject subject) =>
        reason switch
        {
            InlineFallbackReason.GenericUrlEncodedBody => new(
                $"form-url-encoded body parameter '{subject.Name}' of type '{subject.Type}' uses a method type parameter, whose properties are unknown",
                "Use a concrete body type, or send the body as JSON",
                ReflectionAdvice),
            InlineFallbackReason.UnknownBodySerialization => new(
                $"body parameter '{subject.Name}' names a BodySerializationMethod value the generator does not recognize",
                "Use a defined BodySerializationMethod member",
                ReflectionAdvice),
            InlineFallbackReason.UnsupportedPathParameterType => new(
                $"path parameter '{subject.Name}' has type '{subject.Type}', whose shape is not known at compile time",
                "Declare a simple type, a concrete class, struct or array, or constrain the type parameter to a class",
                ReflectionAdvice),
            InlineFallbackReason.EncodedRoundTripNotString => new(
                $"[Encoded] round-trip parameter '{subject.Name}' is of type '{subject.Type}'",
                "Declare it as string, or remove [Encoded] so each segment is escaped",
                ReflectionAdvice),
            InlineFallbackReason.UnresolvedPathProperty => new(
                $"a dotted path placeholder for parameter '{subject.Name}' does not name a readable property of a simple type on '{subject.Type}'",
                "Name a public readable property of a simple type in the placeholder, or constrain the type parameter to a class that declares it",
                ReflectionAdvice),
            InlineFallbackReason.UnsupportedPathObjectQuery => new(
                $"a property of path-bound parameter '{subject.Name}' ('{subject.Type}') that no placeholder uses cannot be flattened into the query string",
                "Bind that property to a placeholder, or change it to a simple value or a collection of simple values",
                ReflectionAdvice),
            _ => null,
        };

    /// <summary>Describes a query value or multipart part the generator cannot render at compile time.</summary>
    /// <param name="reason">The fallback reason.</param>
    /// <param name="subject">The responsible parameter's name and type.</param>
    /// <returns>The explanation, or <see langword="null"/> when the reason is not a query or part limitation.</returns>
    private static Explanation? DescribeUnsupportedQueryOrPart(InlineFallbackReason reason, in Subject subject) =>
        reason switch
        {
            InlineFallbackReason.UnsupportedQueryType => new(
                $"query parameter '{subject.Name}' has type '{subject.Type}', which cannot be flattened into query values at compile time",
                $"Add [QueryConverter(typeof(...))] naming an IQueryConverter<{subject.Type}>, use a concrete type with readable properties, or set TreatAsString",
                ReflectionAdvice),
            InlineFallbackReason.UnreadableQueryConverter => new(
                $"the [QueryConverter] on parameter '{subject.Name}' does not name a converter type",
                $"Pass typeof(...) of an IQueryConverter<{subject.Type}> implementation to [QueryConverter]",
                GeneratedOnlyAdvice),
            InlineFallbackReason.UnsupportedMultipartPart => new(
                $"multipart parameter '{subject.Name}' has type '{subject.Type}', which is not a part type the generator can dispatch at compile time",
                "Use StreamPart, ByteArrayPart, FileInfoPart, Stream, byte[], FileInfo, string, HttpContent or a concrete serializable type",
                ReflectionAdvice),
            InlineFallbackReason.FormObjectMultipartPart => new(
                $"multipart parameter '{subject.Name}' uses [FormObject], whose per-property flattening only the reflection builder performs",
                $"Declare each form field as its own parameter, or remove [FormObject] to send '{subject.Name}' as one serialized part",
                ReflectionAdvice),
            _ => null,
        };

    /// <summary>Gets the location of a method's declared return type.</summary>
    /// <param name="method">The method.</param>
    /// <returns>The return type's location, or <see langword="null"/> when the method has no source declaration.</returns>
    private static Location? GetReturnTypeLocation(IMethodSymbol method)
    {
        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is MethodDeclarationSyntax declaration)
            {
                return declaration.ReturnType.GetLocation();
            }
        }

        return null;
    }

    /// <summary>Formats a type the way a user wrote it.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The minimally qualified display string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string Display(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    /// <summary>The wording of one RF006 explanation.</summary>
    /// <param name="Problem">What stops generation, naming the parameter and type where one is responsible.</param>
    /// <param name="Remedy">What to change so the request is generated.</param>
    /// <param name="Compatibility">Whether and how <c>Refit.Reflection</c> can run the method instead.</param>
    internal readonly record struct Explanation(string Problem, string Remedy, string Compatibility);

    /// <summary>The parameter an explanation names.</summary>
    /// <param name="Name">The parameter name, or empty for a method-level reason.</param>
    /// <param name="Type">The parameter type as written, or empty for a method-level reason.</param>
    private readonly record struct Subject(string Name, string Type);
}
