// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Refit.GeneratedCode.TestModels.Scenarios
{
    /// <summary>Exercises generated Refit code under the repository analyzer configuration.</summary>
    public interface IGeneratedUserApi
    {
        /// <summary>Gets a user.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user payload.</returns>
        [Get("/users")]
        public Task<ApiResponse<string>> GetUserAsync(
            CancellationToken cancellationToken);

        /// <summary>Creates a user.</summary>
        /// <param name="payload">The user payload.</param>
        /// <param name="headers">The request headers.</param>
        /// <returns>The created user payload.</returns>
        [Post("/users")]
        public Task<string> CreateUserAsync(
            [Body] string payload,
            [HeaderCollection] IDictionary<string, string> headers);
    }
}
