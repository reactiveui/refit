// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
extern alias GeneratorTooling;
extern alias AnalyzerTooling;

using AnalyzerIndex = AnalyzerTooling::System.Index;
using AnalyzerRange = AnalyzerTooling::System.Range;
using GeneratorIndex = GeneratorTooling::System.Index;
using GeneratorRange = GeneratorTooling::System.Range;

namespace Refit.Documentation.Tooling;

/// <summary>Checks each distinct shipped public polyfill identity.</summary>
internal static class PolyfillSample
{
    /// <summary>Exercises the generator and analyzer copies separately.</summary>
    internal static void Run()
    {
        RunGeneratorCopy();
        RunGeneratorRange();
        RunAnalyzerIndex();
        RunAnalyzerRange();
        CheckSeparateCopies();
    }

    /// <summary>Demonstrates every declared index and range method group in the generator.</summary>
    private static void RunGeneratorCopy()
    {
        const int sequenceLength = 4;
        GeneratorIndex first = new(1);
        GeneratorIndex last = new(1, fromEnd: true);
        GeneratorIndex converted = 1;
        Console.WriteLine(first.Value); // 1
        Console.WriteLine(last.IsFromEnd); // True
        Console.WriteLine(last.GetOffset(sequenceLength)); // 3
        Console.WriteLine(GeneratorIndex.Start.GetOffset(sequenceLength)); // 0
        Console.WriteLine(GeneratorIndex.End.GetOffset(sequenceLength)); // 4
        Console.WriteLine(first == converted); // True
        Console.WriteLine(first != last); // True
        Console.WriteLine(first.Equals(converted)); // True
        Console.WriteLine(first.Equals((object)converted)); // True
        Console.WriteLine(first.GetHashCode() == converted.GetHashCode()); // True
        Console.WriteLine(first.ToString());

        Check.Require(first.Value == 1 && !first.IsFromEnd && last.Value == 1 && last.IsFromEnd, "Generator Index properties retain both constructor arguments.");
        Check.Require(first.GetOffset(sequenceLength) == 1 && last.GetOffset(sequenceLength) == sequenceLength - 1, "Generator Index calculates offsets from either end.");
        Check.Require(GeneratorIndex.Start.GetOffset(sequenceLength) == 0 && GeneratorIndex.End.GetOffset(sequenceLength) == sequenceLength, "Generator Index exposes both sequence bounds.");
        Check.Require(first == converted && first != last && first.Equals(converted) && first.Equals((object)converted), "Generator Index conversion and equality preserve stored fields.");
        Check.Require(first.GetHashCode() == converted.GetHashCode() && first.ToString().Contains("Value = 1", StringComparison.Ordinal), "Generator Index record methods describe equal values.");
    }

    /// <summary>Checks the generator's range endpoints, factories and synthesized record methods.</summary>
    private static void RunGeneratorRange()
    {
        GeneratorIndex first = new(1);
        GeneratorIndex last = new(1, fromEnd: true);
        GeneratorRange middle = new(first, last);
        GeneratorRange same = new(first, last);
        Console.WriteLine(middle.Start.Value); // 1
        Console.WriteLine(middle.End.IsFromEnd); // True
        Console.WriteLine(GeneratorRange.All.Start == GeneratorIndex.Start); // True
        Console.WriteLine(GeneratorRange.All.End == GeneratorIndex.End); // True
        Console.WriteLine(GeneratorRange.StartAt(first).End == GeneratorIndex.End); // True
        Console.WriteLine(GeneratorRange.EndAt(last).Start == GeneratorIndex.Start); // True
        Console.WriteLine(middle == same); // True
        Console.WriteLine(middle != GeneratorRange.All); // True
        Console.WriteLine(middle.Equals(same)); // True
        Console.WriteLine(middle.Equals((object)same)); // True
        Console.WriteLine(middle.GetHashCode() == same.GetHashCode()); // True
        Console.WriteLine(middle.ToString());

        Check.Require(middle.Start == first && middle.End == last, "Generator Range retains both constructor arguments.");
        Check.Require(GeneratorRange.All.Start == GeneratorIndex.Start && GeneratorRange.All.End == GeneratorIndex.End, "Generator Range.All covers both sequence bounds.");
        Check.Require(GeneratorRange.StartAt(first) == new GeneratorRange(first, GeneratorIndex.End), "Generator Range.StartAt retains the supplied start.");
        Check.Require(GeneratorRange.EndAt(last) == new GeneratorRange(GeneratorIndex.Start, last), "Generator Range.EndAt retains the supplied end.");
        Check.Require(middle == same && middle != GeneratorRange.All && middle.Equals(same) && middle.Equals((object)same), "Generator Range equality compares both endpoints.");
        Check.Require(middle.GetHashCode() == same.GetHashCode() && middle.ToString().Contains("Range", StringComparison.Ordinal), "Generator Range record methods describe equal values.");
    }

