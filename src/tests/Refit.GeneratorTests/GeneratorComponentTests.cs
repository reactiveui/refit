// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>
/// Focused unit tests for the individual building blocks of the source generator,
/// exercised directly rather than through end-to-end snapshot generation.
/// </summary>
public static class GeneratorComponentTests
{
    /// <summary>Tests for <see cref="UniqueNameBuilder"/>.</summary>
    public class UniqueNameBuilderTests
    {
        /// <summary>The member name used to test simple generated-name collisions.</summary>
        private const string ClientName = "client";

        /// <summary>The first generated collision suffix for <see cref="ClientName"/>.</summary>
        private const string FirstClientCollisionName = "client0";

        /// <summary>Verifies that an unused name is returned unchanged.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task New_ReturnsOriginalName_WhenUnused()
        {
            var builder = new UniqueNameBuilder();

            await Assert.That(builder.New(ClientName)).IsEqualTo(ClientName);
        }

        /// <summary>Verifies that a numeric suffix is appended when a name collides.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task New_AppendsSuffix_OnCollision()
        {
            var builder = new UniqueNameBuilder();

            await Assert.That(builder.New(ClientName)).IsEqualTo(ClientName);
            await Assert.That(builder.New(ClientName)).IsEqualTo(FirstClientCollisionName);
            await Assert.That(builder.New(ClientName)).IsEqualTo("client1");
        }

        /// <summary>Verifies that a reserved name is never handed out directly.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task Reserve_PreventsName_FromBeingHandedOut()
        {
            var builder = new UniqueNameBuilder();
            builder.Reserve([ClientName]);

            await Assert.That(builder.New(ClientName)).IsEqualTo(FirstClientCollisionName);
        }

        /// <summary>Verifies that reserving an enumerable prevents every supplied name.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task Reserve_Enumerable_PreventsAllNames()
        {
            var builder = new UniqueNameBuilder();
            builder.Reserve([ClientName, "requestBuilder"]);

            await Assert.That(builder.New(ClientName)).IsEqualTo(FirstClientCollisionName);
            await Assert.That(builder.New("requestBuilder")).IsEqualTo("requestBuilder0");
        }

        /// <summary>Verifies that reserving a null enumerable does not throw.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task Reserve_NullEnumerable_DoesNotThrow()
        {
            var builder = new UniqueNameBuilder();

            builder.Reserve((IEnumerable<string>)null!);

            await Assert.That(builder.New(ClientName)).IsEqualTo(ClientName);
        }

        /// <summary>Verifies that independent builders do not share reservations.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task IndependentBuilders_DoNotShareReservations()
        {
            var first = new UniqueNameBuilder();
            first.Reserve([ClientName]);

            var second = new UniqueNameBuilder();

            await Assert.That(second.New(ClientName)).IsEqualTo(ClientName);
        }

        /// <summary>Verifies that names handed out in one builder do not leak to another builder.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task IndependentBuilders_DoNotShareGeneratedNames()
        {
            var first = new UniqueNameBuilder();
            _ = first.New("local");

            var second = new UniqueNameBuilder();

            await Assert.That(second.New("local")).IsEqualTo("local");
        }
    }

    /// <summary>Tests for <see cref="ImmutableEquatableArray{T}"/>.</summary>
    public class ImmutableEquatableArrayTests
    {
        /// <summary>Verifies that arrays with the same sequence are equal and share a hash code.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task Equals_SameSequence_IsTrue()
        {
            var left = new ImmutableEquatableArray<string>(["a", "b", "c"]);
            var right = new ImmutableEquatableArray<string>(["a", "b", "c"]);

            await Assert.That(right).IsEqualTo(left);
            await Assert.That(right.GetHashCode()).IsEqualTo(left.GetHashCode());
        }

        /// <summary>Verifies that arrays with differing sequences are not equal.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task Equals_DifferentSequence_IsFalse()
        {
            var left = new ImmutableEquatableArray<string>(["a", "b", "c"]);
            var right = new ImmutableEquatableArray<string>(["a", "x", "c"]);

            await Assert.That(right).IsNotEqualTo(left);
        }

        /// <summary>Verifies that the empty array has no elements.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task Empty_HasNoElements() => await Assert.That(ImmutableEquatableArray<string>.Empty.Count).IsEqualTo(0);

        /// <summary>Verifies that converting a null source yields an empty array.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task ToImmutableEquatableArray_Null_ReturnsEmpty()
        {
            var result = ((List<string>?)null).ToImmutableEquatableArray();

            await Assert.That(result.Count).IsEqualTo(0);
        }

