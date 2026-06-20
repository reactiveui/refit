// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit
{
    /// <summary>Typed request builder that targets a specific Refit interface.</summary>
    /// <typeparam name="TApi">The Refit interface type requests are built for.</typeparam>
    internal class RequestBuilderImplementation<
        [
            DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TApi> : RequestBuilderImplementation, IRequestBuilder<TApi>
    {
        /// <summary>Initializes a new instance of the <see cref="RequestBuilderImplementation{TApi}"/> class.</summary>
        /// <param name="refitSettings">The settings to use, or null for defaults.</param>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        public RequestBuilderImplementation(RefitSettings? refitSettings = null)
            : base(typeof(TApi), refitSettings)
        {
        }
    }
}
