using System.Net;
using System.Net.Http;

namespace Refit
{
    public class WindowsAuthenticationRefitSettings : RefitSettings
    {
        public WindowsAuthenticationRefitSettings()
        {
            SetCredentials(CredentialCache.DefaultCredentials);
        }

        public WindowsAuthenticationRefitSettings(string username, string password)
        {
            SetCredentials(new NetworkCredential(username, password));
        }

        public WindowsAuthenticationRefitSettings(string username, string password, string domain)
        {
            SetCredentials(new NetworkCredential(username, password, domain));
        }

        public WindowsAuthenticationRefitSettings(ICredentials credentials)
        {
            SetCredentials(credentials);
        }

        private void SetCredentials(ICredentials credentials)
        {
            HttpMessageHandlerFactory = () => new HttpClientHandler() { Credentials = credentials };
        }
    }
}