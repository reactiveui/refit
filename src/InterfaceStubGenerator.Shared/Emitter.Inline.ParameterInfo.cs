// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Emits the cached parameter attribute-provider fields and the parameters argument for the inline path.</summary>
internal static partial class Emitter
{
    /// <summary>Builds the unique cached attribute-provider field name for a path parameter.</summary>
    /// <param name="parameterName">The source parameter name.</param>
    /// <param name="uniqueNames">The unique member name builder for the interface scope.</param>
    /// <returns>The unique generated field name.</returns>
    internal static string GetParameterInfoFieldName(string parameterName, UniqueNameBuilder uniqueNames) =>
        uniqueNames.New($"______{parameterName}AttributeProvider");

    /// <summary>Appends a separator before all but the first element.</summary>
    /// <param name="i">The zero-based element index.</param>
    /// <param name="sb">The target builder.</param>
    /// <param name="separator">The separator to append.</param>
    /// <returns>The same builder for chaining.</returns>
    internal static PooledStringBuilder AppendSeparator(int i, PooledStringBuilder sb, string separator = ", ")
    {
        return i <= 0 ? sb : sb.Append(separator);
    }

    /// <summary>Appends a value, prefixed by a separator for all but the first element.</summary>
    /// <param name="value">The value to append.</param>
    /// <param name="i">The zero-based element index.</param>
    /// <param name="sb">The target builder.</param>
    /// <param name="separator">The separator to append before the value.</param>
    /// <returns>The same builder for chaining.</returns>
    internal static PooledStringBuilder AppendJoining(string value, int i, PooledStringBuilder sb, string separator = ", ")
    {
        return AppendSeparator(i, sb, separator).Append(value);
    }

    /// <summary>Appends a C# attribute construction expression to the builder.</summary>
    /// <param name="attribute">The attribute model to render.</param>
    /// <param name="sb0">The target builder.</param>
    internal static void AppendAttributeValue(ParameterAttributeModel attribute, PooledStringBuilder sb0)
    {
        _ = sb0.Append("new ").Append(attribute.TypeExpression).Append('(');
        var i = 0;

        foreach (var argument in attribute.ConstructorArguments)
        {
            _ = AppendJoining(argument, i, sb0);
            i++;
        }

        _ = sb0.Append(')');
        if (attribute.NamedArguments.Count < 1)
        {
            return;
        }

        i = 0;
        _ = sb0.Append("{ ");
        foreach (var named in attribute.NamedArguments)
        {
            _ = AppendSeparator(i, sb0);
            i++;
            _ = sb0.Append(named.Name).Append(" = ").Append(named.ValueExpression);
        }

        _ = sb0.Append(" }");
    }

    /// <summary>Emits the cached attribute-provider field for a single path parameter.</summary>
    /// <param name="parameter">The path parameter model.</param>
    /// <param name="method">The declaring method name, used for the generated documentation.</param>
    /// <param name="paramInfoFieldName">The unique generated field name.</param>
    /// <param name="sb">The target builder.</param>
    internal static void BuildParameterInfoField(RequestParameterModel parameter, string method, string paramInfoFieldName, PooledStringBuilder sb)
    {
        // Build the initializer.
        var memberIndent = Indent(MethodMemberIndentation);
        Dictionary<string, List<ParameterAttributeModel>> grouped = new();

        foreach (var attribute in parameter.Attributes)
        {
            var key = $"typeof({attribute.TypeExpression})";
            if (grouped.TryGetValue(key, out var groupedAttributes))
            {
                groupedAttributes.Add(attribute);
            }
            else
            {
                grouped.Add(key, [attribute]);
            }
        }

        _ = sb.AppendLine().Append(memberIndent).Append("/// <summary>Cached attribute provider for the generated ")
            .Append(ToXmlDocumentationText(method)).Append(" method's ").Append(ToXmlDocumentationText(parameter.Name)).AppendLine(" parameter.</summary>")
            .Append(memberIndent).Append("private static readonly global::Refit.GeneratedParameterAttributeProvider ").Append(paramInfoFieldName).Append(" = ");

        // A parameter with no attributes shares the singleton empty provider instead of allocating an empty dictionary.
        if (grouped.Count == 0)
        {
            _ = sb.AppendLine("global::Refit.GeneratedParameterAttributeProvider.Empty;");
            return;
        }

        const string dictType = "global::System.Collections.Generic.Dictionary<global::System.Type, object[]>";
        _ = sb.Append("new global::Refit.GeneratedParameterAttributeProvider(new ").Append(dictType).Append("() {");
        var i = 0;
        foreach (var kv in grouped)
        {
            _ = AppendJoining("{ ", i, sb).Append(kv.Key).Append(", new object[] { ");
            i++;
            var argIndex = 0;
            foreach (var arg in kv.Value)
            {
                // Multiple attributes of the same type must be comma-separated inside the array.
                _ = AppendSeparator(argIndex, sb);
                argIndex++;
                AppendAttributeValue(arg, sb);
            }

            _ = sb.Append("} }");
        }

        _ = sb.Append('}').AppendLine(");");
    }

