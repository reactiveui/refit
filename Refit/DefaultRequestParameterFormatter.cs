using System;
using System.Linq;
using System.Reflection;

namespace Refit
{
    public interface IRequestParameterFormatter
    {
        string Format(object value, ParameterInfo parameterInfo);
    }

    public class DefaultRequestParameterFormatter : IRequestParameterFormatter
    {
        protected Type InterfaceType { get; private set; }

        public virtual string Format(object value, ParameterInfo parameterInfo)
        {
            var formatAttribute = parameterInfo.GetCustomAttributes(true).OfType<FormatAttribute>()
                                               .FirstOrDefault();
            var formattable = value as IFormattable;

            if (formattable != null && formatAttribute != null)
            {
                return Format(formattable, formatAttribute.Format, formatAttribute.FormatProvider);
            }

            return value.ToString();
        }

        protected virtual string Format(IFormattable value, string format, IFormatProvider formatProvider)
        {
            return value.ToString(format, formatProvider);
        }
    }
}