// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Documentation;

/// <summary>Builds requests using explicit converters instead of ordinary object flattening.</summary>
internal interface IConverterApi
{
    /// <summary>Uses a custom converter to emit the q and limit query fields.</summary>
    /// <param name="choice">The search text and limit to flatten through the custom converter.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Get("/people")]
    Task<HttpRequestMessage> SearchAsync([QueryConverter(typeof(SearchChoiceConverter))] SearchChoice choice);

    /// <summary>Uses serializer field names under the person query prefix.</summary>
    /// <param name="person">The person model serialized using the configured generated JSON metadata.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Get("/people")]
    Task<HttpRequestMessage> PersonAsync([Query(".", "person")] [QueryConverter(typeof(SystemTextJsonQueryConverter<Person>))] Person person);
}
