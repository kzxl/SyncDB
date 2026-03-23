using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SyncDB.Model
{
    public class AppConfig
    {
        public List<SyncProfile> Profiles { get; set; } = new List<SyncProfile> { new SyncProfile() };
        public int ActiveProfileIndex { get; set; }
        public bool MinimizeToTray { get; set; }
        public bool StartMinimized { get; set; }
        public bool AutoStartWatch { get; set; }
    }

    public class SyncProfile
    {
        public string Name { get; set; } = "Default";
        public string BackupPath { get; set; } = "";
        public string RemotePath { get; set; } = "";

        [JsonConverter(typeof(StringEnumConverter))]
        public RcloneSyncMode SyncMode { get; set; } = RcloneSyncMode.Copy;

        // Performance
        public int Transfers { get; set; } = 4;
        public int Checkers { get; set; } = 8;
        public string BandwidthLimit { get; set; } = "";

        // Behavior
        public bool IgnoreExisting { get; set; }
        public bool DryRun { get; set; }
        public string LogLevel { get; set; } = "INFO";
        public string ExtraFlags { get; set; } = "";

        // Watch
        public bool WatchEnabled { get; set; }
        public int DebounceSec { get; set; } = 15;
        public string FileFilter { get; set; } = "*.bak;*.txt";

        // Schedule
        public bool ScheduleEnabled { get; set; }
        public int ScheduleIntervalMin { get; set; } = 60;
    }

    public enum RcloneSyncMode
    {
        Copy,
        Sync,
        Move
    }
}
