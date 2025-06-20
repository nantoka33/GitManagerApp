using System;
using System.Diagnostics;

namespace GitManagerApp.Services
{
    public static class GitExecutor
    {
        public static string Run(string command, string workingDir, out string error)
        {
            error = "";
            var psi = new ProcessStartInfo("cmd.exe", $"/c git {command}")
            {
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null) return "git プロセスの起動に失敗しました。";

                string output = process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return string.IsNullOrWhiteSpace(output) ? error : output;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return $"[Git実行エラー] {ex.Message}";
            }
        }
    }
}
