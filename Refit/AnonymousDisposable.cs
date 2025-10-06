namespace Refit
{
    sealed class AnonymousDisposable(Action block) : IDisposable
    {
        public void Dispose()
        {
            block();
        }
    }
}
