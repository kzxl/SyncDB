using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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

        private int _selectedLogLevel = 1; // INFO
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

        private readonly Dispatcher _dispatcher;

        public MainViewModel()
        {
            _dispatcher = Application.Current.Dispatcher;

            var appPath = AppDomain.CurrentDomain.BaseDirectory;
            _configService = new ConfigService(appPath);
            _rcloneService = new RcloneService(appPath);
            _watcherService = new WatcherService();

            // Wire events
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

            // Commands
            BrowseCommand = new RelayCommand(BrowseFolder);
            TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync(), () => !IsRunning);
            RunSyncCommand = new RelayCommand(async () => await DoRunSyncAsync(), () => !IsRunning);
            CancelSyncCommand = new RelayCommand(CancelSync, () => IsRunning);
            StartWatchCommand = new RelayCommand(StartWatch, () => !IsWatching);
            StopWatchCommand = new RelayCommand(StopWatch, () => IsWatching);
            AddProfileCommand = new RelayCommand(AddProfile);
            DeleteProfileCommand = new RelayCommand(DeleteProfile, () => Profiles != null && Profiles.Count > 1);
            ClearLogCommand = new RelayCommand(() => LogEntries.Clear());
            SaveConfigCommand = new RelayCommand(SaveConfig);

            // Load config
            LoadConfig();

            AddLog("SyncDB v2.0 khởi động");
            if (!_rcloneService.RcloneExists())
                AddLog("⚠ rclone.exe không tìm thấy trong thư mục ứng dụng");
        }

        // ═══ Load / Save ═══
        private void LoadConfig()
        {
            _appConfig = _configService.Load();

            Profiles = new ObservableCollection<SyncProfile>(_appConfig.Profiles);
            if (Profiles.Count == 0)
                Profiles.Add(new SyncProfile());

            var idx = Math.Max(0, Math.Min(_appConfig.ActiveProfileIndex, Profiles.Count - 1));
            SelectedProfile = Profiles[idx];
        }

        private void SaveConfig()
        {
            _appConfig.Profiles = Profiles.ToList();
            _appConfig.ActiveProfileIndex = Profiles.IndexOf(SelectedProfile);
            _configService.Save(_appConfig);
            AddLog("💾 Config đã lưu");
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

        private void StartWatch()
        {
            if (string.IsNullOrWhiteSpace(BackupPath) || !System.IO.Directory.Exists(BackupPath))
            {
                AddLog("⚠ Đường dẫn backup không hợp lệ để watch");
                return;
            }

            _watcherService.Start(BackupPath, FileFilter, DebounceSec);
            IsWatching = true;
            StatusText = "👁 Đang theo dõi thư mục...";
            SaveConfig();
            AddLog("👁 Bắt đầu watch: " + BackupPath);
        }

        private void StopWatch()
        {
            _watcherService.Stop();
            IsWatching = false;
            StatusText = "Sẵn sàng";
            AddLog("⏹ Dừng watch");
        }

        // ═══ Log ═══
        private void OnOutputReceived(string msg)
        {
            _dispatcher.BeginInvoke(new Action(() => AddLog(msg)));
        }

        private void AddLog(string msg)
        {
            var entry = DateTime.Now.ToString("HH:mm:ss") + "  " + msg;
            LogEntries.Add(entry);

            // Giới hạn 1000 dòng
            while (LogEntries.Count > 1000)
                LogEntries.RemoveAt(0);
        }

        public void OnClosing()
        {
            _watcherService.Dispose();
            _rcloneService.Cancel();
            SaveConfig();
        }
    }
}
