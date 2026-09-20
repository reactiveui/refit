// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Refit.GeneratorTests;

/// <summary>Compares a generator run's output with snapshots stored in the test project.</summary>
internal static class GeneratorSnapshot
{
    /// <summary>The environment variable that makes a run write its output over the snapshots.</summary>
    private const string AcceptVariable = "ACCEPT_SNAPSHOTS";

    /// <summary>The assembly metadata key that records the snapshot directory.</summary>
    private const string DirectoryMetadataKey = "GeneratorSnapshotDirectory";

    /// <summary>The suffix of a stored snapshot.</summary>
    private const string VerifiedSuffix = ".verified.cs";

    /// <summary>The suffix of output written beside a snapshot that does not match.</summary>
    private const string ReceivedSuffix = ".received.cs";

    /// <summary>The directory holding the snapshots.</summary>
    private static readonly string SnapshotDirectory = ReadSnapshotDirectory();

    /// <summary>Asserts generated files match the snapshots for a test.</summary>
    /// <param name="driver">The driver after the generator has run.</param>
    /// <param name="typeName">The snapshot name's type segment.</param>
    /// <param name="methodName">The snapshot name's method segment.</param>
    /// <param name="ignoreGeneratedResult">An optional predicate identifying generated results to exclude.</param>
    /// <returns>A task that completes after all generated files have been compared.</returns>
    internal static async Task VerifyAsync(
        GeneratorDriver driver,
        string typeName,
        string methodName,
        Func<GeneratedSourceResult, bool>? ignoreGeneratedResult = null)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentException.ThrowIfNullOrEmpty(methodName);

        var prefix = $"{typeName}.{methodName}#";
        var accept = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(AcceptVariable));
        var produced = new HashSet<string>(StringComparer.Ordinal);
        var failures = new List<string>();

        foreach (var result in driver.GetRunResult().Results)
        {
            foreach (var source in result.GeneratedSources)
            {
                if (ignoreGeneratedResult?.Invoke(source) == true)
                {
                    continue;
                }

                var name = prefix + Path.GetFileNameWithoutExtension(source.HintName);
                _ = produced.Add(name);
                var output = $"\uFEFF//HintName: {source.HintName}\n{Normalize(source.SourceText.ToString())}";
                if (!await StoreAsync(Path.Combine(SnapshotDirectory, name), output, accept))
                {
                    failures.Add($"{name}{VerifiedSuffix} does not match the generated output; see {name}{ReceivedSuffix}");
                }
            }
        }

        foreach (var snapshot in Directory.EnumerateFiles(SnapshotDirectory, $"{prefix}*{VerifiedSuffix}"))
        {
            var fileName = Path.GetFileName(snapshot);
            if (produced.Contains(fileName[..^VerifiedSuffix.Length]))
            {
                continue;
            }

            if (accept)
            {
                File.Delete(snapshot);
            }
            else
            {
                failures.Add($"{fileName} is no longer generated");
            }
        }

        await Assert.That(failures).IsEmpty();
    }

    /// <summary>Settles one generated file against its snapshot.</summary>
    /// <param name="basePath">The snapshot path without its suffix.</param>
    /// <param name="output">The generated output in snapshot form.</param>
    /// <param name="accept">Whether changed output replaces the snapshot.</param>
    /// <returns><see langword="true"/> when the output matched or was accepted.</returns>
    private static async Task<bool> StoreAsync(string basePath, string output, bool accept)
    {
        var verifiedPath = basePath + VerifiedSuffix;
        var receivedPath = basePath + ReceivedSuffix;

        if (File.Exists(verifiedPath) && Normalize(await File.ReadAllTextAsync(verifiedPath)) == Normalize(output))
        {
            File.Delete(receivedPath);
            return true;
        }

        if (accept)
        {
            await File.WriteAllTextAsync(verifiedPath, output);
            File.Delete(receivedPath);
            return true;
        }

        await File.WriteAllTextAsync(receivedPath, output);
        return false;
    }

    /// <summary>Reads the snapshot directory recorded in test assembly metadata.</summary>
    /// <returns>The absolute snapshot directory.</returns>
    /// <exception cref="InvalidOperationException">The test assembly does not record the snapshot directory.</exception>
    private static string ReadSnapshotDirectory()
    {
        foreach (var attribute in typeof(GeneratorSnapshot).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attribute.Key == DirectoryMetadataKey && !string.IsNullOrEmpty(attribute.Value))
            {
                return attribute.Value;
            }
        }

        throw new InvalidOperationException($"The test assembly records no '{DirectoryMetadataKey}' assembly metadata.");
    }

    /// <summary>Normalizes a snapshot's byte-order mark, line endings, and unstable generator version.</summary>
    /// <param name="text">The text to normalize.</param>
    /// <returns>The normalized text.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string Normalize(string text) =>
        string.Join(
            "\n",
            text.TrimStart('\uFEFF')
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .Where(static line => !line.Contains(
                    "System.CodeDom.Compiler.GeneratedCodeAttribute",
                    StringComparison.Ordinal)));
}
