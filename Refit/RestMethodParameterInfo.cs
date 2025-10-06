using System.Reflection;

namespace Refit
{
    /// <summary>
    /// RestMethodParameterInfo.
    /// </summary>
    public class RestMethodParameterInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestMethodParameterInfo"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="parameterInfo">The parameter information.</param>
        public RestMethodParameterInfo(string name, ParameterInfo parameterInfo)
        {
            Name = name;
            ParameterInfo = parameterInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestMethodParameterInfo"/> class.
        /// </summary>
        /// <param name="isObjectPropertyParameter">if set to <c>true</c> [is object property parameter].</param>
        /// <param name="parameterInfo">The parameter information.</param>
        public RestMethodParameterInfo(bool isObjectPropertyParameter, ParameterInfo parameterInfo)
        {
            IsObjectPropertyParameter = isObjectPropertyParameter;
            ParameterInfo = parameterInfo;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the parameter information.
        /// </summary>
        /// <value>
        /// The parameter information.
        /// </value>
        public ParameterInfo ParameterInfo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is object property parameter.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is object property parameter; otherwise, <c>false</c>.
        /// </value>
        public bool IsObjectPropertyParameter { get; set; }

        /// <summary>
        /// Gets or sets the parameter properties.
        /// </summary>
        /// <value>
        /// The parameter properties.
        /// </value>
        public List<RestMethodParameterProperty> ParameterProperties { get; set; } = [];

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public ParameterType Type { get; set; } = ParameterType.Normal;
    }

    /// <summary>
    /// RestMethodParameterProperty.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="RestMethodParameterProperty"/> class.
    /// </remarks>
    /// <param name="name">The name.</param>
    /// <param name="propertyInfo">The property information.</param>
    public class RestMethodParameterProperty(string name, PropertyInfo propertyInfo)
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; } = name;

        /// <summary>
        /// Gets or sets the property information.
        /// </summary>
        /// <value>
        /// The property information.
        /// </value>
        public PropertyInfo PropertyInfo { get; set; } = propertyInfo;
    }

    /// <summary>
    /// ParameterType.
    /// </summary>
    public enum ParameterType
    {
        /// <summary>
        /// The normal
        /// </summary>
        Normal,

        /// <summary>
        /// The round tripping
        /// </summary>
        RoundTripping
    }
}
