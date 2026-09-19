// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.RuntimeHelpers;

/// <summary>Supplies formatter attributes without reflecting over a parameter.</summary>
internal static class AttributeSample
{
    /// <summary>Checks exact-type lookup and the shared arrays retained by generated providers.</summary>
    internal static void Run()
    {
        QueryAttribute query = new() { Format = "D3" };
        object[] queryAttributes = [query];
        GeneratedSingleTypeParameterAttributeProvider single = new(typeof(QueryAttribute), queryAttributes);
        GeneratedParameterAttributeProvider multiple = new(new Dictionary<Type, object[]> { [typeof(QueryAttribute)] = queryAttributes, [typeof(AliasAsAttribute)] = [new AliasAsAttribute("page")], });
        string? formatted = GeneratedRequestRunner.FormatUrlParameter(new(), SampleValues.Count, single, typeof(int));
        object[] all = multiple.GetCustomAttributes(inherit: true);
        object[] exact = multiple.GetCustomAttributes(typeof(QueryAttribute), inherit: false);
        bool hasQuery = single.IsDefined(typeof(QueryAttribute), inherit: true);

        Check.Require(formatted == "012" && all.Length == 2 && ReferenceEquals(exact, queryAttributes) && hasQuery, "Generated attributes are visible to formatters.");
        Check.Require(ReferenceEquals(single.GetCustomAttributes(false), queryAttributes), "The single provider returns its original attributes.");
        Check.Require(ReferenceEquals(single.GetCustomAttributes(typeof(QueryAttribute), false), queryAttributes), "Typed queries share the original attributes.");
        Check.Require(multiple.IsDefined(typeof(AliasAsAttribute), false) && !multiple.IsDefined(typeof(Attribute), false), "Lookups do not search base attribute types.");
        Check.Require(ReferenceEquals(all, multiple.GetCustomAttributes(false)) && all.Contains(query), "The multi-type provider reuses its flattened array and retains the attribute objects.");
        Check.Require(
            multiple.GetCustomAttributes(typeof(Attribute), true).Length == 0 && !single.IsDefined(typeof(Attribute), true),
            "Inheritance flags do not turn exact-type queries into base-type searches.");
        Check.Require(GeneratedParameterAttributeProvider.Empty.GetCustomAttributes(false).Length == 0, "The empty provider supplies no attributes.");
        GeneratedSingleTypeParameterAttributeProvider emptyTyped = new(typeof(QueryAttribute), []);
        Check.Require(emptyTyped.IsDefined(typeof(QueryAttribute), false), "A configured type is defined even when its array is empty.");
        Check.Require(emptyTyped.GetCustomAttributes(typeof(Attribute), false).Length == 0, "Base attribute types are not searched.");
        bool rejectedEmptyQuery = false;
        try
        {
            _ = GeneratedRequestRunner.FormatUrlParameter(new(), SampleValues.Count, emptyTyped, typeof(int));
        }
        catch (IndexOutOfRangeException)
        {
            rejectedEmptyQuery = true;
        }

        Check.Require(rejectedEmptyQuery, "Current default formatting fails when QueryAttribute is defined with an empty array.");
        DefaultUrlParameterFormatter registered = new();
        registered.AddFormat<int>("D4");
        RefitSettings mapped = new();
        mapped.UrlParameterFormatterMap[typeof(int)] = registered;
        Check.Require(!GeneratedRequestRunner.UsesDefaultUrlParameterFormatting(mapped), "Registered per-type formatters disable static URL formatting.");
        Check.Require(
            GeneratedRequestRunner.FormatUrlParameter(mapped, SampleValues.Count, GeneratedParameterAttributeProvider.Empty, typeof(object)) == "0012",
            "Formatter map selection uses the runtime value type, even when its declared type differs.");
    }
}
