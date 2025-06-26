using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitManagerApp.Const
{
    /// <summary>
    /// 定数クラス
    /// </summary>
    internal class AppConstants
    {
        /// <summary>
        /// ルートディレクトリのパス
        /// </summary>
        public const string BASE_DIR = "C:\\";

        /// <summary>
        /// デフォルトのブランチ名
        /// </summary>
        public const string DEFAULT_BRANCH = "main";

        /// <summary>
        /// AppData ディレクトリのパス
        /// </summary>
        public static string AppDataDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GitManagerApp");

        /// <summary>
        /// プロジェクトの設定ファイル名
        /// </summary>
        public static string RecentFilePath => Path.Combine(AppDataDir ?? string.Empty, "recent_projects.json");

        /// <summary>
        /// 実行予約ファイル名
        /// </summary>
        public static string ConfigFilePath => Path.Combine(AppDataDir ?? string.Empty, "config.json");

        /// <summary>
        /// アクション名を定義するクラス
        /// </summary>
        public class Action
        {
            /// <summary>
            /// 初回 push のアクション名
            /// </summary>
            public const string FirastPush = "初回 push";

            /// <summary>
            /// 通常 pull のアクション名
            /// </summary>
            public const string NormalPull = "通常 pull";

            /// <summary>
            /// 強制 pull のアクション名
            /// </summary>
            public const string ForcePull = "強制 pull";

            /// <summary>
            /// pull + push のアクション名
            /// </summary>
            public const string PullAndPush = "pull + push（PR対応）";

            /// <summary>
            /// ブランチ一覧表示 のアクション名
            /// </summary>
            public const string BranchList = "ブランチ一覧表示";

            /// <summary>
            /// ブランチ削除 のアクション名
            /// </summary>
            public const string DeleteBranch = "ブランチ削除";

            /// <summary>
            /// ブランチ変更 のアクション名
            /// </summary>
            public const string ModifyBranch = "ブランチ変更";

            /// <summary>
            /// ブランチ作成 のアクション名
            /// </summary>
            public const string CreateBranch = "ブランチ作成";

            /// <summary>
            /// ブランチマージ のアクション名
            /// </summary>
            public const string BranchMerge = "ブランチマージ";

            /// <summary>
            /// 削除済みリモートブランチの削除 のアクション名
            /// </summary>
            public const string DeleteRemoteBranch = "削除済みリモートブランチの削除";

            /// <summary>
            /// 一時変更破棄(pull前) のアクション名
            /// </summary>
            public const string TempBeforePull = "一時変更破棄(pull前)";
        }
    }
}