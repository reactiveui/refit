// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

// This should be made into a union come C# 15.
/// <summary>
/// Union data type representing part of the path.
/// </summary>
internal abstract record PathFragmentModel
{
    internal record Constant(string Value) : PathFragmentModel;
    internal record ObjectAccess(string AccessExpression, string ParameterType, string Property, string PropertyType) : PathFragmentModel;
    internal record StandardParameter(string ParameterType, string MetadataName, bool IsRoundTripping) : PathFragmentModel;
    internal record UnmatchedPathGuard(string RawName) : PathFragmentModel;
    internal record RoundTripNotStringError(string MetadataName, string ParamType) : PathFragmentModel;
}
