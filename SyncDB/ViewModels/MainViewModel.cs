using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using SyncDB.Core;
using SyncDB.Model;
using SyncDB.Service;

namespace SyncDB.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ConfigService _configService;
        private readonly RcloneService _rcloneService;
        private readonly WatcherService _watcherService;
        private readonly RcloneInstaller _installer;
        private AppConfig _appConfig;

        // ═══ Profile ═══
        private ObservableCollection<SyncProfile> _profiles;
        public ObservableCollection<SyncProfile> Profiles
        {
            get { return _profiles; }
            set { SetProperty(ref _profiles, value); }
        }

        private SyncProfile _selectedProfile;
        public SyncProfile SelectedProfile
        {
            get { return _selectedProfile; }
            set
            {
                if (SetProperty(ref _selectedProfile, value))
                    RefreshProfileBindings();
            }
        }

        // ═══ Sync Tab ═══
        private string _backupPath = "";
        public string BackupPath
        {
            get { return _backupPath; }
            set { if (SetProperty(ref _backupPath, value) && SelectedProfile != null) SelectedProfile.BackupPath = value; }
        }

        private string _remotePath = "";
        public string RemotePath
        {
            get { return _remotePath; }
            set { if (SetProperty(ref _remotePath, value) && SelectedProfile != null) SelectedProfile.RemotePath = value; }
        }

        private int _selectedSyncMode;
        public int SelectedSyncMode
        {
            get { return _selectedSyncMode; }
            set
            {
                if (SetProperty(ref _selectedSyncMode, value) && SelectedProfile != null)
                    SelectedProfile.SyncMode = (RcloneSyncMode)value;
            }
        }

        // ═══ Options Tab ═══
        private bool _ignoreExisting;
        public bool IgnoreExisting
        {
            get { return _ignoreExisting; }
            set { if (SetProperty(ref _ignoreExisting, value) && SelectedProfile != null) SelectedProfile.IgnoreExisting = value; }
        }

        private bool _dryRun;
        public bool DryRun
        {
            get { return _dryRun; }
            set { if (SetProperty(ref _dryRun, value) && SelectedProfile != null) SelectedProfile.DryRun = value; }
        }

        private int _transfers = 4;
        public int Transfers
        {
            get { return _transfers; }
            set { if (SetProperty(ref _transfers, value) && SelectedProfile != null) SelectedProfile.Transfers = value; }
        }

        private int _checkers = 8;
        public int Checkers
        {
            get { return _checkers; }
            set { if (SetProperty(ref _checkers, value) && SelectedProfile != null) SelectedProfile.Checkers = value; }
        }

        private string _bandwidthLimit = "";
        public string BandwidthLimit
        {
            get { return _bandwidthLimit; }
            set { if (SetProperty(ref _bandwidthLimit, value) && SelectedProfile != null) SelectedProfile.BandwidthLimit = value; }
        }

        private int _selectedLogLevel = 1;
        public int SelectedLogLevel
        {
            get { return _selectedLogLevel; }
            set
            {
                if (SetProperty(ref _selectedLogLevel, value) && SelectedProfile != null)
                    SelectedProfile.LogLevel = LogLevels[value];
            }
        }

        private string _extraFlags = "";
        public string ExtraFlags
        {
            get { return _extraFlags; }
            set { if (SetProperty(ref _extraFlags, value) && SelectedProfile != null) SelectedProfile.ExtraFlags = value; }
        }

        // ═══ Watch ═══
        private bool _watchEnabled;
        public bool WatchEnabled
        {
            get { return _watchEnabled; }
            set { if (SetProperty(ref _watchEnabled, value) && SelectedProfile != null) SelectedProfile.WatchEnabled = value; }
        }

        private int _debounceSec = 15;
        public int DebounceSec
        {
            get { return _debounceSec; }
            set { if (SetProperty(ref _debounceSec, value) && SelectedProfile != null) SelectedProfile.DebounceSec = value; }
        }

        private string _fileFilter = "*.bak;*.txt";
        public string FileFilter
        {
            get { return _fileFilter; }
            set { if (SetProperty(ref _fileFilter, value) && SelectedProfile != null) SelectedProfile.FileFilter = value; }
        }

        private int _fileLockTimeoutSec = 60;
        public int FileLockTimeoutSec
        {
            get { return _fileLockTimeoutSec; }
            set { if (SetProperty(ref _fileLockTimeoutSec, value) && SelectedProfile != null) SelectedProfile.FileLockTimeoutSec = value; }
        }

        private int _fileLockRetryMs = 500;
        public int FileLockRetryMs
        {
            get { return _fileLockRetryMs; }
            set { if (SetProperty(ref _fileLockRetryMs, value) && SelectedProfile != null) SelectedProfile.FileLockRetryMs = value; }
        }

        private bool _syncOnStartWatch = true;
        public bool SyncOnStartWatch
        {
            get { return _syncOnStartWatch; }
            set { if (SetProperty(ref _syncOnStartWatch, value) && SelectedProfile != null) SelectedProfile.SyncOnStartWatch = value; }
        }

        // ═══ Settings Tab — Rclone ═══
        private string _rclonePath = "";
        public string RclonePath
        {
            get { return _rclonePath; }
            set { SetProperty(ref _rclonePath, value); }
        }

        private string _rcloneCurrentVersion = "chưa kiểm tra";
        public string RcloneCurrentVersion
        {
            get { return _rcloneCurrentVersion; }
            set { SetProperty(ref _rcloneCurrentVersion, value); }
        }

        private string _rcloneLatestVersion = "chưa kiểm tra";
        public string RcloneLatestVersion
        {
            get { return _rcloneLatestVersion; }
            set { SetProperty(ref _rcloneLatestVersion, value); }
        }

        private bool _isInstalling;
        public bool IsInstalling
        {
            get { return _isInstalling; }
            set { SetProperty(ref _isInstalling, value); }
        }

        private int _installProgress;
        public int InstallProgress
        {
            get { return _installProgress; }
            set { SetProperty(ref _installProgress, value); }
        }

        private string _installStatusText = "";
        public string InstallStatusText
        {
            get { return _installStatusText; }
            set { SetProperty(ref _installStatusText, value); }
        }

        // ═══ Settings Tab — App ═══
        private bool _minimizeToTray;
        public bool MinimizeToTray
        {
            get { return _minimizeToTray; }
            set { SetProperty(ref _minimizeToTray, value); }
        }

        private bool _startMinimized;
        public bool StartMinimized
        {
            get { return _startMinimized; }
            set { SetProperty(ref _startMinimized, value); }
        }

        private bool _runOnStartup;
        public bool RunOnStartup
        {
            get { return _runOnStartup; }
            set { SetProperty(ref _runOnStartup, value); }
        }

        // ═══ Status ═══
        private bool _isRunning;
        public bool IsRunning
        {
            get { return _isRunning; }
            set { SetProperty(ref _isRunning, value); }
        }

        private bool _isWatching;
        public bool IsWatching
        {
            get { return _isWatching; }
            set { SetProperty(ref _isWatching, value); }
        }

        private string _statusText = "Sẵn sàng";
        public string StatusText
        {
            get { return _statusText; }
            set { SetProperty(ref _statusText, value); }
        }

        private string _lastSyncTime = "—";
        public string LastSyncTime
        {
            get { return _lastSyncTime; }
            set { SetProperty(ref _lastSyncTime, value); }
        }

        // ═══ Log ═══
        private ObservableCollection<string> _logEntries = new ObservableCollection<string>();
        public ObservableCollection<string> LogEntries
        {
            get { return _logEntries; }
            set { SetProperty(ref _logEntries, value); }
        }

        // ═══ Lookup data ═══
        public List<string> SyncModes { get; } = new List<string> { "Copy", "Sync", "Move" };
        public List<string> LogLevels { get; } = new List<string> { "DEBUG", "INFO", "NOTICE", "ERROR" };

        // ═══ Commands ═══
        public ICommand BrowseCommand { get; }
        public ICommand TestConnectionCommand { get; }
        public ICommand RunSyncCommand { get; }
        public ICommand CancelSyncCommand { get; }
        public ICommand StartWatchCommand { get; }
        public ICommand StopWatchCommand { get; }
        public ICommand AddProfileCommand { get; }
        public ICommand DeleteProfileCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand SaveConfigCommand { get; }
        public ICommand BrowseRcloneCommand { get; }
        public ICommand CheckVersionCommand { get; }
        public ICommand InstallRcloneCommand { get; }
        public ICommand OpenRcloneConfigCommand { get; }
        public ICommand SaveAppSettingsCommand { get; }
        public ICommand LoadRemotesCommand { get; }
        public ICommand LoadBackupLogCommand { get; }

        // ═══ Rclone Remotes ═══
        private ObservableCollection<string> _rcloneRemotes = new ObservableCollection<string>();
        public ObservableCollection<string> RcloneRemotes
        {
            get { return _rcloneRemotes; }
            set { SetProperty(ref _rcloneRemotes, value); }
        }

        private string _selectedRemote;
        public string SelectedRemote
        {
            get { return _selectedRemote; }
            set
            {
                if (SetProperty(ref _selectedRemote, value) && !string.IsNullOrEmpty(value))
                    RemotePath = value;
            }
        }

        // ═══ Backup Log Viewer ═══
        private string _backupLogContent = "(Nhấn Làm mới để tải log)";
        public string BackupLogContent
        {
            get { return _backupLogContent; }
            set { SetProperty(ref _backupLogContent, value); }
        }

        private bool _isLoadingLog;
        public bool IsLoadingLog
        {
            get { return _isLoadingLog; }
            set { SetProperty(ref _isLoadingLog, value); }
        }

        private ObservableCollection<string> _logFiles = new ObservableCollection<string>();
        public ObservableCollection<string> LogFiles
        {
            get { return _logFiles; }
            set { SetProperty(ref _logFiles, value); }
        }

        private string _selectedLogFile;
        public string SelectedLogFile
        {
            get { return _selectedLogFile; }
            set { SetProperty(ref _selectedLogFile, value); }
        }

        private readonly Dispatcher _dispatcher;
        private CancellationTokenSource _installCts;

        public MainViewModel()
        {
            _dispatcher = Application.Current.Dispatcher;

            var appPath = AppDomain.CurrentDomain.BaseDirectory;
            _configService = new ConfigService(appPath);
            _installer = new RcloneInstaller();

            _appConfig = _configService.Load();
            _rcloneService = new RcloneService(appPath, _appConfig.RclonePath);
            _watcherService = new WatcherService();

            _rcloneService.OutputReceived += OnOutputReceived;
            _rcloneService.ProcessExited += code =>
            {
                _dispatcher.BeginInvoke(new Action(() =>
                {
                    IsRunning = false;
                    LastSyncTime = DateTime.Now.ToString("HH:mm:ss");
                    StatusText = code == 0 ? "✔ Đồng bộ thành công" : "✖ Lỗi (code " + code + ")";
                }));
            };

            _watcherService.FileDetected += file =>
            {
                _dispatcher.BeginInvoke(new Action(() =>
                    AddLog("📁 Phát hiện file: " + System.IO.Path.GetFileName(file))));
            };
            _watcherService.LogMessage += msg =>
            {
                _dispatcher.BeginInvoke(new Action(() => AddLog(msg)));
            };
            _watcherService.DebounceTrigger += () =>
            {
                _dispatcher.BeginInvoke(new Action(async () =>
                {
                    if (!IsRunning && SelectedProfile != null)
                    {
                        AddLog("🔄 Auto sync triggered...");
                        await DoRunSyncAsync();
                    }
                }));
            };

            BrowseCommand = new RelayCommand(BrowseFolder);
            TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync(), () => !IsRunning);
            RunSyncCommand = new RelayCommand(async () => await DoRunSyncAsync(), () => !IsRunning);
            CancelSyncCommand = new RelayCommand(CancelSync, () => IsRunning);
            StartWatchCommand = new RelayCommand(async () => await StartWatch(), () => !IsWatching);
            StopWatchCommand = new RelayCommand(StopWatch, () => IsWatching);
            AddProfileCommand = new RelayCommand(AddProfile);
            DeleteProfileCommand = new RelayCommand(DeleteProfile, () => Profiles != null && Profiles.Count > 1);
            ClearLogCommand = new RelayCommand(() => LogEntries.Clear());
            SaveConfigCommand = new RelayCommand(SaveConfig);
            BrowseRcloneCommand = new RelayCommand(BrowseRclone);
            CheckVersionCommand = new RelayCommand(async () => await CheckVersionAsync(), () => !IsInstalling);
            InstallRcloneCommand = new RelayCommand(async () => await InstallRcloneAsync(), () => !IsInstalling);
            OpenRcloneConfigCommand = new RelayCommand(() => _rcloneService.OpenConfig(), () => !IsRunning);
            SaveAppSettingsCommand = new RelayCommand(SaveAppSettings);
            LoadRemotesCommand = new RelayCommand(async () => await LoadRemotesAsync());
            LoadBackupLogCommand = new RelayCommand(async () => await LoadBackupLogAsync());

            LoadConfig();

            AddLog("SyncDB v2.0 khởi động");
            if (!_rcloneService.RcloneExists())
                AddLog("⚠ rclone.exe không tìm thấy — vào tab Cài đặt để tải về");
            else
                Task.Run(async () => await LoadRemotesAsync());
        }

        // ═══ Load / Save ═══
        private void LoadConfig()
        {
            Profiles = new ObservableCollection<SyncProfile>(_appConfig.Profiles);
            if (Profiles.Count == 0)
                Profiles.Add(new SyncProfile());

            var idx = Math.Max(0, Math.Min(_appConfig.ActiveProfileIndex, Profiles.Count - 1));
            SelectedProfile = Profiles[idx];

            RclonePath = _appConfig.RclonePath ?? "";
            MinimizeToTray = _appConfig.MinimizeToTray;
            StartMinimized = _appConfig.StartMinimized;
            RunOnStartup = _appConfig.RunOnStartup;
        }

        private void SaveConfig()
        {
            _appConfig.Profiles = Profiles.ToList();
            _appConfig.ActiveProfileIndex = Profiles.IndexOf(SelectedProfile);
            _configService.Save(_appConfig);
            AddLog("💾 Config đã lưu");
        }

        private void SaveAppSettings()
        {
            _appConfig.RclonePath = RclonePath;
            _appConfig.MinimizeToTray = MinimizeToTray;
            _appConfig.StartMinimized = StartMinimized;
            _appConfig.RunOnStartup = RunOnStartup;
            _configService.Save(_appConfig);
            _rcloneService.UpdateRclonePath(RclonePath);
            ApplyRunOnStartup(RunOnStartup);
            AddLog("✔ Đã lưu cài đặt ứng dụng");
        }

        private void ApplyRunOnStartup(bool enable)
        {
            const string regKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            const string appName = "SyncDB";
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(regKey, writable: true))
                {
                    if (key == null) return;
                    if (enable)
                        key.SetValue(appName, "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"");
                    else
                        key.DeleteValue(appName, throwOnMissingValue: false);
                }
            }
            catch (Exception ex) { AddLog("⚠ Registry startup: " + ex.Message); }
        }

        // ═══ Profile ═══
        private void RefreshProfileBindings()
        {
            if (SelectedProfile == null) return;
            var p = SelectedProfile;

            BackupPath = p.BackupPath;
            RemotePath = p.RemotePath;
            SelectedSyncMode = (int)p.SyncMode;
            IgnoreExisting = p.IgnoreExisting;
            DryRun = p.DryRun;
            Transfers = p.Transfers;
            Checkers = p.Checkers;
            BandwidthLimit = p.BandwidthLimit;
            SelectedLogLevel = LogLevels.IndexOf(p.LogLevel);
            if (SelectedLogLevel < 0) SelectedLogLevel = 1;
            ExtraFlags = p.ExtraFlags;
            WatchEnabled = p.WatchEnabled;
            DebounceSec = p.DebounceSec;
            FileFilter = p.FileFilter;
            FileLockTimeoutSec = p.FileLockTimeoutSec;
            FileLockRetryMs = p.FileLockRetryMs;
            SyncOnStartWatch = p.SyncOnStartWatch;
        }

        private void AddProfile()
        {
            var profile = new SyncProfile { Name = "Profile " + (Profiles.Count + 1) };
            Profiles.Add(profile);
            SelectedProfile = profile;
            AddLog("➕ Tạo profile mới: " + profile.Name);
        }

        private void DeleteProfile()
        {
            if (Profiles.Count <= 1) return;
            var name = SelectedProfile.Name;
            Profiles.Remove(SelectedProfile);
            SelectedProfile = Profiles[0];
            AddLog("🗑 Xóa profile: " + name);
        }

        // ═══ Actions ═══
        private void BrowseFolder()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Chọn thư mục backup",
                ShowNewFolderButton = false
            };
            if (!string.IsNullOrEmpty(BackupPath) && System.IO.Directory.Exists(BackupPath))
                dialog.SelectedPath = BackupPath;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                BackupPath = dialog.SelectedPath;
        }

        private void BrowseRclone()
        {
            var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Chọn rclone.exe",
                Filter = "rclone.exe|rclone.exe|Executable (*.exe)|*.exe",
                CheckFileExists = true
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                RclonePath = dialog.FileName;
        }

        private async Task CheckVersionAsync()
        {
            IsInstalling = true;
            InstallStatusText = "Đang kiểm tra...";
            try
            {
                RcloneCurrentVersion = await _rcloneService.GetVersionAsync();
                InstallStatusText = "Đang lấy version mới nhất...";
                RcloneLatestVersion = await _installer.GetLatestVersionAsync();
                InstallStatusText = "";
                AddLog($"📋 Hiện tại: {RcloneCurrentVersion} | Mới nhất: {RcloneLatestVersion}");
            }
            finally { IsInstalling = false; }
        }

        private async Task InstallRcloneAsync()
        {
            IsInstalling = true;
            InstallProgress = 0;
            _installCts = new CancellationTokenSource();

            var progress = new Progress<Tuple<int, string>>(p =>
            {
                _dispatcher.BeginInvoke(new Action(() =>
                {
                    InstallProgress = p.Item1;
                    InstallStatusText = p.Item2;
                }));
            });

            try
            {
                var destDir = AppDomain.CurrentDomain.BaseDirectory;
                var result = await _installer.DownloadAndInstallAsync(destDir, progress, _installCts.Token);
                if (result.Item1)
                {
                    AddLog("✔ Cài xong: " + result.Item2);
                    RclonePath = "";
                    _rcloneService.UpdateRclonePath("");
                    RcloneCurrentVersion = await _rcloneService.GetVersionAsync();
                }
                else
                {
                    AddLog("✖ Cài rclone thất bại: " + result.Item2);
                }
            }
            finally { IsInstalling = false; _installCts = null; }
        }

        private async Task TestConnectionAsync()
        {
            if (string.IsNullOrWhiteSpace(RemotePath))
            {
                AddLog("⚠ Remote path không được trống");
                return;
            }
            StatusText = "🔌 Đang test kết nối...";
            AddLog("🔌 Test kết nối: " + RemotePath);
            var result = await _rcloneService.TestConnectionAsync(RemotePath);
            if (result.Item1)
            {
                StatusText = "✔ Kết nối thành công";
                AddLog("✔ " + result.Item2);
            }
            else
            {
                StatusText = "✖ Kết nối thất bại";
                AddLog("✖ " + result.Item2);
            }
        }

        private async Task DoRunSyncAsync()
        {
            if (IsRunning || SelectedProfile == null) return;
            if (string.IsNullOrWhiteSpace(BackupPath) || !System.IO.Directory.Exists(BackupPath))
            {
                AddLog("⚠ Đường dẫn backup không hợp lệ");
                return;
            }
            if (string.IsNullOrWhiteSpace(RemotePath))
            {
                AddLog("⚠ Remote path không được trống");
                return;
            }
            IsRunning = true;
            StatusText = "🔄 Đang đồng bộ...";
            SaveConfig();
            await _rcloneService.RunSyncAsync(SelectedProfile);
        }

        private void CancelSync()
        {
            _rcloneService.Cancel();
            IsRunning = false;
            StatusText = "⚠ Đã hủy";
            AddLog("⚠ Sync đã bị hủy");
        }

        private async Task StartWatch()
        {
            if (string.IsNullOrWhiteSpace(BackupPath) || !System.IO.Directory.Exists(BackupPath))
            {
                AddLog("⚠ Đường dẫn backup không hợp lệ để watch");
                return;
            }
            _watcherService.Start(BackupPath, FileFilter, DebounceSec, FileLockTimeoutSec, FileLockRetryMs);
            IsWatching = true;
            StatusText = "👁 Đang theo dõi thư mục...";
            SaveConfig();
            AddLog($"👁 Bắt đầu watch: {BackupPath} | Debounce: {DebounceSec}s | Lock timeout: {FileLockTimeoutSec}s | Retry: {FileLockRetryMs}ms");

            if (SyncOnStartWatch)
            {
                AddLog("🔄 Đồng bộ tất cả file hiện có trước khi watch...");
                await DoRunSyncAsync();
            }
        }

        private void StopWatch()
        {
            _watcherService.Stop();
            IsWatching = false;
            StatusText = "Sẵn sàng";
            AddLog("⏹ Dừng watch");
        }

        private void OnOutputReceived(string msg)
        {
            _dispatcher.BeginInvoke(new Action(() => AddLog(msg)));
        }

        private void AddLog(string msg)
        {
            var entry = DateTime.Now.ToString("HH:mm:ss") + "  " + msg;
            LogEntries.Add(entry);
            while (LogEntries.Count > 1000)
                LogEntries.RemoveAt(0);
        }

        private async Task LoadRemotesAsync()
        {
            AddLog("🔄 Đang tải danh sách remote...");
            var remotes = await _rcloneService.ListRemotesAsync();
            RcloneRemotes = new ObservableCollection<string>(remotes);
            if (remotes.Count == 0)
                AddLog("⚠ Không tìm thấy remote nào — hãy mở rclone config để cài đặt");
            else
                AddLog($"✔ Tìm thấy {remotes.Count} remote: {string.Join(", ", remotes)}");
        }

        private async Task LoadBackupLogAsync()
        {
            IsLoadingLog = true;
            try
            {
                var logDir = _rcloneService.LogDirectory;

                // Scan tất cả file log trong thư mục
                var files = await Task.Run(() =>
                {
                    if (!Directory.Exists(logDir)) return new List<string>();
                    return Directory.GetFiles(logDir, "rclone_*.log")
                        .OrderByDescending(f => f)
                        .Select(f => Path.GetFileName(f))
                        .ToList();
                });

                var prevSelected = SelectedLogFile;
                LogFiles = new ObservableCollection<string>(files);

                // Chọn file: giữ nguyên nếu vẫn tồn tại, không thì chọn mới nhất
                if (files.Contains(prevSelected))
                    SelectedLogFile = prevSelected;
                else
                    SelectedLogFile = files.FirstOrDefault();

                if (SelectedLogFile == null)
                {
                    BackupLogContent = $"Chưa có file log.\nThư mục kiểm tra: {logDir}";
                    return;
                }

                var logPath = Path.Combine(logDir, SelectedLogFile);
                var content = await Task.Run(() =>
                {
                    var lines = new List<string>();
                    using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                            if (lines.Count > 5000) lines.RemoveAt(0);
                        }
                    }
                    return string.Join("\n", lines);
                });
                BackupLogContent = content;
            }
            catch (Exception ex) { BackupLogContent = "Lỗi đọc log: " + ex.Message; }
            finally { IsLoadingLog = false; }
        }

        public void OnClosing()
        {
            _watcherService.Dispose();
            _rcloneService.Cancel();
            SaveConfig();
        }
    }
}
