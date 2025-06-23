using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitManagerApp.Const
{
    public class MessageId
    {
        /// <summary>
        /// メッセージのIDを定義する列挙型
        /// </summary>
        public enum EnumMessageId
        {
            /// <summary>
            /// ブランチを変更しました。
            /// </summary>
            BranchChanged = 1,

            /// <summary>
            /// ブランチ一覧を更新します。
            /// </summary>
            BranchListUpdated = 2,

            /// <summary>
            /// ブランチ名とコミットメッセージの両方を入力してください。
            /// </summary>
            EnterBranchNameAndMessage = 3,

            /// <summary>
            /// プッシュに失敗しました。
            /// </summary>
            PushFailed = 4,

            /// <summary>
            /// プッシュに成功しました。
            /// </summary>
            PushSucceeded = 5,

            /// <summary>
            /// マージするブランチ名を入力してください。
            /// </summary>
            EnterBranchNameToMerge = 6,

            /// <summary>
            /// リポジトリの削除済みリモートブランチをローカルから削除しました。
            /// </summary>
            DeletedRemoteBranch = 7,

            /// <summary>
            /// リモートリポジトリURLを入力してください。
            /// </summary>
            EnterRemoteRepoUrl = 8,

            /// <summary>
            /// リモートリポジトリは最新状態です。
            /// </summary>
            RemoteRepoUpToDate = 9,

            /// <summary>
            /// 一時変更を破棄しました。
            /// </summary>
            TempChangeDiscarded = 10,

            /// <summary>
            /// 一時変更を退避しました。
            /// </summary>
            TempChangeStored = 11,

            /// <summary>
            /// 一時変更を適用しました。
            /// </summary>
            TempChangeApplied = 12,

            /// <summary>
            /// 切り替えるブランチ名を入力してください。
            /// </summary>
            EnterBranchNameToSwitch = 13,

            /// <summary>
            /// 削除するブランチ名を入力してください。
            /// </summary>
            EnterBranchNameToDelete = 14,

            /// <summary>
            /// 強制pull完了しました。
            /// </summary>
            ForcePullCompleted = 15,

            /// <summary>
            /// 操作を選択してください。
            /// </summary>
            SelectAction = 16,

            /// <summary>
            /// 新しいブランチ名を入力してください。
            /// </summary>
            EnterNewBranchName = 17,

            /// <summary>
            /// 新しいリモートリポジトリURLを入力してください。
            /// </summary>
            EnterNewRemoteRepoUrl = 18,

            /// <summary>
            /// 更新失敗しました。
            /// </summary>
            UpdateFailed = 19,

            /// <summary>
            /// 更新完了しました
            /// </summary>
            UpdateCompleted = 20,

            /// <summary>
            /// 現在の変更をStashに格納し、強制的にpullします。
            /// </summary>
            StashAndForcePull = 21,

            /// <summary>
            /// $指定ブランチを削除します。 : {deleteBranchName}
            /// </summary>
            DeleteSpecifiedBranch = 22,
        }
    }
}
