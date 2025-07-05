using System;
using System.Diagnostics;

namespace GitManagerApp.Services
{
    public class GitResult
    {
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public int ExitCode { get; set; }
        public bool IsSuccess => ExitCode == 0;
        public string FullMessage => string.IsNullOrWhiteSpace(Output) ? Error : Output;
    }

    public static class GitExecutor
    {
        public static string Run(string command, string workingDir, out string error)
        {
            var result = RunWithResult(command, workingDir);
            error = result.Error;
            return result.FullMessage;
        }

        public static GitResult RunWithResult(string command, string workingDir)
        {
            var result = new GitResult();
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
                if (process == null)
                {
                    result.Error = "git プロセスの起動に失敗しました。";
                    result.ExitCode = -1;
                    return result;
                }

                result.Output = process.StandardOutput.ReadToEnd();
                result.Error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                result.ExitCode = process.ExitCode;

                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                result.ExitCode = -1;
                return result;
            }
        }
    }
}
