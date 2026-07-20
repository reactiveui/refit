// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit;

/// <summary>Wraps an <see cref="ICustomAttributeProvider"/> and materializes its <see cref="QueryAttribute"/> lookup once
/// so the URL parameter formatter, which re-reads the attribute for every formatted value, never re-materializes it from
/// metadata.</summary>
/// <param name="inner">The underlying provider (a parameter, property or type) to read attributes from.</param>
/// <remarks>Only the <c>(typeof(QueryAttribute), inherit: true)</c> lookup the default formatter performs is cached; every
/// other query delegates to <paramref name="inner"/>, so a custom formatter that reads other attributes sees exactly the
/// same values it would from the original provider. The cached array holds immutable metadata attributes, so sharing it
/// across format calls is behaviourally identical to re-reading it.</remarks>
internal sealed class CachedAttributeProvider(ICustomAttributeProvider inner) : ICustomAttributeProvider
{
    /// <summary>The materialized <see cref="QueryAttribute"/> lookup, cached on first read.</summary>
    private object[]? _queryAttributes;

    /// <inheritdoc/>
    public object[] GetCustomAttributes(bool inherit) => inner.GetCustomAttributes(inherit);

    /// <inheritdoc/>
    public object[] GetCustomAttributes(Type attributeType, bool inherit) =>
        inherit && attributeType == typeof(QueryAttribute)
            ? _queryAttributes ??= inner.GetCustomAttributes(typeof(QueryAttribute), true)
            : inner.GetCustomAttributes(attributeType, inherit);

    /// <inheritdoc/>
    public bool IsDefined(Type attributeType, bool inherit) => inner.IsDefined(attributeType, inherit);
}
