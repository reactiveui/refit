// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics;

namespace Refit.GeneratedCode.TestModels.Scenarios
{
    /// <summary>A sealed log entry used to exercise generated JSON Lines bodies under the repository analyzer configuration.</summary>
    [DebuggerDisplay("{Message}")]
    public sealed class GeneratedLogEntry
    {
        /// <summary>Gets or sets the log message.</summary>
        public string? Message { get; set; }
    }
}
