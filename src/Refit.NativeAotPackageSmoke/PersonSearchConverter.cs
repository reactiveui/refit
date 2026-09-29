// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.NativeAotPackageSmoke;

/// <summary>Writes a <see cref="PersonSearch"/> as the <c>q</c> and <c>limit</c> query parameters.</summary>
internal sealed class PersonSearchConverter : IQueryConverter<PersonSearch>
{
    /// <inheritdoc/>
    public void Flatten(PersonSearch value, string keyPrefix, ref GeneratedQueryStringBuilder builder, RefitSettings settings)
    {
        builder.Add($"{keyPrefix}q", value.Text, false);
        builder.Add($"{keyPrefix}limit", GeneratedRequestRunner.FormatInvariant(value.Limit, null), false);
    }
}
