// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Compression;

/// <summary>Sends generated-metadata JSON using settings-level content coding.</summary>
internal interface ICompressionApi
{
    /// <summary>Serializes and compresses a person according to the client settings.</summary>
    /// <param name="person">The body to encode.</param>
    /// <returns>The local person reply.</returns>
    [Post("/compression/person")]
    Task<Person> PutAsync([Body(BodySerializationMethod.Serialized)] Person person);
}
