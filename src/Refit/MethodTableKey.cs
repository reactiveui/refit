// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit;

/// <summary>Represents a method composed of its name, generic arguments and parameters.</summary>
internal readonly struct MethodTableKey : IEquatable<MethodTableKey>
{
    /// <summary>Initializes a new instance of the <see cref="MethodTableKey"/> struct.</summary>
    /// <param name="methodName">Represents the methods name.</param>
    /// <param name="parameters">Array containing the methods parameters.</param>
    /// <param name="genericArguments">Array containing the methods generic arguments.</param>
    public MethodTableKey(string methodName, Type[] parameters, Type[] genericArguments)
    {
        MethodName = methodName;
        Parameters = parameters;
        GenericArguments = genericArguments;
    }

    /// <summary>Gets the methods name.</summary>
    private string MethodName { get; }

    /// <summary>Gets the Array containing the methods parameters.</summary>
    private Type[] Parameters { get; }

    /// <summary>Gets the Array containing the methods generic arguments.</summary>
    private Type[] GenericArguments { get; }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.Add(MethodName);

        for (var i = 0; i < Parameters.Length; i++)
        {
            hashCode.Add(Parameters[i]);
        }

        for (var i = 0; i < GenericArguments.Length; i++)
        {
            hashCode.Add(GenericArguments[i]);
        }

        return hashCode.ToHashCode();
    }

    /// <inheritdoc/>
    public bool Equals(MethodTableKey other)
    {
        if (Parameters.Length != other.Parameters.Length
            || GenericArguments.Length != other.GenericArguments.Length
            || MethodName != other.MethodName)
        {
            return false;
        }

        for (var i = 0; i < Parameters.Length; i++)
        {
            if (Parameters[i] != other.Parameters[i])
            {
                return false;
            }
        }

        for (var i = 0; i < GenericArguments.Length; i++)
        {
            if (GenericArguments[i] != other.GenericArguments[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is MethodTableKey other && Equals(other);
}
