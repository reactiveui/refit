using System.Reflection;

namespace Refit
{
    public interface IRefitSettings
    {
        IRequestParameterFormatter RequestParameterFormatter { get; set; }
    }

    public class DefaultRefitSettings : IRefitSettings
    {
        public DefaultRefitSettings()
        {
            RequestParameterFormatter = new DefaultRequestParameterFormatter();
        }

        public IRequestParameterFormatter RequestParameterFormatter { get; set; }
    }

    public interface IRequestParameterFormatter
    {
        string Format(object value, ParameterInfo parameterInfo);
    }

    public class DefaultRequestParameterFormatter : IRequestParameterFormatter
    {
        public virtual string Format(object parameterValue, ParameterInfo parameterInfo)
        {
            return parameterValue.ToString();
        }
    }
}