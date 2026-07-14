// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Runtime.Serialization;

namespace Refit.GeneratedCode.TestModels.Scenarios
{
    /// <summary>The sort order exercised by the generated query scenario.</summary>
    public enum GeneratedUserSort
    {
        /// <summary>Sort by creation date, newest first.</summary>
        [EnumMember(Value = "date-desc")]
        DateDescending,

        /// <summary>Sort by user name.</summary>
        Name,
    }
}