        /// <summary>Verifies that enumeration yields all values in their original order.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task Enumerator_YieldsAllValuesInOrder()
        {
            const int FirstValue = 10;
            const int SecondValue = 20;
            const int ThirdValue = 30;
            const int ExpectedCount = 3;
            var array = new ImmutableEquatableArray<int>([FirstValue, SecondValue, ThirdValue]);

            var collected = new List<int>(array.Count);
            collected.AddRange(array);

            await Assert.That(collected).IsCollectionEqualTo([FirstValue, SecondValue, ThirdValue]);
            await Assert.That(array.Count).IsEqualTo(ExpectedCount);
            await Assert.That(array[1]).IsEqualTo(SecondValue);
            await Assert.That(array.AsArray()).IsSameReferenceAs(array.AsArray());
            await Assert.That(array.Equals(NullIntArray())).IsFalse();
        }

        /// <summary>Returns a null immutable array reference without making the call site a constant condition.</summary>
        /// <returns>A null array reference.</returns>
        private static ImmutableEquatableArray<int>? NullIntArray() => null;
    }

    /// <summary>Tests for direct emitter formatting helpers.</summary>
    public class EmitterHelperTests
    {
        /// <summary>The default body serialization method name.</summary>
        private const string DefaultSerializationMethod = "Default";

        /// <summary>The generated false literal.</summary>
        private const string FalseLiteral = "false";

        /// <summary>The generated true literal.</summary>
        private const string TrueLiteral = "true";

        /// <summary>The generated fully-qualified task type name.</summary>
        private const string TaskTypeName = "global::System.Threading.Tasks.Task";

        /// <summary>The generated string type name.</summary>
        private const string StringTypeName = "string";

        /// <summary>The populated part count used by join helper tests.</summary>
        private const int PopulatedPartCount = 2;

