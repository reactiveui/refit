// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>
/// Union data type representing dynamic route parameter value.
/// </summary>
internal record RouteFragmentModel
{
    internal record Constant(string Value) : RouteFragmentModel;
    internal record ObjectAccess(string AccessExpression, string ParameterType, string Property) : RouteFragmentModel;
    internal record StandardParameter(string MetadataName, bool IsRoundTripping) : RouteFragmentModel;
    internal record UnmatchedRouteGuard(string RawName) : RouteFragmentModel;
    internal record RoundTripNotStringError(string MetadataName, string ParamType) : RouteFragmentModel;
}
