// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>Refit service used to exercise XML response deserialization.</summary>
public interface IXmlResponseService
{
    /// <summary>Gets the XML response object from the test endpoint.</summary>
    /// <returns>The deserialized <see cref="XmlResponse"/>.</returns>
    [Get("/xmlTest")]
    Task<XmlResponse> GetXmlObject();
}
