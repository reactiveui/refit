using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Refit
{
    /// <summary>
    /// An ApiException that is raised according to RFC 7807, which contains problem details for validation exceptions.
    /// </summary>
    [Serializable]
    public class ValidationApiException : ApiException
    {
        static readonly JsonSerializerOptions SerializerOptions = new();

        static ValidationApiException()
        {
            SerializerOptions.PropertyNameCaseInsensitive = true;
            SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            SerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
        }

        ValidationApiException(ApiException apiException)
            : base(
                apiException.RequestMessage,
                apiException.HttpMethod,
                apiException.Content,
                apiException.StatusCode,
                apiException.ReasonPhrase,
                apiException.Headers,
                apiException.RefitSettings
            )
        {
            // Carry over the content headers from the originating ApiException so callers
            // can inspect them on the validation exception too (#1945).
            ContentHeaders = apiException.ContentHeaders;
        }

        /// <summary>
        /// Creates a new instance of a ValidationException from an existing ApiException.
        /// </summary>
        /// <param name="exception">An instance of an ApiException to use to build a ValidationException.</param>
        /// <returns>ValidationApiException</returns>
        public static ValidationApiException Create(ApiException exception)
        {
            var ex = CreateCore(exception);
            ex.Content = JsonSerializer.Deserialize<ProblemDetails>(
                exception.Content!,
                SerializerOptions
            );
            return ex;
        }

        /// <summary>
        /// Creates a new instance of a ValidationException from an existing ApiException,
        /// deserializing the problem details with the serializer configured in
        /// <see cref="RefitSettings"/>.
        /// </summary>
        /// <param name="exception">An instance of an ApiException to use to build a ValidationException.</param>
        /// <returns>ValidationApiException</returns>
        internal static async Task<ValidationApiException> CreateAsync(ApiException exception)
        {
            var ex = CreateCore(exception);

            // Deserialize through the configured IHttpContentSerializer rather than a
            // hardcoded System.Text.Json instance, so problem details honor the user's
            // serializer (e.g. Newtonsoft) and its settings (#1197).
            using var content = new StringContent(
                exception.Content!,
                Encoding.UTF8,
                "application/problem+json"
            );
            ex.Content = await exception
                .RefitSettings.ContentSerializer.FromHttpContentAsync<ProblemDetails>(content)
                .ConfigureAwait(false);

            return ex;
        }

        static ValidationApiException CreateCore(ApiException exception)
        {
            if (exception is null)
                throw new ArgumentNullException(nameof(exception));
            if (string.IsNullOrWhiteSpace(exception.Content))
                throw new ArgumentException(
                    "Content must be an 'application/problem+json' compliant json string."
                );

            return new ValidationApiException(exception);
        }

        /// <summary>
        /// The problem details of the RFC 7807 validation exception.
        /// </summary>
        public new ProblemDetails? Content { get; private set; }
    }
}
