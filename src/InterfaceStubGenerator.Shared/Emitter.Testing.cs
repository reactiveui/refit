// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Provides internal accessors for focused generator tests.</summary>
/// <content>Contains test-visible wrappers around private emitter helpers.</content>
internal static partial class Emitter
{
    /// <summary>Escapes text for generated XML documentation comments.</summary>
    /// <param name="value">The text to escape.</param>
    /// <returns>The escaped XML documentation text.</returns>
    internal static string ToXmlDocumentationTextForTesting(string value) => ToXmlDocumentationText(value);

    /// <summary>Builds the generated file header for an interface implementation.</summary>
    /// <param name="nullability">The nullable context for the generated source.</param>
    /// <param name="emitGeneratedCodeMarkers">Whether generated-code markers should be emitted.</param>
    /// <returns>The generated file header.</returns>
    internal static string BuildGeneratedFileHeaderForTesting(Nullability nullability, bool emitGeneratedCodeMarkers) =>
        BuildGeneratedFileHeader(nullability, emitGeneratedCodeMarkers);

    /// <summary>Determines whether an interface can be constructed without a reflection request builder.</summary>
    /// <param name="model">The interface model being emitted.</param>
    /// <returns><see langword="true"/> when all Refit methods use generated request construction.</returns>
    internal static bool CanUseGeneratedSettingsFactoryForTesting(InterfaceModel model) =>
        CanUseGeneratedSettingsFactory(model);

    /// <summary>Builds the generated <c>typeof(...)</c> argument list for method parameters.</summary>
    /// <param name="parameters">The parameter models to emit.</param>
    /// <returns>The generated parameter type list.</returns>
    internal static string BuildParameterTypeListForTesting(ImmutableEquatableArray<ParameterModel> parameters) =>
        BuildParameterTypeList(parameters);

    /// <summary>Joins populated source fragments without allocating a trimmed array.</summary>
    /// <param name="parts">The source fragments.</param>
    /// <param name="count">The populated fragment count.</param>
    /// <param name="separator">The separator text.</param>
    /// <returns>The joined source.</returns>
    internal static string JoinPartsForTesting(string[] parts, int count, string separator) =>
        JoinParts(parts, count, separator);

    /// <summary>Determines whether a type parameter has constraints that should be emitted.</summary>
    /// <param name="typeParameter">The type parameter constraint to inspect.</param>
    /// <param name="isOverrideOrExplicitImplementation">True if emitting for an override or explicit implementation.</param>
    /// <returns><see langword="true"/> when at least one constraint should be emitted.</returns>
    internal static bool HasConstraintKeywordsForTesting(in TypeConstraint typeParameter, bool isOverrideOrExplicitImplementation) =>
        HasConstraintKeywords(typeParameter, isOverrideOrExplicitImplementation);

    /// <summary>Builds the generated body of a Refit method.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="isTopLevel">Whether the method is declared directly on the generated interface.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="uniqueNames">The unique member names in the interface scope.</param>
    /// <param name="requestBuilderFieldName">The generated request-builder field name.</param>
    /// <param name="settingsFieldName">The generated settings field name.</param>
    /// <returns>The generated method implementation.</returns>
    internal static string BuildRefitMethodForTesting(
        MethodModel methodModel,
        bool isTopLevel,
        InterfaceModel interfaceModel,
        UniqueNameBuilder uniqueNames,
        string requestBuilderFieldName,
        string settingsFieldName) =>
        BuildRefitMethod(methodModel, isTopLevel, interfaceModel, uniqueNames, requestBuilderFieldName, settingsFieldName);
}