    /// <summary>Assigns the unique cached field name for each attribute-provider parameter and emits its field.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="uniqueNames">The unique member name builder for the interface scope.</param>
    /// <param name="declaredMethod">The declared method name the emitted fields are scoped to.</param>
    /// <param name="paramInfoSb">The builder receiving the emitted attribute-provider fields.</param>
    /// <returns>A map of parameter name to its cached attribute-provider field name.</returns>
    internal static Dictionary<string, string> BuildParameterInfoFields(
        RequestModel request,
        UniqueNameBuilder uniqueNames,
        string declaredMethod,
        PooledStringBuilder paramInfoSb)
    {
        var dict = new Dictionary<string, string>();
        foreach (var parameter in request.Parameters)
        {
            if (!NeedsAttributeProvider(parameter))
            {
                continue;
            }

            var parameterInfoFieldName = GetParameterInfoFieldName(parameter.Name, uniqueNames);
            dict.Add(parameter.Name, parameterInfoFieldName);
            BuildParameterInfoField(parameter, declaredMethod, parameterInfoFieldName, paramInfoSb);
        }

        return dict;
    }

    /// <summary>Builds the additional arguments passed to the generated request path builder.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="uniqueNameLookup">The map of parameter name to cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The generated argument list fragment.</returns>
    internal static string GetParametersArg(
        RequestModel request,
        Dictionary<string, string> uniqueNameLookup,
        in InlineValueEmission emission)
    {
        // A single pre-encoded path parameter switches every replacement to the overload carrying the
        // per-value encoding flag, because a params call cannot mix tuple arities.
        var anyPreEncoded = HasPreEncodedPathParameter(request);

        var replacements = CollectPathReplacements(request, uniqueNameLookup, emission);
        if (replacements.Count == 0)
        {
            return string.Empty;
        }

        // BuildRequestPath fills the template left-to-right and slices between consecutive replacements, so they must
        // be ordered by template position. Parameter order does not match template order when an object binding (or a
        // later parameter) fills an earlier placeholder, so sort here rather than relying on declaration order.
        replacements.Sort(static (left, right) => left.Start.CompareTo(right.Start));

        var parametersSb = new PooledStringBuilder();
        var first = true;
        foreach (var replacement in replacements)
        {
            if (!first)
            {
                _ = parametersSb.Append(", ");
            }

            first = false;
            AppendPathTuple(
                parametersSb,
                replacement.Start,
                replacement.End,
                replacement.Value,
                anyPreEncoded,
                replacement.PreEncoded);
        }

        return WrapPathReplacements(parametersSb.ToString(), emission.SupportsCollectionExpressions);
    }

    /// <summary>Collects the path-template replacements contributed by every path parameter.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="uniqueNameLookup">The map of parameter name to cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The unordered path-template replacements.</returns>
    internal static List<PathReplacement> CollectPathReplacements(
        RequestModel request,
        Dictionary<string, string> uniqueNameLookup,
        in InlineValueEmission emission)
    {
        var pathLength = request.Path.Length;
        var replacements = new List<PathReplacement>();
        foreach (var parameter in request.Parameters)
        {
            if (parameter.Kind is not RequestParameterKind.Path)
            {
                continue;
            }

            var providerField = uniqueNameLookup[parameter.Name];

            // A dotted {param.Prop} object parameter fills each placeholder with a formatted property value.
            if (parameter.PathObjectBindings is { } bindings)
            {
                foreach (var binding in bindings)
                {
                    var bindingValue = BuildPathValueExpressionCore(
                        "@" + parameter.Name + "." + binding.PropertyClrName,
                        binding.PropertyType,
                        binding.ValueFormat,
                        binding.PropertyCanBeNull,
                        providerField,
                        emission);
                    replacements.Add(new(
                        binding.Location.Start.GetOffset(pathLength),
                        binding.Location.End.GetOffset(pathLength),
                        bindingValue,
                        PreEncoded: false));
                }

                continue;
            }

            // Every remaining Path parameter is a direct placeholder built with locations (a dotted object binding is
            // handled above), so its locations are always present.
            var valueExpression = BuildPathValueExpression(parameter, providerField, emission);
            foreach (var location in parameter.Locations!)
            {
                replacements.Add(new(
                    location.Start.GetOffset(pathLength),
                    location.End.GetOffset(pathLength),
                    valueExpression,
                    parameter.PreEncoded));
            }
        }

        return replacements;
    }
}
