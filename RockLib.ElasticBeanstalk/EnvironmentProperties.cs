using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RockLib.ElasticBeanstalk
{
    /// <summary>
    /// Defines the <see cref="MapToEnvironmentVariables()"/> method.
    /// </summary>
    public static class EnvironmentProperties
    {
        /// <summary>
        /// Defines the path where the Elastic Beanstalk configuration file is located.
        /// </summary>
        public const string ConfigurationPath = @"C:\Program Files\Amazon\ElasticBeanstalk\config\containerconfiguration";

        /// <summary>
        /// Sets environment variables for the current process according to Elastic Beanstalk Environment
        /// Properties.
        /// </summary>
        /// <remarks>
        /// This method exists because there is no support for accessing Elastic Beanstalk Environment
        /// Properties from .NET Core applications.
        /// </remarks>
        public static void MapToEnvironmentVariables() => MapToEnvironmentVariables(ConfigurationPath);

        internal static void MapToEnvironmentVariables(string path) =>
            Map(path, File.Exists, File.ReadAllText, GetEnvironmentProperties, Environment.SetEnvironmentVariable);

        internal static void Map(
            string path,
            Func<string, bool> fileExists,
            Func<string, string> readAllText,
            Func<string, IEnumerable<(string, string)>> getEnvironmentProperties,
            Action<string, string> setEnvironmentVariable)
        {
            if (fileExists(path))
            {
                string raw;
                try { raw = readAllText(path); }
                catch { return; }

                foreach (var (variable, value) in getEnvironmentProperties(raw))
                    setEnvironmentVariable(variable, value);
            }
        }

        internal static IEnumerable<(string, string)> GetEnvironmentProperties(string raw)
        {
            dynamic json;
            try { json = JsonConvert.DeserializeObject(raw) as JObject; }
            catch { json = null; }

            if (json?.iis is JObject && json.iis.env is JArray array)
            {
                return from item in array.Children().OfType<JValue>()
                       let tokens = (item.Value as string)?.Split(new[] { '=' }, 2)
                       where tokens != null && tokens.Length == 2 && tokens[0] != ""
                       select (tokens[0], tokens[1]);
            }

            return Enumerable.Empty<(string, string)>();
        }
    }
}
