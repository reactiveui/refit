// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.LiveMultipart;

/// <summary>Contains models used by the generated multipart API.</summary>
public static partial class LiveMultipartApi
{
    /// <summary>Represents a non-sealed serialized multipart value.</summary>
    public class Report
    {
        /// <summary>Gets or sets the report title.</summary>
        public string? Title { get; set; }

        /// <summary>Gets or sets the report score.</summary>
        public int Score { get; set; }
    }

    /// <summary>Provides a derived report so the base fixture remains intentionally unsealed.</summary>
    public sealed class DerivedReport : Report;

    /// <summary>Represents a form-object multipart value.</summary>
    public sealed class Profile
    {
        /// <summary>Gets or sets the aliased profile name.</summary>
        [AliasAs("full_name")]
        public string? Name { get; set; }

        /// <summary>Gets or sets the profile age.</summary>
        public int Age { get; set; }
    }
}
