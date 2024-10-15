// using System.Reflection;
//
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Microsoft.CodeAnalysis.Operations;
//
// namespace Refit.Generator
// {
//     public class AttributeDataAccessor(WellKnownTypes symbolAccessor)
//     {
//         private const string NameOfOperatorName = "nameof";
//         private const char FullNameOfPrefix = '@';
//
//         public TAttribute AccessSingle<TAttribute>(ISymbol symbol)
//             where TAttribute : Attribute => AccessSingle<TAttribute, TAttribute>(symbol);
//
//         public TData AccessSingle<TAttribute, TData>(ISymbol symbol)
//             where TAttribute : Attribute
//             where TData : notnull => Access<TAttribute, TData>(symbol).Single();
//
//         public TAttribute? AccessFirstOrDefault<TAttribute>(ISymbol symbol)
//             where TAttribute : Attribute => Access<TAttribute, TAttribute>(symbol).FirstOrDefault();
//
//         public TData? AccessFirstOrDefault<TAttribute, TData>(ISymbol symbol)
//             where TAttribute : Attribute
//             where TData : notnull => Access<TAttribute, TData>(symbol).FirstOrDefault();
//
//         // public bool HasAttribute<TAttribute>(ISymbol symbol)
//         //     where TAttribute : Attribute => symbolAccessor.HasAttribute<TAttribute>(symbol);
//
//         public IEnumerable<TAttribute> Access<TAttribute>(ISymbol symbol)
//             where TAttribute : Attribute => Access<TAttribute, TAttribute>(symbol);
//
//         // public IEnumerable<TAttribute> TryAccess<TAttribute>(IEnumerable<AttributeData> data)
//         //     where TAttribute : Attribute => TryAccess<TAttribute, TAttribute>(data);
//
//         public IEnumerable<TData> Access<TAttribute, TData>(ISymbol symbol)
//             where TAttribute : Attribute
//             where TData : notnull
//         {
//             var attrDatas = symbolAccessor.GetAttributes<TAttribute>(symbol);
//             return Access<TAttribute, TData>(attrDatas);
//         }
//
//         // public IEnumerable<TData> TryAccess<TAttribute, TData>(IEnumerable<AttributeData> attributes)
//         //     where TAttribute : Attribute
//         //     where TData : notnull
//         // {
//         //     var attrDatas = symbolAccessor.TryGetAttributes<TAttribute>(attributes);
//         //     return attrDatas.Select(a => Access<TAttribute, TData>(a));
//         // }
//
//         /// <summary>
//         /// Reads the attribute data and sets it on a newly created instance of <see cref="TData"/>.
//         /// If <see cref="TAttribute"/> has n type parameters,
//         /// <see cref="TData"/> needs to have an accessible ctor with the parameters 0 to n-1 to be of type <see cref="ITypeSymbol"/>.
//         /// <see cref="TData"/> needs to have exactly the same constructors as <see cref="TAttribute"/> with additional type arguments.
//         /// </summary>
//         /// <param name="attributes">The attributes data.</param>
//         /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
//         /// <typeparam name="TData">The type of the data class. If no type parameters are involved, this is usually the same as <see cref="TAttribute"/>.</typeparam>
//         /// <returns>The attribute data.</returns>
//         /// <exception cref="InvalidOperationException">If a property or ctor argument of <see cref="TData"/> could not be read on the attribute.</exception>
//         public IEnumerable<TData> Access<TAttribute, TData>(IEnumerable<AttributeData> attributes)
//             where TAttribute : Attribute
//             where TData : notnull
//         {
//             foreach (var attrData in symbolAccessor.GetAttributes<TAttribute>(attributes))
//             {
//                 yield return Access<TAttribute, TData>(attrData, symbolAccessor);
//             }
//         }
//
//         internal static TData Access<TAttribute, TData>(AttributeData attrData, WellKnownTypes? symbolAccessor = null)
//             where TAttribute : Attribute
//             where TData : notnull
//         {
//             var attrType = typeof(TAttribute);
//             var dataType = typeof(TData);
//
//             var syntax = (AttributeSyntax?)attrData.ApplicationSyntaxReference?.GetSyntax();
//             var syntaxArguments =
//                 (IReadOnlyList<AttributeArgumentSyntax>?)syntax?.ArgumentList?.Arguments
//                 ?? new AttributeArgumentSyntax[attrData.ConstructorArguments.Length + attrData.NamedArguments.Length];
//             var typeArguments = (IReadOnlyCollection<ITypeSymbol>?)attrData.AttributeClass?.TypeArguments ?? [];
//             var attr = Create<TData>(typeArguments, attrData.ConstructorArguments, syntaxArguments, symbolAccessor);
//
//             var syntaxIndex = attrData.ConstructorArguments.Length;
//             var propertiesByName =
//                 dataType.GetProperties().GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First());
//             foreach (var namedArgument in attrData.NamedArguments)
//             {
//                 if (!propertiesByName.TryGetValue(namedArgument.Key, out var prop))
//                     throw new InvalidOperationException(
//                         $"Could not get property {namedArgument.Key} of attribute {attrType.FullName}");
//
//                 var value = BuildArgumentValue(namedArgument.Value, prop.PropertyType, syntaxArguments[syntaxIndex],
//                     symbolAccessor);
//                 prop.SetValue(attr, value);
//                 syntaxIndex++;
//             }
//
//             // if (attr is HasSyntaxReference symbolRefHolder)
//             // {
//             //     symbolRefHolder.SyntaxReference = attrData.ApplicationSyntaxReference?.GetSyntax();
//             // }
//
//             return attr;
//         }
//
//         private static TData Create<TData>(
//             IReadOnlyCollection<ITypeSymbol> typeArguments,
//             IReadOnlyCollection<TypedConstant> constructorArguments,
//             IReadOnlyList<AttributeArgumentSyntax> argumentSyntax,
//             WellKnownTypes? symbolAccessor
//         )
//             where TData : notnull
//         {
//             // The data class should have a constructor
//             // with generic type parameters of the attribute class
//             // as ITypeSymbol parameters followed by all other parameters
//             // of the attribute constructor.
//             // Multiple attribute class constructors/generic data classes are not yet supported.
//             var argCount = typeArguments.Count + constructorArguments.Count;
//             foreach (var constructor in typeof(TData).GetConstructors())
//             {
//                 var parameters = constructor.GetParameters();
//                 if (parameters.Length != argCount)
//                     continue;
//
//                 var constructorArgumentValues = constructorArguments.Select(
//                     (arg, i) => BuildArgumentValue(arg, parameters[i + typeArguments.Count].ParameterType,
//                         argumentSyntax[i], symbolAccessor)
//                 );
//                 var constructorTypeAndValueArguments = typeArguments.Concat(constructorArgumentValues).ToArray();
//                 if (!ValidateParameterTypes(constructorTypeAndValueArguments, parameters))
//                     continue;
//
//                 return (TData?)Activator.CreateInstance(typeof(TData), constructorTypeAndValueArguments)
//                        ?? throw new InvalidOperationException($"Could not create instance of {typeof(TData)}");
//             }
//
//             throw new InvalidOperationException(
//                 $"{typeof(TData)} does not have a constructor with {argCount} parameters and matchable arguments"
//             );
//         }
//
//         private static object? BuildArgumentValue(
//             TypedConstant arg,
//             Type targetType,
//             AttributeArgumentSyntax? syntax,
//             WellKnownTypes? symbolAccessor
//         )
//         {
//             return arg.Kind switch
//             {
//                 // _ when (targetType == typeof(AttributeValue?) || targetType == typeof(AttributeValue)) && syntax != null => new AttributeValue(
//                 //     arg,
//                 //     syntax.Expression
//                 // ),
//                 _ when arg.IsNull => null,
//                 // _ when targetType == typeof(IMemberPathConfiguration) => CreateMemberPath(arg, syntax, symbolAccessor),
//                 TypedConstantKind.Enum => GetEnumValue(arg, targetType),
//                 TypedConstantKind.Array => BuildArrayValue(arg, targetType, symbolAccessor),
//                 TypedConstantKind.Primitive => arg.Value,
//                 TypedConstantKind.Type when targetType == typeof(ITypeSymbol) => arg.Value,
//                 _ => throw new ArgumentOutOfRangeException(
//                     $"{nameof(AttributeDataAccessor)} does not support constructor arguments of kind {arg.Kind.ToString()} or cannot convert it to {targetType}"
//                 ),
//             };
//         }
//
//         private static object?[] BuildArrayValue(TypedConstant arg, Type targetType, WellKnownTypes? symbolAccessor)
//         {
//             if (!targetType.IsGenericType || targetType.GetGenericTypeDefinition() != typeof(IReadOnlyCollection<>))
//                 throw new InvalidOperationException(
//                     $"{nameof(IReadOnlyCollection<object>)} is the only supported array type");
//
//             var elementTargetType = targetType.GetGenericArguments()[0];
//             return arg.Values.Select(x => BuildArgumentValue(x, elementTargetType, null, symbolAccessor)).ToArray();
//         }
//
//         private static object? GetEnumValue(TypedConstant arg, Type targetType)
//         {
//             if (arg.Value == null)
//                 return null;
//
//             var enumRoslynType = arg.Type ?? throw new InvalidOperationException("Type is null");
//             if (targetType == typeof(IFieldSymbol))
//                 return enumRoslynType.GetMembers().Where(x => x.Kind == SymbolKind.Field)
//                     .First(f => Equals(f.ConstantValue, arg.Value));
//
//             if (targetType.IsConstructedGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
//             {
//                 targetType = Nullable.GetUnderlyingType(targetType)!;
//             }
//
//             return Enum.ToObject(targetType, arg.Value);
//         }
//
//         private static bool ValidateParameterTypes(object?[] arguments, ParameterInfo[] parameters)
//         {
//             if (arguments.Length != parameters.Length)
//                 return false;
//
//             for (var argIdx = 0; argIdx < arguments.Length; argIdx++)
//             {
//                 var value = arguments[argIdx];
//                 var param = parameters[argIdx];
//                 if (value == null && param.ParameterType.IsValueType)
//                     return false;
//
//                 // if (value?.GetType().IsAssignableTo(param.ParameterType) == false)
//                 //     return false;
//             }
//
//             return true;
//         }
//     }
// }