    /// <summary>Checks the analyzer's distinct index type against the same contracts.</summary>
    private static void RunAnalyzerIndex()
    {
        const int sequenceLength = 4;
        AnalyzerIndex first = new(1);
        AnalyzerIndex last = new(1, fromEnd: true);
        AnalyzerIndex converted = 1;
        Check.Require(first.Value == 1 && last.IsFromEnd, "Analyzer Index properties must describe its position.");
        Check.Require(first.GetOffset(sequenceLength) == 1 && last.GetOffset(sequenceLength) == sequenceLength - 1, "Analyzer Index calculates offsets from either end.");
        Check.Require(AnalyzerIndex.Start.GetOffset(sequenceLength) == 0, "Analyzer Index.Start must describe the sequence start.");
        Check.Require(AnalyzerIndex.End.GetOffset(sequenceLength) == sequenceLength, "Analyzer Index.End must describe the sequence end.");
        Check.Require(first == converted && first != last && first.Equals(converted) && first.Equals((object)converted), "Analyzer Index equality must compare stored fields.");
        Check.Require(first.GetHashCode() == converted.GetHashCode() && first.ToString().Contains("Value = 1", StringComparison.Ordinal), "Analyzer Index record methods must describe equal values.");
    }

    /// <summary>Checks the analyzer's distinct range type against the same contracts.</summary>
    private static void RunAnalyzerRange()
    {
        AnalyzerIndex first = new(1);
        AnalyzerIndex last = new(1, fromEnd: true);
        AnalyzerRange middle = new(first, last);
        AnalyzerRange same = new(first, last);
        Check.Require(middle.Start == first && middle.End == last, "Analyzer Range must preserve supplied endpoints.");
        Check.Require(AnalyzerRange.All.Start == AnalyzerIndex.Start && AnalyzerRange.All.End == AnalyzerIndex.End, "Analyzer Range.All must use both sequence bounds.");
        Check.Require(AnalyzerRange.StartAt(first) == new AnalyzerRange(first, AnalyzerIndex.End), "Analyzer Range.StartAt retains the supplied start.");
        Check.Require(AnalyzerRange.EndAt(last) == new AnalyzerRange(AnalyzerIndex.Start, last), "Analyzer Range.EndAt retains the supplied end.");
        Check.Require(middle == same && middle != AnalyzerRange.All && middle.Equals(same) && middle.Equals((object)same), "Analyzer Range equality must compare endpoints.");
        Check.Require(middle.GetHashCode() == same.GetHashCode() && middle.ToString().Contains("Range", StringComparison.Ordinal), "Analyzer Range record methods must describe equal values.");
    }

    /// <summary>Checks retained negative values and object equality across the two shipped identities.</summary>
    private static void CheckSeparateCopies()
    {
        GeneratorIndex generator = new(-1, fromEnd: true);
        AnalyzerIndex analyzer = new(-1, fromEnd: true);
        Check.Require(generator.Value == -1, "The generator polyfill retains negative constructor values.");
        Check.Require(analyzer.Value == -1, "The analyzer polyfill retains negative constructor values.");
        Check.Require(!generator.Equals((object)analyzer), "Generator index equality rejects the analyzer's distinct type.");
        Check.Require(!analyzer.Equals((object)generator), "Analyzer index equality rejects the generator's distinct type.");
        Check.Require(!GeneratorRange.All.Equals((object)AnalyzerRange.All), "Generator range equality rejects the analyzer's distinct type.");
        Check.Require(!AnalyzerRange.All.Equals((object)GeneratorRange.All), "Analyzer range equality rejects the generator's distinct type.");
    }
}
