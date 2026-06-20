// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Marks a Refit interface for generated ASP.NET Core Minimal API endpoint descriptors.</summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class GenerateRefitMinimalApiAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="GenerateRefitMinimalApiAttribute"/> class.</summary>
    /// <param name="jsonSerializerContextType">The source-generated JSON metadata context used by generated handlers.</param>
    public GenerateRefitMinimalApiAttribute(Type jsonSerializerContextType)
        : this(jsonSerializerContextType, true)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GenerateRefitMinimalApiAttribute"/> class.</summary>
    /// <param name="jsonSerializerContextType">The source-generated JSON metadata context used by generated handlers.</param>
    /// <param name="generateClient">Whether the Refit client stub should also be generated for the interface.</param>
    public GenerateRefitMinimalApiAttribute(Type jsonSerializerContextType, bool generateClient)
    {
        ArgumentExceptionHelper.ThrowIfNull(jsonSerializerContextType);
        JsonSerializerContextType = jsonSerializerContextType;
        GenerateClient = generateClient;
    }

    /// <summary>Gets the source-generated JSON metadata context used by generated handlers.</summary>
    public Type JsonSerializerContextType { get; }

    /// <summary>Gets a value indicating whether the Refit client stub should also be generated for the interface.</summary>
    public bool GenerateClient { get; }
}
