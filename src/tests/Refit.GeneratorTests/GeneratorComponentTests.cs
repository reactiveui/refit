// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>
/// Focused unit tests for the individual building blocks of the source generator,
/// exercised directly rather than through end-to-end snapshot generation.
/// </summary>
public static partial class GeneratorComponentTests
{
    /// <summary>The URL-encoded body serialization method name shared across emitter and parser helper tests.</summary>
    private const string UrlEncodedSerializationMethod = "UrlEncoded";

    /// <summary>Tests for <see cref="UniqueNameBuilder"/>.</summary>
    public class UniqueNameBuilderTests
    {
        /// <summary>The member name used to test simple generated-name collisions.</summary>
        private const string ClientName = "client";

        /// <summary>The first generated collision suffix for <see cref="ClientName"/>.</summary>
        private const string FirstClientCollisionName = "client0";

        /// <summary>The generated local name used to test cross-builder name isolation.</summary>
        private const string GeneratedLocalName = "local";

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
            _ = first.New(GeneratedLocalName);

            var second = new UniqueNameBuilder();

            await Assert.That(second.New(GeneratedLocalName)).IsEqualTo(GeneratedLocalName);
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

        /// <summary>Verifies the object-typed equality override matches equal arrays and rejects unrelated objects.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task Equals_Object_MatchesEqualArraysAndRejectsUnrelatedObjects()
        {
            const int FirstValue = 1;
            const int SecondValue = 2;
            var array = new ImmutableEquatableArray<int>([FirstValue, SecondValue]);
            var equal = new ImmutableEquatableArray<int>([FirstValue, SecondValue]);

            await Assert.That(array.Equals((object)equal)).IsTrue();
            await Assert.That(array.Equals((object)"not an array")).IsFalse();
        }

        /// <summary>Verifies the non-generic enumerator yields every element in order.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task NonGenericEnumerator_YieldsAllValues()
        {
            const int FirstValue = 7;
            const int SecondValue = 8;
            var array = new ImmutableEquatableArray<int>([FirstValue, SecondValue]);

            var collected = new List<int>();
            foreach (var value in (System.Collections.IEnumerable)array)
            {
                collected.Add((int)value);
            }

            await Assert.That(collected).IsCollectionEqualTo([FirstValue, SecondValue]);
        }

        /// <summary>Verifies both explicit enumerator paths yield nothing for a defaulted array whose backing store is null.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task ExplicitEnumerators_YieldNothing_ForDefaultArray()
        {
            var defaulted = default(ImmutableEquatableArray<int>);

            // Both explicit interface paths fall back to an empty array for a null backing store, so neither advances.
            using var generic = ((IEnumerable<int>)defaulted).GetEnumerator();
            var nonGeneric = ((System.Collections.IEnumerable)defaulted).GetEnumerator();

            await Assert.That(generic.MoveNext()).IsFalse();
            await Assert.That(nonGeneric.MoveNext()).IsFalse();
        }

        /// <summary>Verifies the factory yields an empty (value-equal) result for empty input and wraps populated input without copying.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task FromArray_ReturnsEmptyForEmptyInputAndWrapsPopulatedInput()
        {
            const int ExpectedPopulatedCount = 3;
            const int FirstValue = 1;
            const int SecondValue = 2;
            const int ThirdValue = 3;

            var source = new[] { FirstValue, SecondValue, ThirdValue };
            var empty = ImmutableEquatableArrayFactory.FromArray<int>([]);
            var populated = ImmutableEquatableArrayFactory.FromArray(source);

            await Assert.That(empty.Count).IsEqualTo(0);
            await Assert.That(empty.Equals(ImmutableEquatableArray<int>.Empty)).IsTrue();
            await Assert.That(populated.Count).IsEqualTo(ExpectedPopulatedCount);
            await Assert.That(populated.AsArray()).IsSameReferenceAs(source);
        }

        /// <summary>Returns a null immutable array reference without making the call site a constant condition.</summary>
        /// <returns>A null array reference.</returns>
        private static ImmutableEquatableArray<int>? NullIntArray() => null;
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
                .Select(Fixture.GetMetadataReference);

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
