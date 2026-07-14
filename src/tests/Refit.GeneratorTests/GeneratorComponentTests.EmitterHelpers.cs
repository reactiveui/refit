// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Direct unit tests for the source generator emitter formatting helpers.</summary>
public static partial class GeneratorComponentTests
{
    /// <summary>Tests for direct emitter formatting helpers.</summary>
    public class EmitterHelperTests
    {
        /// <summary>The default body serialization method name.</summary>
        private const string DefaultSerializationMethod = "Default";

        /// <summary>The property name used to test explicit and public property access expressions.</summary>
        private const string TenantPropertyName = "Tenant";

        /// <summary>The non-standard HTTP method attribute name used by candidate-combining tests.</summary>
        private const string CustomMethodName = "Custom";

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
            var builder = new PooledStringBuilder();

            foreach (var value in new[] { '\\', '"', '\0', '\a', '\b', '\f', '\n', '\r', '\t', '\v', '\u0085', '\u2028', '\u2029', 'x' })
            {
                Emitter.AppendEscapedCharacter(builder, value);
            }

            await Assert.That(builder.ToString()).IsEqualTo("""\\\"\0\a\b\f\n\r\t\v\u0085\u2028\u2029x""");
        }

        /// <summary>Verifies the string-literal escape lookup for special, line-terminator, and verbatim characters.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task EscapeSequence_ReturnsEscapesForSpecialCharactersAndNullOtherwise()
        {
            await Assert.That(Emitter.EscapeSequence('\\')).IsEqualTo(@"\\");
            await Assert.That(Emitter.EscapeSequence('"')).IsEqualTo("\\\"");
            await Assert.That(Emitter.EscapeSequence('\0')).IsEqualTo(@"\0");
            await Assert.That(Emitter.EscapeSequence('\a')).IsEqualTo(@"\a");
            await Assert.That(Emitter.EscapeSequence('\b')).IsEqualTo(@"\b");
            await Assert.That(Emitter.EscapeSequence('\f')).IsEqualTo(@"\f");
            await Assert.That(Emitter.EscapeSequence('\n')).IsEqualTo(@"\n");
            await Assert.That(Emitter.EscapeSequence('\r')).IsEqualTo(@"\r");
            await Assert.That(Emitter.EscapeSequence('\t')).IsEqualTo(@"\t");
            await Assert.That(Emitter.EscapeSequence('\v')).IsEqualTo(@"\v");
            await Assert.That(Emitter.EscapeSequence('\u0085')).IsEqualTo(@"\u0085");
            await Assert.That(Emitter.EscapeSequence('\u2028')).IsEqualTo(@"\u2028");
            await Assert.That(Emitter.EscapeSequence('\u2029')).IsEqualTo(@"\u2029");
            await Assert.That(Emitter.EscapeSequence('x')).IsNull();
        }

        /// <summary>Verifies the rendered length of a quoted literal accounts for escapes and the null keyword.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task LiteralOrNullLength_AccountsForEscapesAndNull()
        {
            const int NullLength = 4;
            const int PlainQuotedLength = 5;

            // '"' wrapper (2) + 'a' (1) + '"' escape (2) + U+2028 escape (6) = 11.
            const int EscapedLength = 11;

            await Assert.That(Emitter.LiteralOrNullLength(null)).IsEqualTo(NullLength);
            await Assert.That(Emitter.LiteralOrNullLength("abc")).IsEqualTo(PlainQuotedLength);
            await Assert.That(Emitter.LiteralOrNullLength("a\"\u2028")).IsEqualTo(EscapedLength);
        }

        /// <summary>Verifies the decimal length helper for zero, single-digit, and multi-digit values.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task Int32Length_HandlesZeroAndMultipleDigits()
        {
            const int SingleDigit = 1;
            const int ThreeDigits = 3;
            const int PositiveThreeDigitValue = 123;

            await Assert.That(Emitter.Int32Length(0)).IsEqualTo(SingleDigit);
            await Assert.That(Emitter.Int32Length(SingleDigit)).IsEqualTo(SingleDigit);
            await Assert.That(Emitter.Int32Length(PositiveThreeDigitValue)).IsEqualTo(ThreeDigits);
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
            var urlEncodedBody = CreateBody(UrlEncodedSerializationMethod, BodyBufferMode.Streaming);

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
            var explicitProperty = CreateProperty(TenantPropertyName, "int", TenantInterface, false, true);
            var prefixedExplicitProperty = CreateProperty(TenantPropertyName, "int", GlobalTenantInterface, false, true);
            var publicProperty = CreateProperty(TenantPropertyName, "int", TenantInterface, false, false);

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

            // A verb outside the cached singletons (a custom HTTP method attribute's verb) constructs an HttpMethod.
            await Assert.That(Emitter.ToHttpMethodExpression("TRACE")).IsEqualTo("new global::System.Net.Http.HttpMethod(\"TRACE\")");
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
            await Assert.That(static () => Emitter.GetReturnInvocationParts((ReturnTypeInfo)int.MaxValue))
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
                false,
                false,
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
                    new("first", StringTypeName, false, false),
                    new("second", "global::System.Int32", false, false)
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
            var custom = ParseMethod(CustomMethodName, "[Custom(\"CUSTOM\", \"/custom\")]");

            var result = InterfaceStubGeneratorV2.CombineCandidateMethodsForTesting(
                ([standard], [custom]));

            var names = result.Select(static method => method.Identifier.ValueText).ToArray();

            await Assert.That(names.Length).IsEqualTo(PopulatedPartCount);
            await Assert.That(names[0]).IsEqualTo("Standard");
            await Assert.That(names[1]).IsEqualTo(CustomMethodName);
        }

        /// <summary>Verifies standard HTTP method attribute detection handles suffixes and qualified names.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task IsStandardHttpMethodAttributeName_HandlesQualifiedAndAliasNames()
        {
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName(nameof(GetAttribute)))).IsTrue();
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName("Refit.Post"))).IsTrue();
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName("global::PutAttribute"))).IsTrue();
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName("global::Refit.PutAttribute"))).IsTrue();
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName("Delete"))).IsTrue();
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName(nameof(HeadAttribute)))).IsTrue();
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName("Options"))).IsTrue();
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName(nameof(PatchAttribute)))).IsTrue();
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName("GetAttribute<string>"))).IsFalse();
            await Assert.That(InterfaceStubGeneratorV2.IsStandardHttpMethodAttributeNameForTesting(ParseName(CustomMethodName))).IsFalse();
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
            new("body", StringTypeName, null, ImmutableEquatableArray<ParameterAttributeModel>.Empty, RequestParameterKind.Body, false, string.Empty, string.Empty, serializationMethod, bufferMode);

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
                GeneratedRequestBuilding: true,
                EmitGeneratedCodeMarkers: true,
                SupportsNullable: true,
                SupportsStaticLambdas: true,
                SupportsCollectionExpressions: true,
                ImmutableEquatableArray<TypeConstraint>.Empty,
                ImmutableEquatableArray<string>.Empty,
                ImmutableEquatableArray<InterfacePropertyModel>.Empty,
                ImmutableEquatableArray<MethodModel>.Empty,
                refitMethods,
                derivedRefitMethods,
                Nullability.Enabled,
                false,
                ImmutableEquatableArray<string>.Empty);

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
                    null,
                    ImmutableEquatableArray<HeaderModel>.Empty,
                    ImmutableEquatableArray<RequestParameterModel>.Empty),
                ImmutableEquatableArray<ParameterModel>.Empty,
                ImmutableEquatableArray<TypeConstraint>.Empty,
                false,
                false,
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
}
