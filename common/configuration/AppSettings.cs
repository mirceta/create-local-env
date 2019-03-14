using System.ComponentModel;
using System.Configuration;

namespace si.birokrat.next.common.configuration {
    public static class AppSettings {
        public static string Get(string key) {
            return Get<string>(key);
        }

        public static T Get<T>(string key) {
            var value = ConfigurationManager.AppSettings.Get(key);
            if (value == null) {
                throw new SettingsPropertyNotFoundException(key);
            }

            return (T)(TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(value));
        }
    }
}
