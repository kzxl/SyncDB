using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SyncDB.Model;

namespace SyncDB.Service
{
    public class RcloneService
    {
        private string _rcloneExe;
        private readonly string _appPath;
        private readonly string _logDir;
        private Process _currentProcess;
        private CancellationTokenSource _cts;

        public event Action<string> OutputReceived;
        public event Action<int> ProcessExited;

        public bool IsRunning => _currentProcess != null && !_currentProcess.HasExited;
        public string RcloneExePath => _rcloneExe;

        public RcloneService(string appPath, string customRclonePath = null)
        {
            _appPath = appPath;
            _logDir = Path.Combine(appPath, "logs");
            UpdateRclonePath(customRclonePath);
        }

        public void UpdateRclonePath(string customPath)
        {
            _rcloneExe = !string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath)
                ? customPath
                : Path.Combine(_appPath, "rclone.exe");
        }

        public bool RcloneExists() => File.Exists(_rcloneExe);

        public async Task<string> GetVersionAsync()
        {
            if (!RcloneExists()) return "rclone.exe không tìm thấy";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _rcloneExe,
                    Arguments = "version",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var p = Process.Start(psi))
                {
                    if (p == null) return "Không thể chạy rclone";
                    var output = await Task.Run(() => p.StandardOutput.ReadToEnd());
                    p.WaitForExit(5000);
                    // Lấy dòng đầu tiên: "rclone v1.68.2"
                    var line = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    return line.Length > 0 ? line[0].Trim() : "unknown";
                }
            }
            catch (Exception ex) { return "Lỗi: " + ex.Message; }
        }

        public void OpenConfig()
        {
            if (!RcloneExists())
            {
                OutputReceived?.Invoke("✖ rclone.exe không tìm thấy");
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k \"{_rcloneExe}\" config",
                    UseShellExecute = true
                });
            }
            catch (Exception ex) { OutputReceived?.Invoke("✖ Lỗi mở config: " + ex.Message); }
        }

        /// <summary>Trả về danh sách remote đã cài (vd: "ggdrive:", "onedrive:")</summary>
        public async Task<List<string>> ListRemotesAsync()
        {
            var result = new List<string>();
            if (!RcloneExists()) return result;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _rcloneExe,
                    Arguments = "listremotes",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var p = Process.Start(psi))
                {
                    if (p == null) return result;
                    var output = await Task.Run(() => p.StandardOutput.ReadToEnd());
                    p.WaitForExit(5000);
                    foreach (var line in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var remote = line.Trim();
                        if (!string.IsNullOrEmpty(remote))
                            result.Add(remote);
                    }
                }
            }
            catch { }
            return result;
        }

        public async Task<Tuple<bool, string>> TestConnectionAsync(string remotePath)
        {
            if (!RcloneExists())
                return Tuple.Create(false, "rclone.exe không tìm thấy: " + _rcloneExe);

            if (string.IsNullOrWhiteSpace(remotePath))
                return Tuple.Create(false, "Remote path không được trống");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _rcloneExe,
                    Arguments = "lsd " + remotePath,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var p = Process.Start(psi))
                {
                    if (p == null) return Tuple.Create(false, "Không thể start rclone");

                    var exited = await Task.Run(() => p.WaitForExit(10000));
                    if (!exited)
                    {
                        try { p.Kill(); } catch { }
                        return Tuple.Create(false, "Kết nối timeout (10s)");
                    }

                    var output = p.StandardOutput.ReadToEnd();
                    var error = p.StandardError.ReadToEnd();

                    return p.ExitCode == 0
                        ? Tuple.Create(true, string.IsNullOrWhiteSpace(output) ? "Kết nối thành công!" : output)
                        : Tuple.Create(false, error);
                }
            }
            catch (Exception ex)
            {
                return Tuple.Create(false, ex.Message);
            }
        }

        public async Task RunSyncAsync(SyncProfile profile)
        {
            if (!RcloneExists())
            {
                OutputReceived?.Invoke("✖ rclone.exe không tìm thấy: " + _rcloneExe);
                return;
            }

            _cts = new CancellationTokenSource();

            try
            {
                if (!Directory.Exists(_logDir))
                    Directory.CreateDirectory(_logDir);

                var logFile = GetDailyLogPath();

                // Ghi header log
                File.AppendAllText(logFile,
                    "\r\n===== RUN " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " =====\r\n");

                var args = BuildArguments(profile, logFile);
                OutputReceived?.Invoke("▶ rclone " + args);

                var psi = new ProcessStartInfo
                {
                    FileName = _rcloneExe,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(_rcloneExe)
                };

                _currentProcess = Process.Start(psi);
                if (_currentProcess == null)
                {
                    OutputReceived?.Invoke("✖ Không thể start rclone process");
                    return;
                }

                // Stream output real-time
                _currentProcess.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null) OutputReceived?.Invoke(e.Data);
                };
                _currentProcess.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null) OutputReceived?.Invoke(e.Data);
                };
                _currentProcess.BeginOutputReadLine();
                _currentProcess.BeginErrorReadLine();

                await Task.Run(() => _currentProcess.WaitForExit());

                var exitCode = _currentProcess.ExitCode;
                ProcessExited?.Invoke(exitCode);

                OutputReceived?.Invoke(exitCode == 0
                    ? "✔ Hoàn thành (exit code 0)"
                    : "✖ Kết thúc với exit code " + exitCode);

                // Ghi log kết quả
                AppendLog("Rclone exit code: " + exitCode);
            }
            catch (OperationCanceledException)
            {
                OutputReceived?.Invoke("⚠ Đã hủy bởi người dùng");
            }
            catch (Exception ex)
            {
                OutputReceived?.Invoke("✖ ERROR: " + ex.Message);
                AppendLog("ERROR: " + ex.Message);
            }
            finally
            {
                _currentProcess = null;
                _cts = null;
            }
        }

        public void Cancel()
        {
            try
            {
                _cts?.Cancel();
                if (_currentProcess != null && !_currentProcess.HasExited)
                    _currentProcess.Kill();
            }
            catch { }
        }

        private string BuildArguments(SyncProfile profile, string logFile)
        {
            var sb = new StringBuilder();
            sb.Append(profile.SyncMode.ToString().ToLower());
            sb.AppendFormat(" \"{0}\" {1}", profile.BackupPath, profile.RemotePath);

            if (profile.IgnoreExisting) sb.Append(" --ignore-existing");
            if (profile.DryRun) sb.Append(" --dry-run");
            if (profile.Transfers > 0) sb.AppendFormat(" --transfers {0}", profile.Transfers);
            if (profile.Checkers > 0) sb.AppendFormat(" --checkers {0}", profile.Checkers);
            if (!string.IsNullOrWhiteSpace(profile.BandwidthLimit))
                sb.AppendFormat(" --bwlimit {0}", profile.BandwidthLimit);

            sb.AppendFormat(" --log-file=\"{0}\" --log-level {1}", logFile, profile.LogLevel);
            sb.Append(" --stats 1s --stats-log-level NOTICE");

            if (!string.IsNullOrWhiteSpace(profile.ExtraFlags))
                sb.Append(" " + profile.ExtraFlags.Trim());

            return sb.ToString();
        }

        private void AppendLog(string msg)
        {
            try
            {
                if (!Directory.Exists(_logDir))
                    Directory.CreateDirectory(_logDir);

                File.AppendAllText(
                    Path.Combine(_logDir, "app.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + msg + "\r\n");
            }
            catch { }
        }

        public string GetDailyLogPath()
        {
            if (!Directory.Exists(_logDir))
                Directory.CreateDirectory(_logDir);
            return Path.Combine(_logDir, "rclone_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
        }

        public string LogDirectory => _logDir;
    }
}
