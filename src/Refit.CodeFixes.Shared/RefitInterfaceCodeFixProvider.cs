// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Refit.Analyzers;

namespace Refit.CodeFixes;

/// <summary>Provides safe code fixes for Refit interface analyzer diagnostics.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RefitInterfaceCodeFixProvider))]
[Shared]
public sealed class RefitInterfaceCodeFixProvider : CodeFixProvider
{
    /// <summary>The title for the route slash code fix.</summary>
    private const string UseForwardSlashesTitle = "Use forward slashes in Refit route";

    /// <summary>The title for the header collection code fix.</summary>
    private const string UseHeaderCollectionTypeTitle =
        "Use IDictionary<string, string> for HeaderCollection";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
#if ROSLYN_5
    [
        DiagnosticIds.InvalidRouteBackslash,
        DiagnosticIds.InvalidHeaderCollectionParameter
    ];
#else
        CreateFixableDiagnosticIds();
#endif

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case DiagnosticIds.InvalidRouteBackslash:
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            UseForwardSlashesTitle,
                            cancellationToken => UseForwardSlashesAsync(
                                context.Document,
                                diagnostic,
                                cancellationToken),
                            UseForwardSlashesTitle),
                        diagnostic);
                    break;
                }

                case DiagnosticIds.InvalidHeaderCollectionParameter:
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            UseHeaderCollectionTypeTitle,
                            cancellationToken => UseHeaderCollectionTypeAsync(
                                context.Document,
                                diagnostic,
                                cancellationToken),
                            UseHeaderCollectionTypeTitle),
                        diagnostic);
                    break;
                }
            }
        }

        return Task.CompletedTask;
    }

#if !ROSLYN_5
    /// <summary>Creates the immutable fixable ID set without using Roslyn 5-only collection expressions.</summary>
    /// <returns>The fixable diagnostic IDs.</returns>
    private static ImmutableArray<string> CreateFixableDiagnosticIds()
    {
        var builder = ImmutableArray.CreateBuilder<string>(2);
        builder.Add(DiagnosticIds.InvalidRouteBackslash);
        builder.Add(DiagnosticIds.InvalidHeaderCollectionParameter);
        return builder.MoveToImmutable();
    }
#endif

    /// <summary>Replaces backslashes in a Refit route literal with forward slashes.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="diagnostic">The diagnostic to fix.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated document.</returns>
    private static async Task<Document> UseForwardSlashesAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var attribute = root
            .FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
            .FirstAncestorOrSelf<AttributeSyntax>();
        var literal = attribute?
            .DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .FirstOrDefault(x => x.IsKind(SyntaxKind.StringLiteralExpression));
        if (literal is null || literal.Token.Value is not string route || route.IndexOf('\\') < 0)
        {
            return document;
        }

        var updatedLiteral = SyntaxFactory
            .LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(route.Replace('\\', '/')))
            .WithTriviaFrom(literal);
        return document.WithSyntaxRoot(root.ReplaceNode(literal, updatedLiteral));
    }

    /// <summary>Changes an invalid HeaderCollection parameter type to the supported dictionary type.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="diagnostic">The diagnostic to fix.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated document.</returns>
    private static async Task<Document> UseHeaderCollectionTypeAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var parameter = root
            .FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
            .FirstAncestorOrSelf<ParameterSyntax>();
        if (parameter?.Type is null)
        {
            return document;
        }

        var replacementType = SyntaxFactory.ParseTypeName(
            "global::System.Collections.Generic.IDictionary<string, string>");
        var updatedParameter = parameter.WithType(replacementType.WithTriviaFrom(parameter.Type));
        return document.WithSyntaxRoot(root.ReplaceNode(parameter, updatedParameter));
    }
}
