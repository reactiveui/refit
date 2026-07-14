// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Describes a query parameter flattened by a user-supplied <c>IQueryConverter&lt;T&gt;</c>.</summary>
/// <param name="ConverterTypeName">The fully-qualified converter type, cached and invoked by the generated method.</param>
/// <param name="KeyPrefix">The compile-time key prefix from the parameter's <c>[Query(Prefix)]</c>, or the empty
/// string, passed to the converter to prepend to every key it writes.</param>
internal sealed record QueryConverterModel(
    string ConverterTypeName,
    string KeyPrefix);
