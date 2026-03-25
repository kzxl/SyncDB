using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;

namespace SyncDB.Service
{
    public class WatcherService : IDisposable
    {
        private FileSystemWatcher _watcher;
        private System.Timers.Timer _debounceTimer;
        private string _lastDetectedFile;
        private DateTime _lastDetectTime;
        private HashSet<string> _filters;

        private int _lockTimeoutSec = 60;
        private int _lockRetryMs = 500;

        public event Action<string> FileDetected;
        public event Action DebounceTrigger;
        public event Action<string> LogMessage; // để gửi timing log về UI

        public bool IsWatching => _watcher != null;

        public void Start(string path, string filterPattern, int debounceSec,
                          int lockTimeoutSec = 60, int lockRetryMs = 500)
        {
            Stop();

            if (!Directory.Exists(path)) return;

            _lockTimeoutSec = lockTimeoutSec;
            _lockRetryMs = lockRetryMs;

            _filters = new HashSet<string>(
                filterPattern
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim().TrimStart('*').ToLower()),
                StringComparer.OrdinalIgnoreCase);

            _watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.DirectoryName
            };

            _watcher.Created += OnChanged;
            _watcher.Changed += OnChanged;

            _debounceTimer = new System.Timers.Timer(debounceSec * 1000);
            _debounceTimer.AutoReset = false;
            _debounceTimer.Elapsed += OnDebounceElapsed;

            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            if (_debounceTimer != null)
            {
                _debounceTimer.Stop();
                _debounceTimer.Elapsed -= OnDebounceElapsed;
                _debounceTimer.Dispose();
                _debounceTimer = null;
            }

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnChanged;
                _watcher.Changed -= OnChanged;
                _watcher.Dispose();
                _watcher = null;
            }

            _lastDetectedFile = null;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            var ext = Path.GetExtension(e.FullPath).ToLower();
            if (_filters != null && _filters.Count > 0 && !_filters.Contains(ext))
                return;

            var detectTime = DateTime.Now;
            LogMessage?.Invoke($"📁 [{detectTime:HH:mm:ss}] Phát hiện: {Path.GetFileName(e.FullPath)} — kiểm tra file lock...");

            // Chờ file không còn bị lock (configurable timeout)
            bool ready = WaitUntilFileReady(e.FullPath, detectTime);
            if (!ready)
            {
                LogMessage?.Invoke($"⚠ [{DateTime.Now:HH:mm:ss}] Bỏ qua — file vẫn bị lock sau {_lockTimeoutSec}s: {Path.GetFileName(e.FullPath)}");
                return;
            }

            var readyTime = DateTime.Now;
            var waitSec = (readyTime - detectTime).TotalSeconds;
            LogMessage?.Invoke($"✔ [{readyTime:HH:mm:ss}] File sẵn sàng (chờ {waitSec:F1}s) — reset debounce timer {_debounceTimer?.Interval / 1000}s");

            _lastDetectedFile = e.FullPath;
            _lastDetectTime = detectTime;
            _debounceTimer?.Stop();
            _debounceTimer?.Start();
        }

        private void OnDebounceElapsed(object sender, ElapsedEventArgs e)
        {
            var triggerTime = DateTime.Now;
            if (_lastDetectedFile != null)
            {
                FileDetected?.Invoke(_lastDetectedFile);
                var totalSec = (triggerTime - _lastDetectTime).TotalSeconds;
                LogMessage?.Invoke($"⚡ [{triggerTime:HH:mm:ss}] Trigger sync — tổng thời gian từ detect: {totalSec:F1}s");
            }

            _lastDetectedFile = null;
            DebounceTrigger?.Invoke();
        }

        /// <summary>
        /// Chờ cho đến khi file không còn bị lock hoặc timeout.
        /// Retry mỗi lockRetryMs, tối đa lockTimeoutSec giây.
        /// </summary>
        private bool WaitUntilFileReady(string path, DateTime startTime)
        {
            while (true)
            {
                try
                {
                    using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                        return true;
                }
                catch
                {
                    if ((DateTime.Now - startTime).TotalSeconds >= _lockTimeoutSec)
                        return false;

                    Thread.Sleep(_lockRetryMs);
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
