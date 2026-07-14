// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NETFRAMEWORK
using System.Collections.Generic;

namespace System;

/// <summary>Polyfill for <c>System.HashCode</c> on .NET Framework targets.</summary>
/// <remarks>
/// This is intentionally small: it provides the compiler/runtime surface Refit uses without
/// attempting to clone the randomized framework implementation. It is a <c>ref struct</c>
/// because every use is a stack-local accumulator; that keeps it from ever being boxed or
/// compared, so it needs no equality members.
/// </remarks>
[Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal ref struct HashCode
{
    /// <summary>The second prime multiplier.</summary>
    private const int Prime2 = -2_048_144_777;

    /// <summary>The third prime multiplier.</summary>
    private const int Prime3 = -1_028_477_379;

    /// <summary>The fourth prime multiplier.</summary>
    private const int Prime4 = 668_265_263;

    /// <summary>The fifth prime multiplier.</summary>
    private const int Prime5 = 374_761_393;

    /// <summary>The number of bytes represented by each queued integer value.</summary>
    private const int BytesPerQueuedValue = 4;

    /// <summary>The first avalanche shift.</summary>
    private const int AvalancheShift1 = 15;

    /// <summary>The second avalanche shift.</summary>
    private const int AvalancheShift2 = 13;

    /// <summary>The third avalanche shift.</summary>
    private const int AvalancheShift3 = 16;

    /// <summary>The queued-value rotation offset.</summary>
    private const int QueueRoundRotation = 17;

    /// <summary>The number of bits in an integer.</summary>
    private const int BitsPerInt32 = 32;

    /// <summary>The running hash value.</summary>
    private int _hash;

    /// <summary>The number of values added to this accumulator.</summary>
    private int _count;

    /// <summary>Combines one value into a hash code.</summary>
    /// <typeparam name="T1">The first value type.</typeparam>
    /// <param name="value1">The first value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1>(T1 value1)
    {
        HashCode hashCode = default;
        hashCode.Add(value1);
        return hashCode.ToHashCode();
    }

    /// <summary>Combines two values into a hash code.</summary>
    /// <typeparam name="T1">The first value type.</typeparam>
    /// <typeparam name="T2">The second value type.</typeparam>
    /// <param name="value1">The first value.</param>
    /// <param name="value2">The second value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
        HashCode hashCode = default;
        hashCode.Add(value1);
        hashCode.Add(value2);
        return hashCode.ToHashCode();
    }

    /// <summary>Combines three values into a hash code.</summary>
    /// <typeparam name="T1">The first value type.</typeparam>
    /// <typeparam name="T2">The second value type.</typeparam>
    /// <typeparam name="T3">The third value type.</typeparam>
    /// <param name="value1">The first value.</param>
    /// <param name="value2">The second value.</param>
    /// <param name="value3">The third value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
    {
        HashCode hashCode = default;
        hashCode.Add(value1);
        hashCode.Add(value2);
        hashCode.Add(value3);
        return hashCode.ToHashCode();
    }

    /// <summary>Combines four values into a hash code.</summary>
    /// <typeparam name="T1">The first value type.</typeparam>
    /// <typeparam name="T2">The second value type.</typeparam>
    /// <typeparam name="T3">The third value type.</typeparam>
    /// <typeparam name="T4">The fourth value type.</typeparam>
    /// <param name="value1">The first value.</param>
    /// <param name="value2">The second value.</param>
    /// <param name="value3">The third value.</param>
    /// <param name="value4">The fourth value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
    {
        HashCode hashCode = default;
        hashCode.Add(value1);
        hashCode.Add(value2);
        hashCode.Add(value3);
        hashCode.Add(value4);
        return hashCode.ToHashCode();
    }

    /// <summary>Adds a value to the hash code.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value.</param>
    public void Add<T>(T value) => Add(value, EqualityComparer<T>.Default);

    /// <summary>Adds a value to the hash code using the specified comparer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="comparer">The equality comparer.</param>
    public void Add<T>(T value, IEqualityComparer<T>? comparer)
    {
        var hashCode = value is null ? 0 : (comparer?.GetHashCode(value) ?? value.GetHashCode());
        Add(hashCode);
    }

    /// <summary>Returns the final hash code.</summary>
    /// <returns>The final hash code.</returns>
    public readonly int ToHashCode()
    {
        var hash = _count == 0 ? Prime5 : _hash;
        hash += _count * BytesPerQueuedValue;
        hash ^= (int)((uint)hash >> AvalancheShift1);
        hash *= Prime2;
        hash ^= (int)((uint)hash >> AvalancheShift2);
        hash *= Prime3;
        hash ^= (int)((uint)hash >> AvalancheShift3);
        return hash;
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode() => ToHashCode();

    /// <summary>Mixes one queued value into the hash.</summary>
    /// <param name="hash">The current hash.</param>
    /// <param name="queuedValue">The queued value.</param>
    /// <returns>The mixed hash.</returns>
    private static int QueueRound(int hash, int queuedValue) =>
        RotateLeft(hash + (queuedValue * Prime3), QueueRoundRotation) * Prime4;

    /// <summary>Rotates a value left by the requested number of bits.</summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="offset">The bit offset.</param>
    /// <returns>The rotated value.</returns>
    private static int RotateLeft(int value, int offset) =>
        (int)(((uint)value << offset) | ((uint)value >> (BitsPerInt32 - offset)));

    /// <summary>Adds a raw hash code value.</summary>
    /// <param name="value">The raw hash code.</param>
    private void Add(int value)
    {
        _hash = QueueRound(_count == 0 ? Prime5 : _hash, value);
        _count++;
    }
}
#endif
