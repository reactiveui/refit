// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>The per-method source fragments assembled into an inline generated Refit method.</summary>
/// <param name="RequestPrologueSource">The request-prologue formatting locals and query-builder setup.</param>
/// <param name="HttpMethodFieldSource">The cached HTTP-method field declaration, if any.</param>
/// <param name="HttpMethodExpression">The expression yielding the request's HTTP method.</param>
/// <param name="RequestUriExpression">The expression yielding the request URI.</param>
/// <param name="FormFieldsSource">The cached form-field descriptor field declaration, if any.</param>
/// <param name="ContentSource">The request-content assignment source, if any.</param>
/// <param name="HeaderSource">The request-header assignment source, if any.</param>
/// <param name="RequestPropertySource">The request-property assignment source, if any.</param>
/// <param name="TimeoutSource">The per-call timeout assignment source, if any.</param>
/// <param name="Opening">The method signature and opening brace.</param>
internal readonly record struct InlineMethodFragments(
    string RequestPrologueSource,
    string HttpMethodFieldSource,
    string HttpMethodExpression,
    string RequestUriExpression,
    string FormFieldsSource,
    string ContentSource,
    string HeaderSource,
    string RequestPropertySource,
    string TimeoutSource,
    string Opening);
