// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit;
using Refit.Documentation;

using SampleHost host = new();

IRequestLookupApi api = RestService.ForGenerated<IRequestLookupApi>(host.Client, host.Settings);

using HttpRequestMessage request = await api.BuildAsync(1);

SampleCheck.Equal("/lookup/1", request.RequestUri?.ToString());

using HttpRequestMessage namedRequest = await api.BuildAsync("Ada");

SampleCheck.Equal("/lookup/Ada", namedRequest.RequestUri?.ToString());
