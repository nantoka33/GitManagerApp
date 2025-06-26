using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitManagerApp.Models
{
    /// <summary>
    /// Gitのスケジュールを表すクラス
    /// </summary>
    public class GitSchedule
    {
        /// <summary>
        /// プロジェクト名
        /// </summary>
        public string? ProjectName { get; set; }

        /// <summary>
        /// 対象ディレクトリのパス
        /// </summary>
        public required string TargetDir { get; set; }

        /// <summary>
        /// ブランチ名
        /// </summary>
        public string? BranchName { get; set; }

        /// <summary>
        /// コミットメッセージ
        /// </summary>
        public string? CommitMessage { get; set; }

        /// <summary>
        /// 予定時間
        /// </summary>
        public DateTime ExecuteAt { get; set; }

        /// <summary>
        /// 実行済みフラグ
        /// </summary>
        public bool Executed { get; set; }
    }
}
