using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Refit.Generator;

namespace Refit.Tests;

/// <summary>
/// Focused unit tests for the individual building blocks of the source generator,
/// exercised directly rather than through end-to-end snapshot generation.
/// </summary>
public class GeneratorComponentTests
{
    public class UniqueNameBuilderTests
    {
        [Test]
        public void New_ReturnsOriginalName_WhenUnused()
        {
            var builder = new UniqueNameBuilder();

            Assert.Equal("client", builder.New("client"));
        }

        [Test]
        public void New_AppendsSuffix_OnCollision()
        {
            var builder = new UniqueNameBuilder();

            Assert.Equal("client", builder.New("client"));
            Assert.Equal("client0", builder.New("client"));
            Assert.Equal("client1", builder.New("client"));
        }

        [Test]
        public void Reserve_PreventsName_FromBeingHandedOut()
        {
            var builder = new UniqueNameBuilder();
            builder.Reserve("client");

            Assert.Equal("client0", builder.New("client"));
        }

        [Test]
        public void Reserve_Enumerable_PreventsAllNames()
        {
            var builder = new UniqueNameBuilder();
            builder.Reserve(["client", "requestBuilder"]);

            Assert.Equal("client0", builder.New("client"));
            Assert.Equal("requestBuilder0", builder.New("requestBuilder"));
        }

        [Test]
        public void Reserve_NullEnumerable_DoesNotThrow()
        {
            var builder = new UniqueNameBuilder();

            builder.Reserve((IEnumerable<string>)null!);

            Assert.Equal("client", builder.New("client"));
        }

        [Test]
        public void NewScope_InheritsParentReservations()
        {
            var parent = new UniqueNameBuilder();
            parent.Reserve("client");

            var child = parent.NewScope();

            Assert.Equal("client0", child.New("client"));
        }

        [Test]
        public void NewScope_ParentDoesNotSeeChildNames()
        {
            var parent = new UniqueNameBuilder();
            var child = parent.NewScope();
            child.New("local");

            // The child's name must not leak back into the parent scope.
            Assert.Equal("local", parent.New("local"));
        }
    }

    public class SourceWriterTests
    {
        [Test]
        public void WriteLine_AppliesIndentation()
        {
            var writer = new SourceWriter { Indentation = 1 };
            writer.WriteLine("body");

            Assert.StartsWith("    body", writer.ToSourceText().ToString());
        }

        [Test]
        public void WriteLine_ZeroIndentation_HasNoLeadingWhitespace()
        {
            var writer = new SourceWriter();
            writer.WriteLine("body");

            Assert.StartsWith("body", writer.ToSourceText().ToString());
        }

        [Test]
        public void Indentation_Negative_Throws()
        {
            var writer = new SourceWriter();

            Assert.Throws<ArgumentOutOfRangeException>(() => writer.Indentation = -1);
        }

        [Test]
        public void Reset_ClearsContentAndIndentation()
        {
            var writer = new SourceWriter { Indentation = 2 };
            writer.WriteLine("first");

            writer.Reset();
            Assert.Equal(0, writer.Indentation);

            writer.WriteLine("second");
            var text = writer.ToSourceText().ToString();

            Assert.StartsWith("second", text);
            Assert.DoesNotContain("first", text.Split('\n'));
        }
    }

    public class ImmutableEquatableArrayTests
    {
        [Test]
        public void Equals_SameSequence_IsTrue()
        {
            var left = new ImmutableEquatableArray<string>(["a", "b", "c"]);
            var right = new ImmutableEquatableArray<string>(["a", "b", "c"]);

            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }

        [Test]
        public void Equals_DifferentSequence_IsFalse()
        {
            var left = new ImmutableEquatableArray<string>(["a", "b", "c"]);
            var right = new ImmutableEquatableArray<string>(["a", "x", "c"]);

            Assert.NotEqual(left, right);
        }

        [Test]
        public void Empty_HasNoElements()
        {
            Assert.Equal(0, ImmutableEquatableArray<string>.Empty.Count);
        }

        [Test]
        public void ToImmutableEquatableArray_Null_ReturnsEmpty()
        {
            IEnumerable<string>? source = null;

            var result = source.ToImmutableEquatableArray();

            Assert.Equal(0, result.Count);
        }

        [Test]
        public void Enumerator_YieldsAllValuesInOrder()
        {
            var array = new ImmutableEquatableArray<int>([10, 20, 30]);

            var collected = new List<int>(array.Count);
            foreach (var value in array)
            {
                collected.Add(value);
            }

            Assert.Equal([10, 20, 30], collected);
            Assert.Equal(20, array[1]);
        }
    }

    public class ITypeSymbolExtensionsTests
    {
        [Test]
        public void GetBaseTypesAndThis_ReturnsSelfThenBaseChain()
        {
            var compilation = Compile("""
                public class Base { }
                public class Middle : Base { }
                public class Derived : Middle { }
                """);
            var derived = GetType(compilation, "Derived");

            var chain = derived.GetBaseTypesAndThis().Select(t => t.Name).ToArray();

            Assert.Equal(["Derived", "Middle", "Base", "Object"], chain);
        }

        [Test]
        public void InheritsFromOrEquals_SameType_IsTrue()
        {
            var compilation = Compile("public class Derived { }");
            var derived = GetType(compilation, "Derived");

            Assert.True(derived.InheritsFromOrEquals(derived));
        }

        [Test]
        public void InheritsFromOrEquals_BaseType_IsTrue()
        {
            var compilation = Compile("""
                public class Base { }
                public class Derived : Base { }
                """);

            Assert.True(
                GetType(compilation, "Derived").InheritsFromOrEquals(GetType(compilation, "Base"))
            );
        }

        [Test]
        public void InheritsFromOrEquals_UnrelatedType_IsFalse()
        {
            var compilation = Compile("""
                public class Foo { }
                public class Bar { }
                """);

            Assert.False(
                GetType(compilation, "Foo").InheritsFromOrEquals(GetType(compilation, "Bar"))
            );
        }

        [Test]
        public void InheritsFromOrEquals_Interface_HonorsIncludeInterfacesFlag()
        {
            var compilation = Compile("""
                public interface IThing { }
                public class Thing : IThing { }
                """);
            var thing = GetType(compilation, "Thing");
            var iThing = GetType(compilation, "IThing");

            Assert.False(thing.InheritsFromOrEquals(iThing, includeInterfaces: false));
            Assert.True(thing.InheritsFromOrEquals(iThing, includeInterfaces: true));
        }

        static CSharpCompilation Compile(string source)
        {
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));

            return CSharpCompilation.Create(
                "TypeSymbolTests",
                [CSharpSyntaxTree.ParseText(source)],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );
        }

        static INamedTypeSymbol GetType(Compilation compilation, string typeName) =>
            compilation.GetTypeByMetadataName(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' was not found.");
    }
}
