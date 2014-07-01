namespace Refit.Tests.Support.Serialization
{
    public class SnakeCasePropertyNamesContractResolver : DeliminatorSeparatedPropertyNamesContractResolver
    {
        public SnakeCasePropertyNamesContractResolver() : base('_') { }
    }
}