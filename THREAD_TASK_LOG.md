# Thread Task Log

## Completed

- [x] Investigated the latest open Refit issues and selected the latest 5 actionable issues (excluding Renovate dashboard automation issue):
  - #2058 Newtonsoft deserialization blocking managed threads
  - #2056 Customer ID header incorrectly sent in microservices communication
  - #1761 AuthorizationHeaderValueGetter ignored for some `RestService.For` overloads
  - #1889 Add possibility to ignore property when building query
  - #2050 Unable inherit normally from regular interface
- [x] Added/updated tests proving fixes and current behavior for #2058, #1761, and #1889.
- [x] Updated README with behavior/documentation changes:
  - Query DTO ignore attributes support
  - AuthorizationHeaderValueGetter behavior with supplied `HttpClient`
  - Notes for per-request values and handler lifetime safety in `IHttpClientFactory`
- [x] Validated test suites after changes:
  - `dotnet test Refit.Tests/Refit.Tests.csproj -f net8.0`
  - `dotnet test Refit.GeneratorTests/Refit.GeneratorTests.csproj -f net8.0`
- [x] Updated examples structure to use the `examples/` folder in solution and verified build.

## Remaining

- [ ] Final decision on #2050 (`non-Refit` base interface inheritance) behavior:
  - Option A: keep current analyzer warning behavior (non-breaking)
  - Option B: suppress generation warnings for non-Refit inherited members (behavioral change)
- [ ] Decide if any additional AoT-focused breaking changes are acceptable (e.g., stricter API constraints or source-generator-only paths).
- [ ] Commit all changes for this thread once final direction is approved.
