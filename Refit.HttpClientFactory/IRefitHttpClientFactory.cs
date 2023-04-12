namespace Refit.HttpClientFactory;

public interface IRefitHttpClientFactory
{
    T CreateClient<T>(string? name);
}
