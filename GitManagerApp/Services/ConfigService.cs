using GitManagerApp.Models;
using System;
using System.IO;
using System.Text.Json;

namespace GitManagerApp.Services
{
    public class ConfigService
    {
        private readonly string configPath;

        public ConfigService(string configFilePath)
        {
            configPath = configFilePath;
        }

        public void Save(AppConfig config)
        {
            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(configPath, json);
        }

        public AppConfig? Load()
        {
            if (!File.Exists(configPath)) return null;
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<AppConfig>(json);
        }
    }
}
