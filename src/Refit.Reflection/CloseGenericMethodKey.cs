// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Reflection;

namespace Refit;

/// <summary>A cache key that identifies a closed generic method by its open definition and type arguments.</summary>
internal readonly struct CloseGenericMethodKey : IEquatable<CloseGenericMethodKey>
{
    /// <summary>Initializes a new instance of the <c>CloseGenericMethodKey</c> struct.</summary>
    /// <param name="openMethodInfo">The open generic method definition.</param>
    /// <param name="types">The type arguments used to close the method.</param>
    internal CloseGenericMethodKey(MethodInfo openMethodInfo, Type[] types)
    {
        OpenMethodInfo = openMethodInfo;
        Types = types;
    }

    /// <summary>Gets the open generic method definition.</summary>
    internal MethodInfo OpenMethodInfo { get; }

    /// <summary>Gets the type arguments used to close the method.</summary>
    internal Type[] Types { get; }

    /// <summary>Determines whether this key equals another key by open method definition and type arguments.</summary>
    /// <param name="other">The key to compare against.</param>
    /// <returns><see langword="true"/> if the keys are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(CloseGenericMethodKey other)
    {
        if (OpenMethodInfo != other.OpenMethodInfo || Types.Length != other.Types.Length)
        {
            return false;
        }

        for (var i = 0; i < Types.Length; i++)
        {
            if (Types[i] != other.Types[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is CloseGenericMethodKey closeGenericMethodKey && Equals(closeGenericMethodKey);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.Add(OpenMethodInfo);
        for (var i = 0; i < Types.Length; i++)
        {
            hashCode.Add(Types[i]);
        }

        return hashCode.ToHashCode();
    }
}
