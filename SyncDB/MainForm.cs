using SyncDB.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SyncDB
{
    public partial class MainForm : Form
    {
        string AppPath = Application.StartupPath;
        string RcloneExe => Path.Combine(AppPath, "rclone.exe");
        string LogDir => Path.Combine(AppPath, "logs");
        string RcloneLog => Path.Combine(LogDir, "rclone.log");
        string ConfigFile => Path.Combine(AppPath, "config.json");

        FileSystemWatcher _watcher;
        Timer _debounceTimer;
        bool _isRunning;
        bool _isWatching;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            EnsureLogDir();
            LoadConfig();
            Log("App started");
        }

        void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigFile)) return;

                var json = File.ReadAllText(ConfigFile);
                var cfg = Newtonsoft.Json.JsonConvert.DeserializeObject<AppConfig>(json);

                txtBackupPath.Text = cfg.BackupPath;
                txtRemotePath.Text = cfg.RemotePath;
                chkIgnoreExisting.Checked = cfg.IgnoreExisting;

                Log("Config loaded");
            }
            catch (Exception ex)
            {
                Log("Load config error: " + ex.Message);
            }
        }


        void StartWatch()
        {
            if (_watcher != null) return;

            _watcher = new FileSystemWatcher(txtBackupPath.Text)
            {
                IncludeSubdirectories = true,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.DirectoryName
            };

            _watcher.Created += OnChanged;
            _watcher.Changed += OnChanged;
            _watcher.EnableRaisingEvents = true;

            _debounceTimer = new Timer { Interval = 15000 };
            _debounceTimer.Tick += async (s, e) =>
            {
                _debounceTimer.Stop();

                if (!string.IsNullOrEmpty(_lastDetectedFile))
                    Log("Detected file: " + _lastDetectedFile);

                _lastDetectedFile = null;

                await RunRcloneAsync();
            };
        }
        string _lastDetectedFile;
        void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnChanged(sender, e)));
                return;
            }
            if (_isRunning) return;

            var ext = Path.GetExtension(e.FullPath).ToLower();
            if (ext != ".bak" && ext != ".txt") return;

            if (!WaitFileReady(e.FullPath)) return;

            _lastDetectedFile = e.FullPath;

            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        bool WaitFileReady(string path)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                        return true;
                }
                catch
                {
                    System.Threading.Thread.Sleep(500);
                }
            }
            return false;
        }

        async Task RunRcloneAsync()
        {
            if (_isRunning) return;

            if (!File.Exists(RcloneExe))
            {
                Log("ERROR: rclone.exe not found: " + RcloneExe);
                MessageBox.Show("Không tìm thấy rclone.exe", "SyncDB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _isRunning = true;

            try
            {
                EnsureLogDir();
                Log("Trigger rclone");

                File.AppendAllText(RcloneLog, "\r\n===== RUN " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " =====\r\n");

                var args = new StringBuilder();
                args.Append($"copy \"{txtBackupPath.Text}\" {txtRemotePath.Text} ");

                if (chkIgnoreExisting.Checked)
                    args.Append("--ignore-existing ");

                args.Append($"--log-file=\"{RcloneLog}\" --log-level INFO");

                var psi = new ProcessStartInfo
                {
                    FileName = RcloneExe,
                    Arguments = args.ToString(),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = AppPath
                };

                using (var p = Process.Start(psi))
                {
                    if (p == null)
                    {
                        Log("ERROR: rclone process start failed");
                        return;
                    }

                    await Task.Run(() => p.WaitForExit());
                    Log("Rclone exit code: " + p.ExitCode);
                }

                Log("Rclone finished");
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
            }
            finally
            {
                _isRunning = false;
            }
        }

        void EnsureLogDir()
        {
            if (!Directory.Exists(LogDir))
                Directory.CreateDirectory(LogDir);
        }

        void Log(string msg)
        {
            File.AppendAllText(
                Path.Combine(LogDir, "app.log"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {msg}\r\n"
            );
        }
        void SaveConfig()
        {
            try
            {
                var cfg = new AppConfig
                {
                    BackupPath = txtBackupPath.Text.Trim(),
                    RemotePath = txtRemotePath.Text.Trim(),
                    IgnoreExisting = chkIgnoreExisting.Checked
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(cfg, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(ConfigFile, json);

                Log("Config saved");
            }
            catch (Exception ex)
            {
                Log("Save config error: " + ex.Message);
            }
        }

        private void btStart_Click(object sender, EventArgs e)
        {
            if (_isWatching) return;

            if (!Directory.Exists(txtBackupPath.Text))
            {
                MessageBox.Show("Đường dẫn backup không tồn tại");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtRemotePath.Text))
            {
                MessageBox.Show("Remote path không được trống");
                return;
            }

            StartWatch();
            _isWatching = true;

            txtBackupPath.Enabled = txtRemotePath.Enabled = chkIgnoreExisting.Enabled = false;
            btTest.Enabled = false;
            btStart.Enabled = false;

            Log("Watcher STARTED");
            SaveConfig();
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            if (!_isWatching) return;

            _watcher?.Dispose();
            _watcher = null;

            _debounceTimer?.Stop();

            _isWatching = false;

            txtBackupPath.Enabled = txtRemotePath.Enabled = chkIgnoreExisting.Enabled = true;
            btTest.Enabled = true;
            btStart.Enabled = true;

            Log("Watcher STOPPED");
        }

        async Task TestRcloneConnectionAsync()
        {
            // check rclone.exe
            if (!File.Exists(RcloneExe))
            {
                MessageBox.Show("Không tìm thấy rclone.exe", "SyncDB",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtRemotePath.Text))
            {
                MessageBox.Show("Remote path không được trống");
                return;
            }

            try
            {
                Log("Test rclone connection");

                var psi = new ProcessStartInfo
                {
                    FileName = RcloneExe,
                    Arguments = $"lsd {txtRemotePath.Text}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var p = Process.Start(psi))
                {
                    if (p == null)
                        throw new Exception("Không thể start rclone");

                    // timeout 10s
                    var exited = await Task.Run(() => p.WaitForExit(10000));

                    if (!exited)
                    {
                        try { p.Kill(); } catch { }
                        throw new TimeoutException("Kết nối timeout");
                    }

                    var output = await p.StandardOutput.ReadToEndAsync();
                    var error = await p.StandardError.ReadToEndAsync();

                    if (p.ExitCode == 0)
                    {
                        Log("Test OK");
                        MessageBox.Show(
                            string.IsNullOrWhiteSpace(output) ? "Kết nối thành công" : output,
                            "Test kết nối",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else
                    {
                        Log("Test FAILED: " + error);
                        MessageBox.Show(
                            "Kết nối thất bại:\r\n" + error,
                            "Test kết nối",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Test ERROR: " + ex.Message);
                MessageBox.Show(ex.Message, "Test kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btTest_Click(object sender, EventArgs e)
        {
            await TestRcloneConnectionAsync();
        }
    }
}
