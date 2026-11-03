// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit;
using Refit.NetFrameworkSmoke;

const int ExpectedTodoId = 42;

const int SearchPage = 3;

const string MissingClientFragment = "doesn't look like a Refit interface";

// The smoke types have no source-generated JSON context, so typed replies need the reflection-based metadata.
ReflectionSupport.EnsureRegistered();

var handler = new NetFrameworkSmokeHandler();

using var client = new HttpClient(handler) { BaseAddress = new("https://netfx.refit.test") };

// .NET Framework does not define [ModuleInitializer], so the generator declares the attribute itself. The compiler still
// lowers it into the module constructor, which registers the generated factories when ForGenerated runs it.
var api = RestService.ForGenerated<INetFrameworkSmokeApi>(client);

var fetched = await api.GetTodoAsync(ExpectedTodoId).ConfigureAwait(false);

if (fetched.Id != ExpectedTodoId || fetched.Title != "fetched on net framework")
{
    throw new InvalidOperationException("The .NET Framework GET response was not deserialized correctly.");
}

var created = await api.CreateTodoAsync(new(ExpectedTodoId, "prove net framework")).ConfigureAwait(false);

if (created.Id != ExpectedTodoId || created.Title != "prove net framework")
{
    throw new InvalidOperationException("The .NET Framework POST response was not deserialized correctly.");
}

if (!handler.SawPostBody)
{
    throw new InvalidOperationException("The .NET Framework request body was not serialized through Refit.");
}

var searched = await api.SearchAsync("a b", SearchPage).ConfigureAwait(false);

if (searched != "found" || !handler.SawExpectedQuery)
{
    throw new InvalidOperationException("The .NET Framework generated query string was not constructed correctly.");
}

// Resolving the same interface twice must reuse the cached generated type rather than resolving it again.
if (RestService.ForGenerated<INetFrameworkSmokeApi>(client) is null)
{
    throw new InvalidOperationException("The .NET Framework repeat lookup did not return a generated client.");
}

// An interface the generator never saw must still report that clearly instead of failing some other way.
string? missingClientMessage = null;

try
{
    _ = RestService.ForGenerated<INoGeneratedClientApi>(client);
}
catch (InvalidOperationException ex)
{
    missingClientMessage = ex.Message;
}

if (missingClientMessage?.IndexOf(MissingClientFragment, StringComparison.Ordinal) < 0
    || missingClientMessage is null)
{
    throw new InvalidOperationException("The .NET Framework lookup for a missing generated client did not report it.");
}

Console.WriteLine("Refit .NET Framework smoke test passed.");

/// <summary>The generated top-level program's declaring type, sealed so the JIT can devirtualize its members.</summary>
internal sealed partial class Program
{
    /// <summary>Initializes a new instance of the <see cref="Program"/> class. Unused; the entry point is the generated top-level <c>Main</c>.</summary>
    private Program()
    {
    }
}
