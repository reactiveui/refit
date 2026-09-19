// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Reflection;

namespace Refit.Documentation.RuntimeHelpers;

/// <summary>Inspects reflected metadata; this demonstration does not claim native AOT compatibility.</summary>
internal static class MetadataSample
{
    /// <summary>Checks metadata constructors, mutable properties, record members, and generated client names.</summary>
    internal static void Run()
    {
        MethodInfo method = typeof(IHelperApi).GetMethod(nameof(IHelperApi.GetAsync))!;
        RestMethodInfo info = new(method.Name, typeof(IHelperApi), method, "/items/{id}", method.ReturnType);
        RestMethodInfo alternate = info with { RelativePath = "/other/{id}" };
        (string name, Type hostingType, MethodInfo reflected, string relativePath, Type returnType) = info;
        ParameterInfo parameter = method.GetParameters()[0];
        RestMethodParameterInfo named = new("id", parameter) { Type = ParameterType.Normal };
        RestMethodParameterInfo objectParameter = new(true, parameter) { Name = "body", Type = ParameterType.RoundTripping };
        PropertyInfo count = typeof(FormBody).GetProperty(nameof(FormBody.Count))!;
        RestMethodParameterProperty direct = new(nameof(count), count);
        RestMethodParameterProperty chained = new("body.count", new[] { count });
        objectParameter.ParameterProperties.Add(chained);

        Check.Require(info.Name == name, "The record deconstructs its name.");
        Check.Require(info.HostingType == hostingType, "The record deconstructs its declaring type.");
        Check.Require(info.MethodInfo == reflected, "The record deconstructs its method.");
        Check.Require(info.RelativePath == relativePath, "The record deconstructs its path.");
        Check.Require(info.ReturnType == returnType, "The record deconstructs its return type.");
        Check.Require(
            name == method.Name && hostingType == typeof(IHelperApi) && reflected == method && relativePath == "/items/{id}" && returnType == method.ReturnType,
            "Construction and deconstruction retain the supplied metadata.");
        Check.Require(alternate != info, "Changed records compare unequal.");
        Check.Require(info.Equals(info), "Typed equality compares the stored values.");
        Check.Require(info.Equals((object)info), "Object equality compares the stored values.");
        Check.Require(info == info with { }, "Copying a record preserves equality.");
        Check.Require(info.GetHashCode() == (info with { }).GetHashCode(), "Equal records have equal hashes.");
        Check.Require(info.ToString().Contains(nameof(RestMethodInfo), StringComparison.Ordinal), "Records render their metadata.");
        Check.Require(named.Name == "id", "The named constructor assigns its name.");
        Check.Require(named.ParameterInfo == parameter, "The named constructor retains reflected metadata.");
        Check.Require(!named.IsObjectPropertyParameter, "Named parameters default to a direct binding.");
        Check.Require(objectParameter.IsObjectPropertyParameter, "The boolean constructor retains its object flag.");
        Check.Require(objectParameter.Type == ParameterType.RoundTripping, "The parameter type can be assigned.");
        Check.Require(objectParameter.ParameterProperties.Count == 1, "The navigation list is mutable.");
        named.Name = "changed";
        ParameterInfo otherParameter = typeof(GeneratedRequestRunner).GetMethod(nameof(GeneratedRequestRunner.RequireAbsoluteUrl))!.GetParameters()[0];
        PropertyInfo note = typeof(FormBody).GetProperty(nameof(FormBody.Note))!;
        named.ParameterInfo = otherParameter;
        named.IsObjectPropertyParameter = true;
        direct.Name = "changed";
        direct.PropertyInfo = note;
        PropertyInfo[] replacementChain = [note];
        direct.PropertyChain = replacementChain;
        Check.Require(chained.PropertyInfo == count, "The chain constructor assigns its final property.");
        Check.Require(chained.PropertyChain.Count == 1, "Single properties retain one-element chains.");
        Check.Require(direct.Name == named.Name, "Metadata names can be changed.");
        Check.Require(named.ParameterInfo == otherParameter && named.IsObjectPropertyParameter, "Parameter metadata and binding flags can be changed.");
        Check.Require(direct.PropertyInfo == note && ReferenceEquals(direct.PropertyChain, replacementChain), "Property metadata setters retain their independently supplied values.");
        RestMethodParameterInfo initialized = new(false, parameter) { ParameterProperties = [direct] };
        Check.Require(initialized.Name is null, "The boolean constructor leaves the name unset.");
        Check.Require(initialized.Type == ParameterType.Normal, "Normal escaping is the default.");
        Check.Require(initialized.ParameterProperties.Count == 1, "The navigation list supports initialization.");
        Check.Require(ReferenceEquals(initialized.ParameterProperties[0], direct), "Initialized navigation lists retain the supplied descriptor.");

        CheckGeneratedNames(method.DeclaringType!);
        CheckRecordInitialization(info);
        CheckNestedChain(note);
    }

