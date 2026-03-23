using System;
using System.IO;
using Newtonsoft.Json;
using SyncDB.Model;

namespace SyncDB.Service
{
    public class ConfigService
    {
        private readonly string _configPath;

        public ConfigService(string appPath)
        {
            _configPath = Path.Combine(appPath, "config.json");
        }

        public AppConfig Load()
        {
            try
            {
                if (!File.Exists(_configPath))
                    return new AppConfig();

                var json = File.ReadAllText(_configPath);
                return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        public void Save(AppConfig config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }
    }
}
