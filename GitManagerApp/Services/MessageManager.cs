using GitManagerApp.Const;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitManagerApp.Services
{
    public static class MessageManager
    {
        /// <summary>
        /// メッセージIDとメッセージのマッピング
        /// </summary>
        private static readonly Dictionary<MessageId.EnumMessageId, string> Messages = new()
        {
            { MessageId.EnumMessageId.BranchChanged, "ブランチを変更しました。" },
            { MessageId.EnumMessageId.BranchListUpdated, "ブランチ一覧を更新します。" },
            { MessageId.EnumMessageId.EnterBranchNameAndMessage, "ブランチ名とコミットメッセージの両方を入力してください。" },
            { MessageId.EnumMessageId.PushFailed, "プッシュに失敗しました。" },
            { MessageId.EnumMessageId.PushSucceeded, "プッシュに成功しました。" },
            { MessageId.EnumMessageId.EnterBranchNameToMerge, "マージするブランチ名を入力してください。" },
            { MessageId.EnumMessageId.DeletedRemoteBranch, "リポジトリの削除済みリモートブランチをローカルから削除しました。" },
            { MessageId.EnumMessageId.EnterRemoteRepoUrl, "リモートリポジトリURLを入力してください。" },
            { MessageId.EnumMessageId.RemoteRepoUpToDate, "リモートリポジトリは最新状態です。" },
            { MessageId.EnumMessageId.TempChangeDiscarded, "一時変更を破棄しました。" },
            { MessageId.EnumMessageId.TempChangeStored, "一時変更を退避しました。" },
            { MessageId.EnumMessageId.TempChangeApplied, "一時変更を適用しました。" },
            { MessageId.EnumMessageId.EnterBranchNameToSwitch, "切り替えるブランチ名を入力してください。" },
            { MessageId.EnumMessageId.EnterBranchNameToDelete, "削除するブランチ名を入力してください。" },
            { MessageId.EnumMessageId.ForcePullCompleted, "強制pull完了しました。" },
            { MessageId.EnumMessageId.SelectAction, "操作を選択してください。" },
            { MessageId.EnumMessageId.EnterNewBranchName, "新しいブランチ名を入力してください。" },
            { MessageId.EnumMessageId.EnterNewRemoteRepoUrl, "新しいリモートリポジトリURLを入力してください。" },
            { MessageId.EnumMessageId.UpdateFailed, "更新失敗しました。" },
            { MessageId.EnumMessageId.UpdateCompleted, "更新完了しました" },
            { MessageId.EnumMessageId.StashAndForcePull, "現在の変更をStashに格納し、強制的にpullします。" },
            { MessageId.EnumMessageId.DeleteSpecifiedBranch, "指定ブランチを削除します。 : {0}" },
        };

        /// <summary>
        /// メッセージIDに対応するメッセージを取得します。
        /// </summary>
        /// <param name="id">メッセージID</param>
        /// <param name="args">指定文字</param>
        /// <returns></returns>
        public static string GetMessage(MessageId.EnumMessageId id, params object[] args)
        {
            if (Messages.TryGetValue(id, out var template))
            {
                return args.Length > 0 ? string.Format(template, args) : template;
            }
            return $"[未定義のメッセージ: {id}]";
        }
    }
}
