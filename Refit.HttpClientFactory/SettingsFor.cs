namespace Refit
{
    public interface ISettingsFor
    {
        RefitSettings? Settings { get;  }
    }

    public class SettingsFor<T> : ISettingsFor
    {
        public SettingsFor(RefitSettings? settings) => Settings = settings;
        public RefitSettings? Settings { get;  }
    }
}
