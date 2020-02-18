namespace Refit
{
    /// <summary>
    /// Collection format defined in https://swagger.io/docs/specification/2-0/describing-parameters/ 
    /// </summary>
    public enum CollectionFormat
    {
        /// <summary>
        /// Values formatted with <see cref="RefitSettings.UrlParameterFormatter"/> or
        /// <see cref="RefitSettings.FormUrlEncodedParameterFormatter"/>.
        /// </summary>
        RefitParameterFormatter,

        /// <summary>
        /// Comma-separated values
        /// </summary>
        Csv,

        /// <summary>
        /// Space-separated values
        /// </summary>
        Ssv,

        /// <summary>
        /// Tab-separated values
        /// </summary>
        Tsv,

        /// <summary>
        /// Pipe-separated values
        /// </summary>
        Pipes,

        /// <summary>
        /// Multiple parameter instances
        /// </summary>
        Multi
    }
}
