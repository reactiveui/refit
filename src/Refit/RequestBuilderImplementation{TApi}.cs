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
                DynamicallyAccessedMemberTypes.Interfaces
                | DynamicallyAccessedMemberTypes.PublicMethods
                | DynamicallyAccessedMemberTypes.NonPublicMethods)] TApi> : RequestBuilderImplementation, IRequestBuilder<TApi>
    {
        /// <summary>Initializes a new instance of the <see cref="RequestBuilderImplementation{TApi}"/> class.</summary>
        /// <param name="refitSettings">The settings to use, or null for defaults.</param>
        [RequiresUnreferencedCode("Building requests from reflected interface methods requires interface and request object metadata to be available at runtime.")]
        public RequestBuilderImplementation(RefitSettings? refitSettings = null)
            : base(typeof(TApi), refitSettings)
        {
        }
    }
}
