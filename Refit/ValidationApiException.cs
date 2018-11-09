using System;

namespace Refit
{
    [Serializable]
    public class ValidationApiException : ApiException
    {

        ValidationApiException(ApiException apiException) :
            base(apiException.RequestMessage, apiException.HttpMethod, apiException.StatusCode, apiException.ReasonPhrase, apiException.Headers, apiException.RefitSettings)
        {
        }
        public static ValidationApiException Create(ApiException exception)
        {
            return new ValidationApiException(exception);
        }

        public new ProblemDetails Content => GetContentAs<ProblemDetails>();

    }
}
