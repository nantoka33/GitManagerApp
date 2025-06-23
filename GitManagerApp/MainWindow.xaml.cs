using GitManagerApp.Const;
using GitManagerApp.Models;
using GitManagerApp.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace GitManagerApp
{
    /// <summary>
    /// GitManagerAppのメインウィンドウ
    /// </summary>
    public partial class MainWindow : Window
    {
        #region property
        private List<GitSchedule> schedules = new();
        private DispatcherTimer scheduleTimer;

        private ConfigService configService;
        private ScheduleManager? scheduleManager;
        private RecentProjectService recentProjectService;
        #endregion

        #region constructor
        public MainWindow()
        {
            InitializeComponent();
            configService = new ConfigService(AppConstants.ConfigFilePath);
            recentProjectService = new RecentProjectService(AppConstants.RecentFilePath);
            scheduleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            scheduleTimer.Tick += ScheduleTimer_Tick;
            scheduleTimer.Start();
        }
        #endregion

        #region event handlers
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var config = configService.Load();
            if (!string.IsNullOrWhiteSpace(config?.ScheduleFilePath))
                ScheduleFilePathBox.Text = config.ScheduleFilePath;

            recentProjectService.PopulateComboBox(ProjectComboBox);

            scheduleManager = new ScheduleManager(GetScheduleFilePath());
            schedules = scheduleManager.Load();
        }

        private void OpenScheduleFileDialog_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "スケジュール保存ファイルを選択",
                Filter = "JSON ファイル (*.json)|*.json",
                FileName = "pull_push_schedule.json",
                InitialDirectory = AppConstants.BASE_DIR
            };

            if (dialog.ShowDialog() == true)
            {
                ScheduleFilePathBox.Text = dialog.FileName;
                configService.Save(new AppConfig { ScheduleFilePath = dialog.FileName });
                Log($"スケジュール保存先を設定: {dialog.FileName}", Brushes.LightGreen);

                scheduleManager = new ScheduleManager(dialog.FileName);
                schedules = scheduleManager.Load();
            }
        }

        private void Execute_Click(object sender, RoutedEventArgs e)
        {
            string projectName = ProjectComboBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(projectName))
            {
                Log("プロジェクト名を入力してください。", Brushes.LightGreen);
                return;
            }

            recentProjectService.Save(projectName);
            configService.Save(new AppConfig { ScheduleFilePath = GetScheduleFilePath() });

            string targetDir = Path.Combine(AppConstants.BASE_DIR, projectName);
            if (!Directory.Exists(targetDir))
            {
                Log($"エラー: フォルダ {targetDir} が存在しません。", Brushes.OrangeRed);
                return;
            }

            Directory.SetCurrentDirectory(targetDir);
            string selectedAction = (ActionComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
            string commitMessage = CommitMessageBox.Text.Trim();
            string branchName = BranchNameBox.Text.Trim();
            string deleteBranchName = DeleteBranchBox.Text.Trim();
            string renameBranchName = RenameBranchBox.Text.Trim();
            string scheduleFilePath = GetScheduleFilePath();

            if (string.IsNullOrWhiteSpace(ScheduleTimeBox.Text))
            {
                Log("時刻指定がないため、即時実行します。", Brushes.LightGreen);
            }
            else if (selectedAction == "pull + push（PR対応）" && ScheduleDatePicker.SelectedDate != null)
            {
                
                if (!TimeSpan.TryParse(ScheduleTimeBox.Text.Trim(), out TimeSpan time))
                {
                    Log("時刻の形式が正しくありません。例: 14:00:00", Brushes.OrangeRed);
                    return;
                }
                DateTime scheduleTime = ScheduleDatePicker.SelectedDate.Value.Date + time;

                if (string.IsNullOrWhiteSpace(branchName) || string.IsNullOrWhiteSpace(commitMessage))
                {
                    Log("ブランチ名とコミットメッセージの両方を入力してください。", Brushes.OrangeRed);
                    return;
                }
                if (!branchName.StartsWith("feature/"))
                    branchName = "feature/" + branchName;

                schedules.Add(new GitSchedule
                {
                    ProjectName = projectName,
                    TargetDir = targetDir,
                    BranchName = branchName,
                    CommitMessage = commitMessage,
                    ExecuteAt = scheduleTime
                });

                scheduleManager?.Save(schedules);
                Log($"pull+push を {scheduleTime} に予約しました。");
                return;
            }

            switch (selectedAction)
            {
                case AppConstants.Action.FirastPush:
                    if (string.IsNullOrWhiteSpace(commitMessage)) commitMessage = "initial commit";
                    string remoteUrl = RemoteUrlBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(remoteUrl))
                    {
                        Log("リモートリポジトリURLを入力してください。", Brushes.OrangeRed);
                        return;
                    }
                    Log(GitExecutor.Run($"init && git add . && git commit -m \"{commitMessage}\" && git remote add origin {remoteUrl} && git branch -M {AppConstants.DEFAULT_BRANCH} && git push -u origin {AppConstants.DEFAULT_BRANCH}", targetDir, out _));
                    break;

                case AppConstants.Action.NormalPull:
                    Log(GitExecutor.Run($"checkout {AppConstants.DEFAULT_BRANCH}", targetDir, out _));
                    var resultLog = GitExecutor.Run($"pull origin {AppConstants.DEFAULT_BRANCH}", targetDir, out _);
                    if (resultLog.Contains("Fast-forward"))
                    {
                        Log(resultLog);
                        Log("更新完了しました", Brushes.LightGreen);
                        Log("リモートリポジトリは最新状態です。", Brushes.LightGreen);
                    }
                    else if (resultLog.Contains("Already up to date."))
                    {
                        Log(resultLog);
                        Log("リモートリポジトリは最新状態です。", Brushes.LightGreen);
                    }
                    else
                    {
                        Log(resultLog);
                        Log("更新失敗しました。", Brushes.OrangeRed);
                    }
                    break;

                case AppConstants.Action.ForcePull:
                    Log(GitExecutor.Run($"checkout {AppConstants.DEFAULT_BRANCH}", targetDir, out _));
                    resultLog = GitExecutor.Run($"pull origin {AppConstants.DEFAULT_BRANCH}", targetDir, out _);
                    if (resultLog.Contains("Fast-forward"))
                    {
                        Log(resultLog);
                        Log("更新完了しました", Brushes.LightGreen);
                        Log("リモートリポジトリは最新状態です。", Brushes.LightGreen);
                    }
                    else
                    {
                        Log(resultLog);
                        Log("更新失敗しました。", Brushes.OrangeRed);
                        Log("現在の変更をStashに格納し、強制的にpullします。", Brushes.OrangeRed);
                        Log(GitExecutor.Run("stash", targetDir, out _));
                        Log("一時変更を退避しました。", Brushes.LightGreen);
                        Log(GitExecutor.Run($"pull origin {AppConstants.DEFAULT_BRANCH}", targetDir, out _));
                        Log("強制pull完了しました。", Brushes.LightGreen);
                        Log(GitExecutor.Run("stash pop", targetDir, out _));
                        Log("一時変更を適用しました。", Brushes.LightGreen);
                    }
                    break;

                case AppConstants.Action.PullAndPush:
                    if (string.IsNullOrWhiteSpace(branchName) || string.IsNullOrWhiteSpace(commitMessage))
                    {
                        Log("ブランチ名とコミットメッセージの両方を入力してください。", Brushes.OrangeRed);
                        return;
                    }
                    if (!branchName.StartsWith("feature/"))
                        branchName = "feature/" + branchName;
                    Log(GitExecutor.Run("fetch origin", targetDir, out _));
                    Log(GitExecutor.Run($"checkout -b {branchName}", targetDir, out _));
                    Log(GitExecutor.Run("add .", targetDir, out _));
                    Log(GitExecutor.Run($"commit -m \"{commitMessage}\"", targetDir, out _));
                    var pushResult = GitExecutor.Run($"push -u origin {branchName}", targetDir, out _);
                    if (pushResult.Contains("changed"))
                    {
                        Log(pushResult);
                        Log("プッシュに成功しました。", Brushes.LightGreen);
                    }
                    else if (pushResult.Contains("Everything up-to-date"))
                    {
                        Log(pushResult);
                        Log("リモートリポジトリは最新状態です。", Brushes.LightGreen);
                    }
                    else
                    {
                        Log(pushResult, Brushes.OrangeRed);
                        Log("プッシュに失敗しました。", Brushes.OrangeRed);
                    }
                    break;

                case AppConstants.Action.BranchList:
                    Log("ブランチ一覧を更新します。", Brushes.LightGreen);
                    Log(GitExecutor.Run("branch && git branch -r", targetDir, out _));
                    break;

                case AppConstants.Action.DeleteBranch:
                    if (!string.IsNullOrWhiteSpace(deleteBranchName) && !deleteBranchName.StartsWith("feature/"))
                        deleteBranchName = "feature/" + deleteBranchName;
                    if (string.IsNullOrWhiteSpace(deleteBranchName))
                    {
                        Log("削除するブランチ名を入力してください。", Brushes.OrangeRed);
                        return;
                    }
                    Log($"指定ブランチを削除します。 : {deleteBranchName}", Brushes.LightGreen);
                    Log(GitExecutor.Run($"branch -d {deleteBranchName}", targetDir, out _));
                    break;

                case AppConstants.Action.ModifyBranch:
                    if (string.IsNullOrWhiteSpace(renameBranchName))
                    {
                        Log("切り替えるブランチ名を入力してください。", Brushes.OrangeRed);
                        return;
                    }
                    Log(GitExecutor.Run($"checkout {renameBranchName}", targetDir, out _));
                    Log("ブランチを変更しました。", Brushes.LightGreen);
                    Log(GitExecutor.Run("branch && git branch -r", targetDir, out _));
                    break;

                case AppConstants.Action.CreateBranch:
                    if (string.IsNullOrWhiteSpace(branchName))
                    {
                        Log("新しいブランチ名を入力してください。", Brushes.OrangeRed);
                        return;
                    }
                    if (!branchName.StartsWith("feature/"))
                        branchName = "feature/" + branchName;
                    Log(GitExecutor.Run($"checkout -b {branchName}", targetDir, out _));
                    break;

                case AppConstants.Action.BranchMerge:
                    if (string.IsNullOrWhiteSpace(branchName))
                    {
                        Log("マージするブランチ名を入力してください。", Brushes.OrangeRed);
                        return;
                    }
                    Log(GitExecutor.Run($"merge {branchName}", targetDir, out _));
                    break;

                case AppConstants.Action.DeleteRemoteBranch:
                    Log(GitExecutor.Run("fetch --prune", targetDir, out _));
                    Log("リポジトリの削除済みリモートブランチをローカルから削除しました。", Brushes.LightGreen);
                    Log("ブランチ一覧を更新します。", Brushes.LightGreen);
                    Log(GitExecutor.Run("branch && git branch -r", targetDir, out _));
                    break;

                case AppConstants.Action.TempBeforePull:
                    Log(GitExecutor.Run("stash", targetDir, out _));
                    Log(GitExecutor.Run("stash drop", targetDir, out _));
                    Log("一時変更を破棄しました。", Brushes.LightGreen);
                    break;
                /* 
                 * あとで実装
                 * 
                case "リモートリポジトリURL変更":
                    if (string.IsNullOrWhiteSpace(RemoteUrlBox.Text.Trim()))
                    {
                        Log("新しいリモートリポジトリURLを入力してください。", Brushes.OrangeRed);
                        return;
                    }
                    Log(GitExecutor.Run($"remote set-url origin {RemoteUrlBox.Text.Trim()}", targetDir, out _));
                    break;

                case "リモートリポジトリURL表示":
                    Log(GitExecutor.Run("remote -v", targetDir, out _));
                    break;

                case "リモートリポジトリ削除":
                    Log(GitExecutor.Run("remote remove origin", targetDir, out _));
                    break;

                case "リモートリポジトリ一覧表示":
                    Log(GitExecutor.Run("remote", targetDir, out _));
                    break;

                case "リモートリポジトリ追加":
                    remoteUrl = RemoteUrlBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(remoteUrl))
                    {
                        Log("リモートリポジトリURLを入力してください。", Brushes.OrangeRed);
                        return;
                    }
                    Log(GitExecutor.Run($"remote add origin {remoteUrl}", targetDir, out _));
                    break;

                case "リモートリポジトリのブランチ一覧表示":
                    Log(GitExecutor.Run("branch -r", targetDir, out _));
                    break;

                case "リモートリポジトリのブランチをローカルにチェックアウト":
                    Log(GitExecutor.Run("fetch origin", targetDir, out _));
                    Log(GitExecutor.Run($"checkout -b {branchName} origin/{branchName}", targetDir, out _));
                    break;

                case "リモートリポジトリのブランチをローカルにマージ":
                    Log(GitExecutor.Run($"merge origin/{branchName}", targetDir, out _));
                    break;

                case "リモートリポジトリのブランチをローカルにフェッチ":
                    Log(GitExecutor.Run("fetch origin", targetDir, out _));
                    break;

                case "リモートリポジトリのブランチをローカルに削除":
                    if (string.IsNullOrWhiteSpace(branchName))
                    {
                        Log("削除するブランチ名を入力してください。", Brushes.OrangeRed);
                        return;
                    }
                    Log(GitExecutor.Run($"branch -d {branchName}", targetDir, out _));
                    break;
                */
                default:
                    Log("操作を選択してください。", Brushes.OrangeRed);
                    break;
            }
        }

        private void ScheduleTimer_Tick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            var toRun = schedules.FindAll(s => !s.Executed && s.ExecuteAt <= now);
            foreach (var s in toRun)
            {
                Log($"スケジュール実行: {s.ProjectName} / {s.BranchName}", Brushes.LightGreen);
                Log(GitExecutor.Run("fetch origin", s.TargetDir, out _));
                Log(GitExecutor.Run($"checkout -b {s.BranchName}", s.TargetDir, out _));
                Log(GitExecutor.Run("add .", s.TargetDir, out _));
                Log(GitExecutor.Run($"commit -m \"{s.CommitMessage}\"", s.TargetDir, out _));
                Log(GitExecutor.Run($"push -u origin {s.BranchName}", s.TargetDir, out _));
                s.Executed = true;
            }
            scheduleManager?.Save(schedules);
        }

        private void ShowSchedule_Click(object sender, RoutedEventArgs e)
        {
            string filter = FilterProjectComboBox.Text.Trim();
            Log("[予約一覧]", Brushes.LightGreen);
            ScheduleListBox.Items.Clear();
            foreach (var s in schedules)
            {
                if (!string.IsNullOrWhiteSpace(filter) && !s.ProjectName?.Equals(filter, StringComparison.OrdinalIgnoreCase) == true)
                    continue;
                string label = $"{s.ExecuteAt:yyyy-MM-dd HH:mm:ss} | {s.ProjectName} / {s.BranchName} [{(s.Executed ? "実行済" : "未実行")}]";
                ScheduleListBox.Items.Add(label);
                Log("・" + label);
            }
        }

        private void ClearUnexecutedSchedules_Click(object sender, RoutedEventArgs e)
        {
            int before = schedules.Count;
            scheduleManager?.RemoveExecuted(ref schedules);
            Log($"未実行の予約 {before - schedules.Count} 件を削除しました。", Brushes.LightGreen);
        }

        private void ScheduleListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ScheduleListBox.SelectedItem is string selected)
            {
                var target = schedules.Find(s => $"{s.ExecuteAt:yyyy-MM-dd HH:mm:ss} | {s.ProjectName} / {s.BranchName} [{(s.Executed ? "実行済" : "未実行")}]" == selected);
                if (target != null && !target.Executed)
                {
                    if (MessageBox.Show($"この予約を削除しますか？\n{selected}", "確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        scheduleManager?.RemoveSchedule(schedules, target);
                        Log($"予約を削除しました: {selected}", Brushes.LightGreen);
                        ScheduleListBox.Items.Remove(selected);
                    }
                }
                else if (target?.Executed == true)
                {
                    MessageBox.Show("この予約はすでに実行済みのため削除できません。", "情報", MessageBoxButton.OK);
                }
            }
        }
        #endregion

        #region private methods
        private string GetScheduleFilePath() =>
            string.IsNullOrWhiteSpace(ScheduleFilePathBox.Text) ? "pull_push_schedule.json" : ScheduleFilePathBox.Text.Trim();

        private void Log(string message, Brush? color = null)
        {
            color ??= Brushes.White; // デフォルト白

            var paragraph = new Paragraph(new Run(message))
            {
                Foreground = color,
                Margin = new Thickness(0)
            };

            LogBox.Document.Blocks.Add(paragraph);
            LogBox.ScrollToEnd();
        }
        #endregion
    }
}