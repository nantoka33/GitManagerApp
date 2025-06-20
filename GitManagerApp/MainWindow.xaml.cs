using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GitManagerApp
{
    public partial class MainWindow : Window
    {
        private const string BASE_DIR = "C:\\";
        private const string DEFAULT_BRANCH = "main";
        private const string RecentFilePath = "recent_projects.json";
        private const string ScheduleFilePath = "pull_push_schedule.json";

        private List<string> recentProjects = new();
        private List<GitSchedule> schedules = new();
        private DispatcherTimer scheduleTimer;

        public MainWindow()
        {
            InitializeComponent();
            scheduleTimer = new DispatcherTimer();
            scheduleTimer.Interval = TimeSpan.FromSeconds(10);
            scheduleTimer.Tick += ScheduleTimer_Tick;
            scheduleTimer.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRecentProjects();
            LoadSchedules();
        }

        private void Execute_Click(object sender, RoutedEventArgs e)
        {
            string projectName = ProjectComboBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(projectName))
            {
                Log("プロジェクト名を入力してください。");
                return;
            }

            SaveRecentProject(projectName);

            string targetDir = Path.Combine(BASE_DIR, projectName);
            if (!Directory.Exists(targetDir))
            {
                Log($"エラー: フォルダ {targetDir} が存在しません。");
                return;
            }

            Directory.SetCurrentDirectory(targetDir);
            string selectedAction = (ActionComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
            string commitMessage = CommitMessageBox.Text.Trim();
            string branchName = BranchNameBox.Text.Trim();

            if (selectedAction == "pull + push（PR対応）" && ScheduleDatePicker.SelectedDate != null)
            {
                string timePart = ScheduleTimeBox.Text.Trim();
                if (!TimeSpan.TryParse(timePart, out TimeSpan time))
                {
                    Log("時刻の形式が正しくありません。例: 14:00:00");
                    return;
                }
                DateTime scheduleTime = ScheduleDatePicker.SelectedDate.Value.Date + time;

                if (string.IsNullOrWhiteSpace(branchName) || string.IsNullOrWhiteSpace(commitMessage))
                {
                    Log("ブランチ名とコミットメッセージの両方を入力してください。");
                    return;
                }
                if (!branchName.StartsWith("feature/"))
                {
                    branchName = "feature/" + branchName;
                }

                schedules.Add(new GitSchedule
                {
                    ProjectName = projectName,
                    TargetDir = targetDir,
                    BranchName = branchName,
                    CommitMessage = commitMessage,
                    ExecuteAt = scheduleTime
                });
                File.WriteAllText(ScheduleFilePath, JsonSerializer.Serialize(schedules));
                Log($"pull+push を {scheduleTime} に予約しました。");
                return;
            }

            switch (selectedAction)
            {
                case "初回 push":
                    if (string.IsNullOrWhiteSpace(commitMessage)) commitMessage = "initial commit";
                    RunGit($"init && git add . && git commit -m \"{commitMessage}\"", targetDir);
                    break;

                case "通常 pull":
                    RunGit($"pull origin {DEFAULT_BRANCH}", targetDir);
                    break;

                case "pull + push（PR対応）":
                    if (string.IsNullOrWhiteSpace(branchName) || string.IsNullOrWhiteSpace(commitMessage))
                    {
                        Log("ブランチ名とコミットメッセージの両方を入力してください。");
                        return;
                    }
                    if (!branchName.StartsWith("feature/"))
                    {
                        branchName = "feature/" + branchName;
                    }
                    RunGit("fetch origin", targetDir);
                    RunGit($"checkout -b {branchName}", targetDir);
                    RunGit("add .", targetDir);
                    RunGit($"commit -m \"{commitMessage}\"", targetDir);
                    RunGit($"push -u origin {branchName}", targetDir);
                    break;

                case "ブランチ一覧表示":
                    RunGit("branch && git branch -r", targetDir);
                    break;

                case "ブランチ削除":
                    RunGit("branch -d branch_name", targetDir);
                    break;

                default:
                    Log("操作を選択してください。");
                    break;
            }
        }

        private void ScheduleTimer_Tick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            var toRun = schedules.Where(s => !s.Executed && s.ExecuteAt <= now).ToList();

            foreach (var schedule in toRun)
            {
                Log($"スケジュール実行: {schedule.ProjectName} / {schedule.BranchName}");
                if (!string.IsNullOrWhiteSpace(schedule.TargetDir)) Directory.SetCurrentDirectory(schedule.TargetDir);
                RunGit("fetch origin", schedule.TargetDir);
                RunGit($"checkout -b {schedule.BranchName}", schedule.TargetDir);
                RunGit("add .", schedule.TargetDir);
                RunGit($"commit -m \"{schedule.CommitMessage}\"", schedule.TargetDir);
                RunGit($"push -u origin {schedule.BranchName}", schedule.TargetDir);
                schedule.Executed = true;
            }

            File.WriteAllText(ScheduleFilePath, JsonSerializer.Serialize(schedules));
        }

        private void ShowSchedule_Click(object sender, RoutedEventArgs e)
        {
            string filter = FilterProjectComboBox.Text.Trim();
            Log("[予約一覧]");
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


        private void RunGit(string command, string workingDir)
        {
            var psi = new ProcessStartInfo("cmd.exe", $"/c git {command}")
            {
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                Log("git プロセスの起動に失敗しました。");
                return;
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Log(string.IsNullOrWhiteSpace(output) ? error : output);
        }

        private void Log(string text)
        {
            LogBox.AppendText(text + "\n");
            LogBox.ScrollToEnd();
        }

        private void SaveRecentProject(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            recentProjects.Remove(name);
            recentProjects.Insert(0, name);

            if (recentProjects.Count > 5)
                recentProjects = recentProjects.Take(5).ToList();

            File.WriteAllText(RecentFilePath, JsonSerializer.Serialize(recentProjects));

            ProjectComboBox.Items.Clear();
            foreach (var item in recentProjects)
                ProjectComboBox.Items.Add(item);
        }

        private void LoadRecentProjects()
        {
            if (File.Exists(RecentFilePath))
            {
                var json = File.ReadAllText(RecentFilePath);
                recentProjects = JsonSerializer.Deserialize<List<string>>(json) ?? new();

                foreach (var name in recentProjects)
                    ProjectComboBox.Items.Add(name);
            }
        }

        private void LoadSchedules()
        {
            if (File.Exists(ScheduleFilePath))
            {
                var json = File.ReadAllText(ScheduleFilePath);
                schedules = JsonSerializer.Deserialize<List<GitSchedule>>(json) ?? new();
            }
        }

        private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectComboBox.SelectedItem is ComboBoxItem item)
            {
                ProjectComboBox.Text = item.Content.ToString() ?? string.Empty;
            }
        }

        private void ClearUnexecutedSchedules_Click(object sender, RoutedEventArgs e)
        {
            int before = schedules.Count;
            schedules = schedules.Where(s => s.Executed).ToList();
            File.WriteAllText(ScheduleFilePath, JsonSerializer.Serialize(schedules));
            Log($"未実行の予約 {before - schedules.Count} 件を削除しました。");
        }
        
        private void ScheduleListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ScheduleListBox.SelectedItem is string selected)
            {
                var target = schedules.FirstOrDefault(s => !s.Executed && $"{s.ExecuteAt:yyyy-MM-dd HH:mm:ss} | {s.ProjectName} / {s.BranchName}" == selected);
                if (target != null)
                {
                    if (MessageBox.Show($"この予約を削除しますか？\n{selected}", "確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        schedules.Remove(target);
                        File.WriteAllText(ScheduleFilePath, JsonSerializer.Serialize(schedules));
                        Log($"予約を削除しました: {selected}");
                        ScheduleListBox.Items.Remove(selected);
                    }
                }
            }
        }

        private void ScheduleListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ScheduleListBox.SelectedItem is string selected)
            {
                var target = schedules.FirstOrDefault(s =>
                    $"{s.ExecuteAt:yyyy-MM-dd HH:mm:ss} | {s.ProjectName} / {s.BranchName} [{(s.Executed ? "実行済" : "未実行")}]" == selected);

                if (target != null && !target.Executed)
                {
                    if (MessageBox.Show($"この予約を削除しますか？\n{selected}", "確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        schedules.Remove(target);
                        File.WriteAllText(ScheduleFilePath, JsonSerializer.Serialize(schedules));
                        Log($"予約を削除しました: {selected}");
                        ScheduleListBox.Items.Remove(selected);
                    }
                }
                else if (target?.Executed == true)
                {
                    MessageBox.Show("この予約はすでに実行済みのため削除できません。", "情報", MessageBoxButton.OK);
                }
            }
        }

    }

    public class GitSchedule
    {
        public string? ProjectName { get; set; }
        public required string TargetDir { get; set; }
        public string? BranchName { get; set; }
        public string? CommitMessage { get; set; }
        public DateTime ExecuteAt { get; set; }
        public bool Executed { get; set; }
    }
}