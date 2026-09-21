// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit.Documentation.SampleHost host = new();

await Refit.Documentation.ReflectionClients.RunAsync(host);

await Refit.Documentation.Clients.RunAsync(host);
