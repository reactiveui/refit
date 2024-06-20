using System.Net.Http;
using System.Reflection;

namespace Refit
{
    /// <summary>
    /// JsonContentSerializer.
    /// </summary>
    /// <seealso cref="Refit.IHttpContentSerializer" />
    [Obsolete(
        "Use NewtonsoftJsonContentSerializer in the Refit.Newtonsoft.Json package instead",
        true
    )]
    public class JsonContentSerializer : IHttpContentSerializer
    {
        /// <summary>
        /// Converts to httpcontent.
        /// </summary>
        /// <typeparam name="T">Type of the object to serialize from.</typeparam>
        /// <param name="item">Object to serialize.</param>
        /// <returns>
        ///   <see cref="HttpContent" /> that contains the serialized <typeparamref name="T" /> object.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public HttpContent ToHttpContent<T>(T item) => throw new NotImplementedException();

        /// <summary>
        /// Deserializes an object of type <typeparamref name="T" /> from an <see cref="HttpContent" /> object.
        /// </summary>
        /// <typeparam name="T">Type of the object to serialize to.</typeparam>
        /// <param name="content">HttpContent object to deserialize.</param>
        /// <param name="cancellationToken">CancellationToken to abort the deserialization.</param>
        /// <returns>
        /// The deserialized object of type <typeparamref name="T" />.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<T?> FromHttpContentAsync<T>(
            HttpContent content,
            CancellationToken cancellationToken = default
        ) => throw new NotImplementedException();

        /// <summary>
        /// Calculates what the field name should be for the given property. This may be affected by custom attributes the serializer understands
        /// </summary>
        /// <param name="propertyInfo">A PropertyInfo object.</param>
        /// <returns>
        /// The calculated field name.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public string GetFieldNameForProperty(PropertyInfo propertyInfo) =>
            throw new NotImplementedException();
    }
}
