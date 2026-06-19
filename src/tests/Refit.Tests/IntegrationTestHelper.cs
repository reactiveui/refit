// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Refit.Tests;

/// <summary>Helpers for locating integration test resource files relative to the test source.</summary>
public static class IntegrationTestHelper
{
    /// <summary>Builds an absolute path under the integration test root directory.</summary>
    /// <param name="paths">The path segments to combine under the test root directory.</param>
    /// <returns>The absolute path to the requested resource.</returns>
    public static string GetPath(params string[] paths)
    {
        var ret = GetIntegrationTestRootDirectory();
        return new FileInfo(paths.Aggregate(ret, Path.Combine)).FullName;
    }

    /// <summary>Gets the directory containing the integration test source files.</summary>
    /// <param name="filePath">The caller source file path, supplied automatically by the compiler.</param>
    /// <returns>The absolute path to the integration test root directory.</returns>
    public static string GetIntegrationTestRootDirectory([CallerFilePath] string filePath = "")
    {
        // XXX: This is an evil hack, but it's okay for a unit test
        // We can't use Assembly.Location because unit test runners love
        // to move stuff to temp directories
        var di = new DirectoryInfo(Path.GetDirectoryName(filePath) ?? string.Empty);

        return di.FullName;
    }
}
