using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitManagerApp.Models
{
    /// <summary>
    /// アプリケーションの設定を表すクラス
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// スケジュールファイルのパス
        /// </summary>
        public string? ScheduleFilePath { get; set; }
    }
}

