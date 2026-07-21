// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Provides the object contract for materializing pooled string-builder content.</summary>
internal sealed partial class PooledStringBuilder
{
    /// <summary>Materializes the accumulated content into a string and returns the pooled buffer.</summary>
    /// <returns>The accumulated string.</returns>
    public override string ToString()
    {
        var result = _pos == 0 ? string.Empty : new string(_buffer, 0, _pos);
        var toReturn = _buffer;
        _buffer = [];
        _pos = 0;
        ReturnBuffer(toReturn);
        return result;
    }
}
