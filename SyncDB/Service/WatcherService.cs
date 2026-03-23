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
        private HashSet<string> _filters;

        public event Action<string> FileDetected;
        public event Action DebounceTrigger;

        public bool IsWatching => _watcher != null;

        public void Start(string path, string filterPattern, int debounceSec)
        {
            Stop();

            if (!Directory.Exists(path)) return;

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

            if (!IsFileReady(e.FullPath)) return;

            _lastDetectedFile = e.FullPath;
            _debounceTimer?.Stop();
            _debounceTimer?.Start();
        }

        private void OnDebounceElapsed(object sender, ElapsedEventArgs e)
        {
            if (_lastDetectedFile != null)
                FileDetected?.Invoke(_lastDetectedFile);

            _lastDetectedFile = null;
            DebounceTrigger?.Invoke();
        }

        private static bool IsFileReady(string path)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                        return true;
                }
                catch
                {
                    Thread.Sleep(200);
                }
            }
            return false;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
