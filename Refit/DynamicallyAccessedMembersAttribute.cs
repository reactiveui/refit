#if NETSTANDARD2_0 || NET462
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Indicates that certain members on a specified <see cref="Type"/> are accessed dynamically,
/// for example through <see cref="Reflection"/>.
/// </summary>
/// <remarks>
/// This allows tools to understand which members are being accessed during the execution
/// of a program.
///
/// This attribute is valid on members whose type is <see cref="Type"/> or <see cref="string"/>.
///
/// When this attribute is applied to a location of type <see cref="string"/>, the assumption is
/// that the string represents a fully qualified type name.
///
/// When this attribute is applied to a class, interface, or struct, the members specified
/// can be accessed dynamically on <see cref="Type"/> instances returned from calling
/// <see cref="object.GetType"/> on instances of that class, interface, or struct.
///
/// If the attribute is applied to a method it's treated as a special case and it implies
/// the attribute should be applied to the "this" parameter of the method. As such the attribute
/// should only be used on instance methods of types assignable to System.Type (or string, but no methods
/// will use it there).
/// </remarks>
internal sealed class DynamicallyAccessedMembersAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicallyAccessedMembersAttribute"/> class
    /// with the specified member types.
    /// </summary>
    /// <param name="memberTypes">The types of members dynamically accessed.</param>
    public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
    {
        MemberTypes = memberTypes;
    }

    /// <summary>
    /// Gets the <see cref="DynamicallyAccessedMemberTypes"/> which specifies the type
    /// of members dynamically accessed.
    /// </summary>
    public DynamicallyAccessedMemberTypes MemberTypes { get; }
}

[Flags]
internal enum DynamicallyAccessedMemberTypes
{
    None = 0,
    PublicParameterlessConstructor = 1,
    PublicConstructors = 3,
    NonPublicConstructors = 4,
    PublicMethods = 8,
    NonPublicMethods = 16, // 0x00000010
    PublicFields = 32, // 0x00000020
    NonPublicFields = 64, // 0x00000040
    PublicNestedTypes = 128, // 0x00000080
    NonPublicNestedTypes = 256, // 0x00000100
    PublicProperties = 512, // 0x00000200
    NonPublicProperties = 1024, // 0x00000400
    PublicEvents = 2048, // 0x00000800
    NonPublicEvents = 4096, // 0x00001000
    Interfaces = 8192, // 0x00002000
    All = -1, // 0xFFFFFFFF
}
#endif
