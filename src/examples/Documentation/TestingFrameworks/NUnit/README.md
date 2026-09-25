# Testing a Refit client with NUnit

Run from `src/`: `dotnet test examples/Documentation/TestingFrameworks/NUnit/NUnitTests.csproj`.

The same scenarios exist, unchanged, for [xUnit v3](../XUnit), [MSTest](../MSTest) and [TUnit](../TUnit) — only
the test attributes and assert calls differ, so you can compare frameworks side by side.

## Scenarios

- **GettingStartedTests** — reading a reply (`GetPerson_ReturnsTheStubbedPerson`) and checking the JSON your app
  sent (`CreatePerson_SendsTheRightJsonBody`).
- **ErrorHandlingTests** — a 404 response (`GetMissingPerson_ReturnsNotFound`) and catching a test that forgot to
  call its API (`ForgottenApiCall_FailsVerification`).
- **NetworkConditionsTests** — a slow server tested without waiting in real time
  (`SlowServer_DelaysTheReplyWithoutWaiting`) and a dropped connection (`BrokenNetwork_ThrowsAConnectionError`).
- **StreamingTests** — reacting to each item as it streams in (`WatchPeople_SeesEachOneAsTheyArrive`) and proving
  an upload is not fully buffered before the server starts reading it (`UploadPeople_ServerDoesNotWaitForTheWholeUpload`).
