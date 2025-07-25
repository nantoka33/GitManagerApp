﻿using GitManagerApp.Const;
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
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            if (!Directory.Exists(AppConstants.AppDataDir))
            {
                Directory.CreateDirectory(AppConstants.AppDataDir);
            }
            Log(AppConstants.ConfigFilePath);
            Log(AppConstants.RecentFilePath);
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
            foreach (var p in recentProjectService.Projects)
            {
                Log($"読み込んだプロジェクト: {p}", Brushes.LightBlue);
            }

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

            if (selectedAction == "pull + push（PR対応）" && string.IsNullOrWhiteSpace(ScheduleTimeBox.Text))
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
                    
                    var initResult = GitExecutor.RunWithResult("init", targetDir);
                    Log(initResult.FullMessage, initResult.IsSuccess ? null : Brushes.OrangeRed);
                    
                    var addAllResult = GitExecutor.RunWithResult("add .", targetDir);
                    Log(addAllResult.FullMessage, addAllResult.IsSuccess ? null : Brushes.OrangeRed);
                    
                    var commitResult = GitExecutor.RunWithResult($"commit -m \"{commitMessage}\"", targetDir);
                    Log(commitResult.FullMessage, commitResult.IsSuccess ? null : Brushes.OrangeRed);
                    
                    var remoteAddResult = GitExecutor.RunWithResult($"remote add origin {remoteUrl}", targetDir);
                    Log(remoteAddResult.FullMessage, remoteAddResult.IsSuccess ? null : Brushes.OrangeRed);
                    
                    var branchResult = GitExecutor.RunWithResult($"branch -M {AppConstants.DEFAULT_BRANCH}", targetDir);
                    Log(branchResult.FullMessage, branchResult.IsSuccess ? null : Brushes.OrangeRed);
                    
                    var pushResult = GitExecutor.RunWithResult($"push -u origin {AppConstants.DEFAULT_BRANCH}", targetDir);
                    Log(pushResult.FullMessage, pushResult.IsSuccess ? Brushes.LightGreen : Brushes.OrangeRed);
                    
                    if (pushResult.IsSuccess)
                    {
                        Log("初回プッシュに成功しました。", Brushes.LightGreen);
                    }
                    else
                    {
                        Log("初回プッシュに失敗しました。", Brushes.OrangeRed);
                    }
                    break;

                case AppConstants.Action.NormalPull:
                    var checkoutMainResult = GitExecutor.RunWithResult($"checkout {AppConstants.DEFAULT_BRANCH}", targetDir);
                    Log(checkoutMainResult.FullMessage, checkoutMainResult.IsSuccess ? null : Brushes.OrangeRed);
                    
                    var pullResult = GitExecutor.RunWithResult($"pull origin {AppConstants.DEFAULT_BRANCH}", targetDir);
                    Log(pullResult.FullMessage);
                    
                    if (pullResult.IsSuccess)
                    {
                        if (pullResult.Output.Contains("Fast-forward"))
                        {
                            Log("更新完了しました", Brushes.LightGreen);
                            Log("リモートリポジトリは最新状態です。", Brushes.LightGreen);
                        }
                        else if (pullResult.Output.Contains("Already up to date."))
                        {
                            Log("リモートリポジトリは最新状態です。", Brushes.LightGreen);
                        }
                        else
                        {
                            Log("更新が完了しました。", Brushes.LightGreen);
                        }
                    }
                    else
                    {
                        Log("更新失敗しました。", Brushes.OrangeRed);
                    }
                    break;

                case AppConstants.Action.ForcePull:
                    var forceCheckoutResult = GitExecutor.RunWithResult($"checkout {AppConstants.DEFAULT_BRANCH}", targetDir);
                    Log(forceCheckoutResult.FullMessage, forceCheckoutResult.IsSuccess ? null : Brushes.OrangeRed);
                    
                    var forcePullResult = GitExecutor.RunWithResult($"pull origin {AppConstants.DEFAULT_BRANCH}", targetDir);
                    Log(forcePullResult.FullMessage);
                    
                    if (forcePullResult.IsSuccess)
                    {
                        if (forcePullResult.Output.Contains("Fast-forward"))
                        {
                            Log("更新完了しました", Brushes.LightGreen);
                            Log("リモートリポジトリは最新状態です。", Brushes.LightGreen);
                        }
                        else
                        {
                            Log("更新失敗しました。", Brushes.OrangeRed);
                            Log("現在の変更をStashに格納し、強制的にpullします。", Brushes.OrangeRed);
                            
                            var stashResult = GitExecutor.RunWithResult("stash", targetDir);
                            Log(stashResult.FullMessage, stashResult.IsSuccess ? null : Brushes.OrangeRed);
                            
                            if (!stashResult.Output.Contains("No local changes to save"))
                            {
                                Log("一時変更を退避しました。", Brushes.LightGreen);
                                
                                var forcePullAfterStash = GitExecutor.RunWithResult($"pull origin {AppConstants.DEFAULT_BRANCH}", targetDir);
                                Log(forcePullAfterStash.FullMessage, forcePullAfterStash.IsSuccess ? Brushes.LightGreen : Brushes.OrangeRed);
                                
                                if (forcePullAfterStash.IsSuccess)
                                {
                                    Log("強制pull完了しました。", Brushes.LightGreen);
                                    
                                    var stashPopResult = GitExecutor.RunWithResult("stash pop", targetDir);
                                    Log(stashPopResult.FullMessage, stashPopResult.IsSuccess ? null : Brushes.OrangeRed);
                                    
                                    if (stashPopResult.IsSuccess)
                                    {
                                        Log("一時変更を適用しました。", Brushes.LightGreen);
                                    }
                                }
                            }
                            else
                            {
                                Log("更新完了しました", Brushes.LightGreen);
                                Log("リモートリポジトリは最新状態です。", Brushes.LightGreen);
                            }
                        }
                    }
                    else
                    {
                        Log("更新失敗しました。", Brushes.OrangeRed);
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
                    
                    // fetch origin
                    var pullPushFetchResult = GitExecutor.RunWithResult("fetch origin", targetDir);
                    Log(pullPushFetchResult.FullMessage, pullPushFetchResult.IsSuccess ? null : Brushes.OrangeRed);
                    
                    // checkout -b branchName
                    var pullPushCheckoutResult = GitExecutor.RunWithResult($"checkout -b {branchName}", targetDir);
                    Log(pullPushCheckoutResult.FullMessage, pullPushCheckoutResult.IsSuccess ? null : Brushes.OrangeRed);
                    
                    // add .
                    var pullPushAddResult = GitExecutor.RunWithResult("add .", targetDir);
                    Log(pullPushAddResult.FullMessage, pullPushAddResult.IsSuccess ? null : Brushes.OrangeRed);
                    
                    // commit
                    var pullPushCommitResult = GitExecutor.RunWithResult($"commit -m \"{commitMessage}\"", targetDir);
                    Log(pullPushCommitResult.FullMessage, pullPushCommitResult.IsSuccess ? null : Brushes.OrangeRed);
                    
                    // push -u origin branchName
                    var pullPushPushResult = GitExecutor.RunWithResult($"push -u origin {branchName}", targetDir);
                    
                    // push結果の詳細な判定
                    if (pullPushPushResult.IsSuccess)
                    {
                        Log(pullPushPushResult.FullMessage);
                        if (pullPushPushResult.Output.Contains("set up to track") || 
                            pullPushPushResult.Output.Contains("Branch '") && pullPushPushResult.Output.Contains("' set up to track"))
                        {
                            Log("プッシュに成功しました。", Brushes.LightGreen);
                        }
                        else if (pullPushPushResult.Output.Contains("Everything up-to-date"))
                        {
                            Log("リモートリポジトリは最新状態です。", Brushes.LightGreen);
                        }
                        else
                        {
                            Log("プッシュに成功しました。", Brushes.LightGreen);
                        }
                    }
                    else
                    {
                        Log(pullPushPushResult.FullMessage, Brushes.OrangeRed);
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
                
                bool allSuccess = true;
                
                // fetch origin
                var scheduleFetchResult = GitExecutor.RunWithResult("fetch origin", s.TargetDir);
                Log(scheduleFetchResult.FullMessage, scheduleFetchResult.IsSuccess ? null : Brushes.OrangeRed);
                if (!scheduleFetchResult.IsSuccess) allSuccess = false;
                
                // checkout -b branchName
                var scheduleCheckoutResult = GitExecutor.RunWithResult($"checkout -b {s.BranchName}", s.TargetDir);
                Log(scheduleCheckoutResult.FullMessage, scheduleCheckoutResult.IsSuccess ? null : Brushes.OrangeRed);
                if (!scheduleCheckoutResult.IsSuccess) allSuccess = false;
                
                // add .
                var scheduleAddResult = GitExecutor.RunWithResult("add .", s.TargetDir);
                Log(scheduleAddResult.FullMessage, scheduleAddResult.IsSuccess ? null : Brushes.OrangeRed);
                if (!scheduleAddResult.IsSuccess) allSuccess = false;
                
                // commit
                var scheduleCommitResult = GitExecutor.RunWithResult($"commit -m \"{s.CommitMessage}\"", s.TargetDir);
                Log(scheduleCommitResult.FullMessage, scheduleCommitResult.IsSuccess ? null : Brushes.OrangeRed);
                if (!scheduleCommitResult.IsSuccess) allSuccess = false;
                
                // push -u origin branchName
                var schedulePushResult = GitExecutor.RunWithResult($"push -u origin {s.BranchName}", s.TargetDir);
                Log(schedulePushResult.FullMessage, schedulePushResult.IsSuccess ? null : Brushes.OrangeRed);
                if (!schedulePushResult.IsSuccess) allSuccess = false;
                
                // すべてのコマンドが成功した場合のみ実行済みとする
                if (allSuccess)
                {
                    s.Executed = true;
                    Log($"スケジュール実行完了: {s.ProjectName} / {s.BranchName}", Brushes.LightGreen);
                }
                else
                {
                    Log($"スケジュール実行失敗: {s.ProjectName} / {s.BranchName}", Brushes.OrangeRed);
                }
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

        /// <summary>
        /// 削除するブランチ名をコピーするボタンのクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteCopy_Click(object sender, RoutedEventArgs e)
        {
            var branchName = BranchNameBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(branchName))
            {
                DeleteBranchBox.Text = branchName;
            }
        }

        /// <summary>
        /// 現在の日時を設定するボタンのクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NowTime_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            ScheduleDatePicker.SelectedDate = now.Date;
            ScheduleTimeBox.Text = "";
            Log($"現在の日時を設定しました: {now:yyyy-MM-dd HH:mm:ss}", Brushes.LightGreen);
        }

        /// <summary>
        /// 3分後の日時を設定するボタンのクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ThreeMTime_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            ScheduleDatePicker.SelectedDate = now.Date;
            ScheduleTimeBox.Text = now.AddMinutes(3).ToString("HH:mm:ss");
            Log($"3分後の日時を設定しました: {now.AddMinutes(3):yyyy-MM-dd HH:mm:ss}", Brushes.LightGreen);
        }

        /// <summary>
        /// 30分後の日時を設定するボタンのクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HarfHTime_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            ScheduleDatePicker.SelectedDate = now.Date;
            ScheduleTimeBox.Text = now.AddMinutes(30).ToString("HH:mm:ss");
            Log($"30分後の日時を設定しました: {now.AddMinutes(30):yyyy-MM-dd HH:mm:ss}", Brushes.LightGreen);
        }

        /// <summary>
        /// 1時間後の日時を設定するボタンのクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HTime_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            ScheduleDatePicker.SelectedDate = now.Date;
            ScheduleTimeBox.Text = now.AddHours(1).ToString("HH:mm:ss");
            Log($"1時間後の日時を設定しました: {now.AddHours(1):yyyy-MM-dd HH:mm:ss}", Brushes.LightGreen);
        }

        /// <summary>
        /// 1日後の日時を設定するボタンのクリックイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OneDTime_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            ScheduleDatePicker.SelectedDate = now.AddDays(1).Date;
            ScheduleTimeBox.Text = "01:00:00";
            Log($"1日後の日時を設定しました: {now.AddDays(1):yyyy-MM-dd HH:mm:ss}", Brushes.LightGreen);
        }
    }
}