    /// <summary>Checks all unique-name overloads against the actual emitted client identity.</summary>
    /// <param name="interfaceType">The reflected interface type.</param>
    private static void CheckGeneratedNames(Type interfaceType)
    {
        string generatedName = UniqueName.ForType<IHelperApi>();
        string runtimeName = UniqueName.ForType(interfaceType);
        string keyedName = UniqueName.ForType<IHelperApi>("primary");
        string runtimeKeyedName = UniqueName.ForType(interfaceType, "primary");

        Check.Require(generatedName == runtimeName, "Generic and runtime-Type names agree.");
        Type? generatedType = Type.GetType(generatedName);
        Check.Require(generatedType is not null && typeof(IHelperApi).IsAssignableFrom(generatedType), "The generated name resolves to the emitted implementation of the interface.");
        Check.Require(keyedName == runtimeKeyedName, "Generic and runtime-Type keyed names agree.");
        Check.Require(keyedName == $"{generatedName}, ServiceKey=primary", "Keys add their string representation.");
        Check.Require(UniqueName.ForType<IHelperApi>(null) == generatedName, "Null keys add no suffix.");
        Check.Require(UniqueName.ForType<IHelperApi>(string.Empty) == generatedName, "Empty keys add no suffix.");
    }

    /// <summary>Checks every record init setter with independently changed metadata.</summary>
    /// <param name="original">The original interface method descriptor.</param>
    private static void CheckRecordInitialization(RestMethodInfo original)
    {
        MethodInfo replacementMethod = typeof(GeneratedRequestRunner).GetMethod(nameof(GeneratedRequestRunner.RequireAbsoluteUrl))!;
        RestMethodInfo replacement = original with
        {
            Name = replacementMethod.Name,
            HostingType = typeof(GeneratedRequestRunner),
            MethodInfo = replacementMethod,
            RelativePath = "/url",
            ReturnType = replacementMethod.ReturnType,
        };
        Check.Require(replacement.Name == replacementMethod.Name && replacement.HostingType == typeof(GeneratedRequestRunner), "Record init setters replace the name and hosting type.");
        Check.Require(
            replacement.MethodInfo == replacementMethod && replacement.RelativePath == "/url" && replacement.ReturnType == typeof(string),
            "Record init setters replace the method, path and return type.");
        Check.Require(!original.Equals(replacement) && !original.Equals((object)replacement), "Both record equality methods observe changed values.");
    }

    /// <summary>Checks that nested navigation retains its supplied chain and final property.</summary>
    /// <param name="note">The first link in the two-property navigation.</param>
    private static void CheckNestedChain(PropertyInfo note)
    {
        PropertyInfo length = typeof(string).GetProperty(nameof(string.Length))!;
        PropertyInfo[] navigation = [note, length];
        RestMethodParameterProperty nested = new("body.note.length", navigation);
        Check.Require(ReferenceEquals(nested.PropertyChain, navigation), "Nested chain construction retains the supplied list.");
        Check.Require(nested.PropertyInfo == length, "Nested chain construction selects the final property, rather than the first.");
    }
}
