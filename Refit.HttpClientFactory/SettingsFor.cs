namespace Refit
{
    /// <summary>
    /// ISettingsFor
    /// </summary>
    public interface ISettingsFor
    {
        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        RefitSettings? Settings { get; }
    }

    /// <summary>
    /// SettingsFor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Refit.ISettingsFor" />
    /// <remarks>
    /// Initializes a new instance of the <see cref="SettingsFor{T}"/> class.
    /// </remarks>
    /// <param name="settings">The settings.</param>
    public class SettingsFor<T>(RefitSettings? settings) : ISettingsFor
    {
        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        public RefitSettings? Settings { get; } = settings;
    }
}
