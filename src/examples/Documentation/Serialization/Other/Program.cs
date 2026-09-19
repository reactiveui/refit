// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit.Documentation;

await OtherSerializers.RunAsync();

Console.WriteLine("Other serializer examples passed.");

/// <summary>Hosts serializer examples that depend on runtime reflection.</summary>
internal sealed partial class Program
{
    /// <summary>Initializes a new instance of the <see cref="Program"/> class.</summary>
    private Program()
    {
    }
}
