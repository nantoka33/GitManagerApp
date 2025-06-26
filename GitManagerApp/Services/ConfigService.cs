using GitManagerApp.Models;
using System;
using System.IO;
using System.Text.Json;

namespace GitManagerApp.Services
{
    /// <summary>
    /// コンフィグサービスクラス
    /// </summary>
    public class ConfigService
    {
        /// <summary>
        /// コンフィグファイルのパス
        /// </summary>
        private readonly string configPath;

        /// <summary>
        /// コンフィグサービスのコンストラクタ
        /// </summary>
        /// <param name="configFilePath">コンフィグファイルのパス</param>
        public ConfigService(string configFilePath)
        {
            configPath = configFilePath;
        }

        /// <summary>
        /// コンフィグを保存するメソッド
        /// </summary>
        /// <param name="config">コンフィグ</param>
        public void Save(AppConfig config)
        {
            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(configPath, json);
        }

        /// <summary>
        /// コンフィグをロードするメソッド
        /// </summary>
        /// <returns>コンフィグ</returns>
        public AppConfig? Load()
        {
            if (!File.Exists(configPath)) return null;
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<AppConfig>(json);
        }
    }
}