        /// <summary>Verifies escaping every special C# string-literal character.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task AppendEscapedCharacter_HandlesSpecialCharacters()
        {
            var builder = new StringBuilder();

            foreach (var value in new[] { '\\', '"', '\0', '\a', '\b', '\f', '\n', '\r', '\t', '\v', 'x' })
            {
                Emitter.AppendEscapedCharacter(builder, value);
            }

            await Assert.That(builder.ToString()).IsEqualTo(@"\\\""\0\a\b\f\n\r\t\vx");
        }

        /// <summary>Verifies body buffering and streaming expressions for all supported modes.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task BodyExpressionHelpers_HandleBufferModes()
        {
            const string settings = "refitSettings";
            var settingsBody = CreateBody(DefaultSerializationMethod, BodyBufferMode.Settings);
            var bufferedBody = CreateBody(DefaultSerializationMethod, BodyBufferMode.Buffered);
            var streamingBody = CreateBody(DefaultSerializationMethod, BodyBufferMode.Streaming);
            var noneBody = CreateBody(DefaultSerializationMethod, BodyBufferMode.None);
            var urlEncodedBody = CreateBody("UrlEncoded", BodyBufferMode.Streaming);

            await Assert.That(Emitter.BuildBufferBodyExpression(null, settings)).IsEqualTo(FalseLiteral);
            await Assert.That(Emitter.BuildBufferBodyExpression(settingsBody, settings)).IsEqualTo($"{settings}.Buffered");
            await Assert.That(Emitter.BuildBufferBodyExpression(bufferedBody, settings)).IsEqualTo(TrueLiteral);
            await Assert.That(Emitter.BuildBufferBodyExpression(streamingBody, settings)).IsEqualTo(FalseLiteral);
            await Assert.That(Emitter.BuildBufferBodyExpression(noneBody, settings)).IsEqualTo(FalseLiteral);
            await Assert.That(Emitter.BuildStreamBodyExpression(settingsBody, settings)).IsEqualTo($"!{settings}.Buffered");
            await Assert.That(Emitter.BuildStreamBodyExpression(bufferedBody, settings)).IsEqualTo(FalseLiteral);
            await Assert.That(Emitter.BuildStreamBodyExpression(streamingBody, settings)).IsEqualTo(TrueLiteral);
            await Assert.That(Emitter.BuildStreamBodyExpression(noneBody, settings)).IsEqualTo(FalseLiteral);
            await Assert.That(Emitter.BuildStreamBodyExpression(urlEncodedBody, settings)).IsEqualTo(FalseLiteral);

            // With a different settings local name, the expression follows it (collision-avoidance threading).
            await Assert.That(Emitter.BuildBufferBodyExpression(settingsBody, "renamed")).IsEqualTo("renamed.Buffered");
            await Assert.That(Emitter.BuildStreamBodyExpression(settingsBody, "renamed")).IsEqualTo("!renamed.Buffered");
        }

        /// <summary>Verifies property access and global-prefix helpers.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task PropertyAccessHelpers_HandleGeneratedExplicitAndPublicProperties()
        {
            const string TenantInterface = "RefitGeneratorTest.ITenant";
            const string GlobalTenantInterface = "global::RefitGeneratorTest.ITenant";

            var generatedProperty = CreateProperty("Client", "global::System.Net.Http.HttpClient", "RefitGeneratorTest.IClient", true, false);
            var explicitProperty = CreateProperty("Tenant", "int", TenantInterface, false, true);
            var prefixedExplicitProperty = CreateProperty("Tenant", "int", GlobalTenantInterface, false, true);
            var publicProperty = CreateProperty("Tenant", "int", TenantInterface, false, false);

            await Assert.That(Emitter.BuildPropertyAccessExpression(generatedProperty)).IsEqualTo("this.Client");
            await Assert.That(Emitter.BuildPropertyAccessExpression(explicitProperty))
                .IsEqualTo($"(({GlobalTenantInterface})this).Tenant");
            await Assert.That(Emitter.BuildPropertyAccessExpression(prefixedExplicitProperty))
                .IsEqualTo($"(({GlobalTenantInterface})this).Tenant");
            await Assert.That(Emitter.BuildPropertyAccessExpression(publicProperty)).IsEqualTo("this.Tenant");
            await Assert.That(Emitter.EnsureGlobalPrefix(TenantInterface)).IsEqualTo(GlobalTenantInterface);
            await Assert.That(Emitter.EnsureGlobalPrefix(GlobalTenantInterface)).IsEqualTo(GlobalTenantInterface);
        }

        /// <summary>Verifies HTTP method, literal, and explicit-prefix formatting helpers.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task LiteralAndHttpMethodHelpers_HandleKnownAndInvalidValues()
        {
            await Assert.That(Emitter.ToNullableCSharpStringLiteral(null)).IsEqualTo("null");
            await Assert.That(Emitter.ToNullableCSharpStringLiteral("value")).IsEqualTo("\"value\"");
            await Assert.That(Emitter.ToHttpMethodExpression("DELETE")).IsEqualTo("global::System.Net.Http.HttpMethod.Delete");
            await Assert.That(Emitter.ToHttpMethodExpression("GET")).IsEqualTo("global::System.Net.Http.HttpMethod.Get");
            await Assert.That(Emitter.ToHttpMethodExpression("HEAD")).IsEqualTo("global::System.Net.Http.HttpMethod.Head");
            await Assert.That(Emitter.ToHttpMethodExpression("OPTIONS")).IsEqualTo("global::System.Net.Http.HttpMethod.Options");
            await Assert.That(Emitter.ToHttpMethodExpression("POST")).IsEqualTo("global::System.Net.Http.HttpMethod.Post");
            await Assert.That(Emitter.ToHttpMethodExpression("PUT")).IsEqualTo("global::System.Net.Http.HttpMethod.Put");
            await Assert.That(Emitter.ToHttpMethodExpression("PATCH")).IsEqualTo("new global::System.Net.Http.HttpMethod(\"PATCH\")");
            await Assert.That(Emitter.StripExplicitInterfacePrefix("IFoo.Bar")).IsEqualTo("Bar");
            await Assert.That(Emitter.StripExplicitInterfacePrefix("IFoo.")).IsEqualTo("IFoo.");
            await Assert.That(Emitter.StripExplicitInterfacePrefix("Bar")).IsEqualTo("Bar");
            await Assert.That(() => Emitter.ToHttpMethodExpression("TRACE")).ThrowsExactly<ArgumentOutOfRangeException>();
        }

        /// <summary>Verifies generated return invocation text for every return type shape.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task ReturnInvocationParts_HandleKnownAndInvalidValues()
        {
            await Assert.That(Emitter.GetReturnInvocationParts(ReturnTypeInfo.AsyncVoid))
                .IsEqualTo((true, "await (", ").ConfigureAwait(false)"));
            await Assert.That(Emitter.GetReturnInvocationParts(ReturnTypeInfo.AsyncResult))
                .IsEqualTo((true, "return await (", ").ConfigureAwait(false)"));
            await Assert.That(Emitter.GetReturnInvocationParts(ReturnTypeInfo.Return))
                .IsEqualTo((false, "return ", string.Empty));
            await Assert.That(Emitter.GetReturnInvocationParts(ReturnTypeInfo.SyncVoid))
                .IsEqualTo((false, string.Empty, string.Empty));
            await Assert.That(() => Emitter.GetReturnInvocationParts((ReturnTypeInfo)int.MaxValue))
                .ThrowsExactly<ArgumentOutOfRangeException>();
        }

        /// <summary>Verifies explicit method openings receive a global interface qualifier.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task BuildMethodOpening_QualifiesExplicitInterfaceMethods()
        {
            var method = new MethodModel(
                "Ping",
                TaskTypeName,
                "RefitGeneratorTest.IBase",
                "Ping",
                ReturnTypeInfo.AsyncVoid,
                RequestModel.Empty,
                ImmutableEquatableArray<ParameterModel>.Empty,
                ImmutableEquatableArray<TypeConstraint>.Empty,
                false);

            var source = Emitter.BuildMethodOpening(method, true, true, supportsNullable: true, isAsync: true);

            await Assert.That(source)
                .Contains("async global::System.Threading.Tasks.Task global::RefitGeneratorTest.IBase.Ping(");
        }

        /// <summary>Verifies emitted XML documentation text escapes special characters.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task ToXmlDocumentationText_EscapesSpecialCharacters() =>
            await Assert.That(Emitter.ToXmlDocumentationTextForTesting("A&B<C>")).IsEqualTo("A&amp;B&lt;C&gt;");

        /// <summary>Verifies generated file headers for analyzer-enabled generated code tests.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task BuildGeneratedFileHeader_SupportsNonGeneratedMarkers()
        {
            var noneHeader = Emitter.BuildGeneratedFileHeaderForTesting(Nullability.None, emitGeneratedCodeMarkers: false);
            var nullableHeader = Emitter.BuildGeneratedFileHeaderForTesting(Nullability.Enabled, emitGeneratedCodeMarkers: false);

            await Assert.That(noneHeader).Contains("ReactiveUI and Contributors");
            await Assert.That(noneHeader).DoesNotContain("#nullable");
            await Assert.That(nullableHeader).Contains("#nullable enable annotations");
        }

        /// <summary>Verifies generated settings factories require at least one inline-capable Refit method.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task CanUseGeneratedSettingsFactory_RejectsEmptyInterfaces()
        {
            var emptyModel = CreateInterfaceModel(
                ImmutableEquatableArray<MethodModel>.Empty,
                ImmutableEquatableArray<MethodModel>.Empty);
            var inlineModel = CreateInterfaceModel(
                new([CreateRefitMethod(canGenerateInline: true)]),
                ImmutableEquatableArray<MethodModel>.Empty);

            await Assert.That(Emitter.CanUseGeneratedSettingsFactoryForTesting(emptyModel)).IsFalse();
            await Assert.That(Emitter.CanUseGeneratedSettingsFactoryForTesting(inlineModel)).IsTrue();
        }

        /// <summary>Verifies parameter type-list helpers handle empty and populated parameter lists.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task BuildParameterTypeList_HandlesEmptyAndPopulatedParameters()
        {
            var parameters = new ImmutableEquatableArray<ParameterModel>(
                [
                    new("first", null, StringTypeName, false, false),
                    new("second", null, "global::System.Int32", false, false)
                ]);

            await Assert.That(Emitter.BuildParameterTypeListForTesting(ImmutableEquatableArray<ParameterModel>.Empty))
                .IsEqualTo(string.Empty);
            await Assert.That(Emitter.BuildParameterTypeListForTesting(parameters))
                .IsEqualTo("typeof(string), typeof(global::System.Int32)");
        }

        /// <summary>Verifies source fragment joining avoids extra separators for empty input.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task JoinParts_HandlesEmptyAndPopulatedParts()
        {
            await Assert.That(Emitter.JoinPartsForTesting([], 0, ", ")).IsEqualTo(string.Empty);
            await Assert.That(Emitter.JoinPartsForTesting(["a", "b", "ignored"], PopulatedPartCount, ", ")).IsEqualTo("a, b");
        }

        /// <summary>Verifies candidate method combining preserves standard and custom methods.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task CombineCandidateMethods_CombinesStandardAndCustomMethods()
        {
            var standard = ParseMethod("Standard", "[Get(\"/standard\")]");
            var custom = ParseMethod("Custom", "[Custom(\"CUSTOM\", \"/custom\")]");

            var result = InterfaceStubGeneratorV2.CombineCandidateMethodsForTesting(
                ([standard], [custom]));

            var names = result.Select(static method => method.Identifier.ValueText).ToArray();

            await Assert.That(names.Length).IsEqualTo(PopulatedPartCount);
            await Assert.That(names[0]).IsEqualTo("Standard");
            await Assert.That(names[1]).IsEqualTo("Custom");
        }

        /// <summary>Verifies standard HTTP method attribute detection handles suffixes and qualified names.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task IsStandardHttpMethodAttributeName_HandlesQualifiedAndAliasNames()
        {
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName("GetAttribute"))).IsTrue();
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName("Refit.Post"))).IsTrue();
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName("global::PutAttribute"))).IsTrue();
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName("global::Refit.PutAttribute"))).IsTrue();
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName("GetAttribute<string>"))).IsFalse();
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName("Custom"))).IsFalse();
        }

        /// <summary>Verifies an explicitly-implemented Refit method emits an explicit interface prefix on both paths.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task BuildRefitMethod_EmitsExplicitInterfaceForExplicitMethod()
        {
            var explicitReflective = CreateRefitMethod(canGenerateInline: false) with { IsExplicitInterface = true };
            var explicitInline = CreateRefitMethod(canGenerateInline: true) with { IsExplicitInterface = true };
            var model = CreateInterfaceModel(
                new([explicitReflective]),
                ImmutableEquatableArray<MethodModel>.Empty);

            var reflective = Emitter.BuildRefitMethodForTesting(
                explicitReflective,
                isTopLevel: true,
                model,
                new(),
                "_requestBuilder",
                "_settings");
            var inline = Emitter.BuildRefitMethodForTesting(
                explicitInline,
                isTopLevel: true,
                model,
                new(),
                "_requestBuilder",
                "_settings");

            await Assert.That(reflective).Contains("global::RefitGeneratorTest.IGeneratedClient.Get");
            await Assert.That(inline).Contains("global::RefitGeneratorTest.IGeneratedClient.Get");
        }

        /// <summary>Verifies constraint emission suppresses override-incompatible keywords for explicit implementations.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task HasConstraintKeywords_HandlesOverrideAndNonOverridePaths()
        {
            // class/struct are always emitted; unmanaged/notnull/new/textual are suppressed on overrides/explicit impls.
            await Assert.That(Emitter.HasConstraintKeywordsForTesting(Constraint(KnownTypeConstraint.Class), true)).IsTrue();
            await Assert.That(Emitter.HasConstraintKeywordsForTesting(Constraint(KnownTypeConstraint.Struct), true)).IsTrue();
            await Assert.That(Emitter.HasConstraintKeywordsForTesting(Constraint(KnownTypeConstraint.Unmanaged), true)).IsFalse();
            await Assert.That(Emitter.HasConstraintKeywordsForTesting(Constraint(KnownTypeConstraint.NotNull), true)).IsFalse();
            await Assert.That(Emitter.HasConstraintKeywordsForTesting(Constraint(KnownTypeConstraint.New), true)).IsFalse();
            await Assert.That(Emitter.HasConstraintKeywordsForTesting(Constraint(KnownTypeConstraint.None, "global::System.IDisposable"), true)).IsFalse();

            await Assert.That(Emitter.HasConstraintKeywordsForTesting(Constraint(KnownTypeConstraint.Unmanaged), false)).IsTrue();
            await Assert.That(Emitter.HasConstraintKeywordsForTesting(Constraint(KnownTypeConstraint.NotNull), false)).IsTrue();
            await Assert.That(Emitter.HasConstraintKeywordsForTesting(Constraint(KnownTypeConstraint.New), false)).IsTrue();
            await Assert.That(Emitter.HasConstraintKeywordsForTesting(Constraint(KnownTypeConstraint.None, "global::System.IDisposable"), false)).IsTrue();
            await Assert.That(Emitter.HasConstraintKeywordsForTesting(Constraint(KnownTypeConstraint.None), false)).IsFalse();
        }

        /// <summary>Creates a type-parameter constraint model.</summary>
        /// <param name="known">The well-known constraint flags.</param>
        /// <param name="extra">Additional textual constraints.</param>
        /// <returns>The type constraint.</returns>
        private static TypeConstraint Constraint(KnownTypeConstraint known, params string[] extra)
        {
            var textual = extra.Length == 0
                ? ImmutableEquatableArray<string>.Empty
                : new(extra);
            return new("T", "T", known, textual);
        }

        /// <summary>Creates a body request parameter model.</summary>
        /// <param name="serializationMethod">The serialization method name.</param>
        /// <param name="bufferMode">The body buffer mode.</param>
        /// <returns>The request parameter model.</returns>
        private static RequestParameterModel CreateBody(string serializationMethod, BodyBufferMode bufferMode) =>
            new("body", StringTypeName, RequestParameterKind.Body, false, string.Empty, string.Empty, serializationMethod, bufferMode);

        /// <summary>Creates an interface property model.</summary>
        /// <param name="name">The property name.</param>
        /// <param name="type">The property type.</param>
        /// <param name="containingType">The containing type display name.</param>
        /// <param name="generated">Whether it is satisfied by a generated member.</param>
        /// <param name="explicitInterface">Whether it is implemented explicitly.</param>
        /// <returns>The interface property model.</returns>
        private static InterfacePropertyModel CreateProperty(
            string name,
            string type,
            string containingType,
            bool generated,
            bool explicitInterface) =>
            new(name, type, false, containingType, string.Empty, true, true, generated, explicitInterface);

        /// <summary>Creates an interface model for direct emitter helper tests.</summary>
        /// <param name="refitMethods">The directly declared Refit methods.</param>
        /// <param name="derivedRefitMethods">The inherited Refit methods.</param>
        /// <returns>The interface model.</returns>
        private static InterfaceModel CreateInterfaceModel(
            ImmutableEquatableArray<MethodModel> refitMethods,
            ImmutableEquatableArray<MethodModel> derivedRefitMethods) =>
            new(
                "RefitInternalGenerated.PreserveAttribute",
                "IGeneratedClient.g.cs",
                "IGeneratedClient",
                "RefitGeneratorTest",
                "public partial class IGeneratedClient",
                "RefitGeneratorTest.IGeneratedClient",
                string.Empty,
                true,
                true,
                true,
                ImmutableEquatableArray<TypeConstraint>.Empty,
                ImmutableEquatableArray<string>.Empty,
                ImmutableEquatableArray<InterfacePropertyModel>.Empty,
                ImmutableEquatableArray<MethodModel>.Empty,
                refitMethods,
                derivedRefitMethods,
                Nullability.Enabled,
                false);

        /// <summary>Creates a Refit method model for direct emitter helper tests.</summary>
        /// <param name="canGenerateInline">Whether the request can be generated inline.</param>
        /// <returns>The method model.</returns>
        private static MethodModel CreateRefitMethod(bool canGenerateInline) =>
            new(
                "Get",
                TaskTypeName,
                "RefitGeneratorTest.IGeneratedClient",
                "Get",
                ReturnTypeInfo.AsyncVoid,
                new(
                    "GET",
                    "/",
                    TaskTypeName,
                    "global::System.Threading.Tasks.Task",
                    false,
                    true,
                    canGenerateInline,
                    ImmutableEquatableArray<HeaderModel>.Empty,
                    ImmutableEquatableArray<RequestParameterModel>.Empty,
                    ImmutableEquatableArray<RouteFragmentModel>.Empty),
                ImmutableEquatableArray<ParameterModel>.Empty,
                ImmutableEquatableArray<TypeConstraint>.Empty,
                false);

        /// <summary>Parses a method declaration for syntax helper tests.</summary>
        /// <param name="name">The method name.</param>
        /// <param name="attribute">The attribute source.</param>
        /// <returns>The method declaration syntax.</returns>
        private static MethodDeclarationSyntax ParseMethod(string name, string attribute)
        {
            var compilationUnit = SyntaxFactory.ParseCompilationUnit(
                $"public interface I {{ {attribute} void {name}(); }}");
            return compilationUnit.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        }

        /// <summary>Parses an attribute name for syntax helper tests.</summary>
        /// <param name="name">The name source.</param>
        /// <returns>The parsed name syntax.</returns>
        private static NameSyntax ParseName(string name) =>
            SyntaxFactory.ParseName(name);
    }

    /// <summary>Tests for direct parser request helpers.</summary>
    public class ParserRequestHelperTests
    {
        /// <summary>The simple path used by parser helper assertions.</summary>
        private const string SimplePath = "/path";

        /// <summary>The number of characters checked in whitespace assertions.</summary>
        private const int WhitespaceLength = 2;

        /// <summary>The enum value for URL encoded body serialization.</summary>
        private const int UrlEncodedSerializationValue = 2;

        /// <summary>The enum value for serialized body serialization.</summary>
        private const int SerializedSerializationValue = 3;

        /// <summary>The enum value for JSON Lines body serialization.</summary>
        private const int JsonLinesSerializationValue = 4;

        /// <summary>An unsupported body serialization enum value.</summary>
        private const int UnsupportedSerializationValue = 99;

        /// <summary>Verifies inline path normalization and constant path classification.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task InlinePathHelpers_NormalizeAndClassifyPaths()
        {
            await Assert.That(Parser.NormalizeConstantPathForInline(SimplePath)).IsEqualTo(SimplePath);
            await Assert.That(Parser.NormalizeConstantPathForInline("/path?")).IsEqualTo(SimplePath);
            await Assert.That(Parser.NormalizeConstantPathForInline("/path?& \t =drop")).IsEqualTo(SimplePath);
            await Assert.That(Parser.NormalizeConstantPathForInline("/path?one=1&&two=2#fragment")).IsEqualTo("/path?one=1&two=2");
            await Assert.That(Parser.IsConstantPathSupported(string.Empty)).IsTrue();
            await Assert.That(Parser.IsConstantPathSupported(SimplePath)).IsTrue();
            await Assert.That(Parser.IsConstantPathSupported("relative")).IsFalse();
            await Assert.That(Parser.IsConstantPathSupported("/{id}")).IsTrue();
            await Assert.That(Parser.IsConstantPathSupported("/id}")).IsTrue();
            await Assert.That(Parser.IsConstantPathSupported("/line\nbreak")).IsFalse();
            await Assert.That(Parser.IsConstantPathSupported("/line\rbreak")).IsFalse();
            await Assert.That(Parser.IsWhiteSpace(" \t", 0, WhitespaceLength)).IsTrue();
            await Assert.That(Parser.IsWhiteSpace(" a", 0, WhitespaceLength)).IsFalse();
        }

        /// <summary>Verifies static header merging behavior.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task AddStaticHeader_SkipsBlankAndReplacesExistingValues()
        {
            const int ExpectedHeaderCount = 2;
            var headers = new List<HeaderModel>();

            Parser.AddStaticHeader(headers, " ");
            Parser.AddStaticHeader(headers, "X-One");
            Parser.AddStaticHeader(headers, "X-Two: two");
            Parser.AddStaticHeader(headers, "X-One: replaced");

            await Assert.That(headers.Count).IsEqualTo(ExpectedHeaderCount);
            await Assert.That(headers[0].Name).IsEqualTo("X-One");
            await Assert.That(headers[0].Value).IsEqualTo("replaced");
            await Assert.That(headers[1].Name).IsEqualTo("X-Two");
            await Assert.That(headers[1].Value).IsEqualTo("two");
        }

        /// <summary>Verifies body serialization, inline-body eligibility, and response disposal helpers.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task BodyAndDisposalHelpers_ClassifySupportedValues()
        {
            await Assert.That(Parser.GetBodySerializationMethodName(0)).IsEqualTo("Default");
            await Assert.That(Parser.GetBodySerializationMethodName(1)).IsEqualTo("Json");
            await Assert.That(Parser.GetBodySerializationMethodName(UrlEncodedSerializationValue)).IsEqualTo("UrlEncoded");
            await Assert.That(Parser.GetBodySerializationMethodName(SerializedSerializationValue)).IsEqualTo("Serialized");
            await Assert.That(Parser.GetBodySerializationMethodName(JsonLinesSerializationValue)).IsEqualTo("JsonLines");
            await Assert.That(Parser.GetBodySerializationMethodName(UnsupportedSerializationValue)).IsEqualTo(string.Empty);
            await Assert.That(Parser.IsSupportedInlineBody(ImmutableEquatableArray<RequestParameterModel>.Empty)).IsTrue();
            await Assert.That(Parser.IsSupportedInlineBody(new([CreateHeaderParameter()]))).IsTrue();
            await Assert.That(Parser.IsSupportedInlineBody(new([CreateBody(string.Empty)]))).IsFalse();
            await Assert.That(Parser.IsSupportedInlineBody(new([CreateBody("UrlEncoded")]))).IsTrue();
            await Assert.That(Parser.IsSupportedInlineBody(new([CreateBody("Serialized")]))).IsTrue();
            await Assert.That(Parser.IsSupportedInlineBody(new([CreateBody("JsonLines")]))).IsTrue();
            await Assert.That(Parser.ShouldDisposeResponse("global::System.Net.Http.HttpResponseMessage")).IsFalse();
            await Assert.That(Parser.ShouldDisposeResponse("global::System.Net.Http.HttpContent")).IsFalse();
            await Assert.That(Parser.ShouldDisposeResponse("global::System.IO.Stream")).IsFalse();
            await Assert.That(Parser.ShouldDisposeResponse("global::System.String")).IsTrue();
        }

        /// <summary>Creates a non-body parameter model.</summary>
        /// <returns>The request parameter model.</returns>
        private static RequestParameterModel CreateHeaderParameter() =>
            new("query", "string", RequestParameterKind.Header, true, string.Empty, string.Empty, string.Empty, BodyBufferMode.None);

        /// <summary>Creates a body parameter model.</summary>
        /// <param name="serializationMethod">The serialization method name.</param>
        /// <returns>The request parameter model.</returns>
        private static RequestParameterModel CreateBody(string serializationMethod) =>
            new("body", "string", RequestParameterKind.Body, false, string.Empty, string.Empty, serializationMethod, BodyBufferMode.Buffered);
    }

    /// <summary>Tests for the <c>ITypeSymbol</c> generator extension helpers.</summary>
    public class ITypeSymbolExtensionsTests
    {
        /// <summary>The derived test type name used by inheritance assertions.</summary>
        private const string DerivedTypeName = "Derived";

        /// <summary>Verifies that inheritance checks walk through the full base-type chain.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task InheritsFromOrEquals_TransitiveBaseType_IsTrue()
        {
            var compilation = Compile("""
                public class Base { }
                public class Middle : Base { }
                public class Derived : Middle { }
                """);
            var derived = GetType(compilation, DerivedTypeName);
            var baseType = GetType(compilation, "Base");

            await Assert.That(derived.InheritsFromOrEquals(baseType)).IsTrue();
        }

        /// <summary>Verifies that a type inherits from or equals itself.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task InheritsFromOrEquals_SameType_IsTrue()
        {
            var compilation = Compile("public class Derived { }");
            var derived = GetType(compilation, DerivedTypeName);

            await Assert.That(derived.InheritsFromOrEquals(derived)).IsTrue();
        }

        /// <summary>Verifies that a derived type inherits from its base type.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task InheritsFromOrEquals_BaseType_IsTrue()
        {
            var compilation = Compile("""
                public class Base { }
                public class Derived : Base { }
                """);

            await Assert.That(
                GetType(compilation, DerivedTypeName).InheritsFromOrEquals(GetType(compilation, "Base"))).IsTrue();
        }

        /// <summary>Verifies that unrelated types do not inherit from one another.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task InheritsFromOrEquals_UnrelatedType_IsFalse()
        {
            var compilation = Compile("""
                public class Foo { }
                public class Bar { }
                """);

            await Assert.That(
                GetType(compilation, "Foo").InheritsFromOrEquals(GetType(compilation, "Bar"))).IsFalse();
        }

        /// <summary>Verifies that interface inheritance is only considered when the include-interfaces flag is set.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task InheritsFromOrEquals_Interface_HonorsIncludeInterfacesFlag()
        {
            var compilation = Compile("""
                public interface IThing { }
                public class Thing : IThing { }
                """);
            var thing = GetType(compilation, "Thing");
            var thingInterface = GetType(compilation, "IThing");

            await Assert.That(thing.InheritsFromOrEquals(thingInterface, includeInterfaces: false)).IsFalse();
            await Assert.That(thing.InheritsFromOrEquals(thingInterface, includeInterfaces: true)).IsTrue();
        }

        /// <summary>Compiles the supplied C# source into an in-memory compilation.</summary>
        /// <param name="source">The C# source to compile.</param>
        /// <returns>The resulting compilation.</returns>
        private static CSharpCompilation Compile(string source)
        {
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));

            return CSharpCompilation.Create(
                "TypeSymbolTests",
                [CSharpSyntaxTree.ParseText(source)],
                references,
                new(OutputKind.DynamicallyLinkedLibrary));
        }

        /// <summary>Resolves a named type symbol from the compilation by metadata name.</summary>
        /// <param name="compilation">The compilation to search.</param>
        /// <param name="typeName">The metadata name of the type to find.</param>
        /// <returns>The resolved type symbol.</returns>
        private static INamedTypeSymbol GetType(Compilation compilation, string typeName) =>
            compilation.GetTypeByMetadataName(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' was not found.");
    }
}
