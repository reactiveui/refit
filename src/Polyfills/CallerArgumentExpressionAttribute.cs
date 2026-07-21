// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET6_0_OR_GREATER
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

/// <summary>Polyfill for the caller-argument-expression attribute on older target frameworks.</summary>
/// <param name="parameterName">The parameter whose source expression should be captured.</param>
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class CallerArgumentExpressionAttribute(string parameterName)
    : Attribute, CallerArgumentExpressionAttribute.IMetadata
{
    /// <summary>Defines the compiler-required public metadata contract.</summary>
    internal interface IMetadata
    {
        /// <summary>Gets the parameter whose source expression should be captured.</summary>
        string ParameterName { get; }
    }

    /// <summary>Gets the parameter whose source expression should be captured.</summary>
    public string ParameterName { get; } = parameterName;
}
#endif
