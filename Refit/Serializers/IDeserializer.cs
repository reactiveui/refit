namespace Refit
{
    public interface IDeserializer
    {
        T Deserialize<T>(string objectToDeserialize);
    }
}
