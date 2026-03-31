namespace Refit.Tests
{
    public interface IBaseApi
    {
        string GetBaseUri();
    }

    public interface IMyRefitApi : IBaseApi
    {
        [Get("/users")]
        Task<List<string>> GetUsers();
    }
}
