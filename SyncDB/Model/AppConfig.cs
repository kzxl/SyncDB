using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncDB.Model
{
    public class AppConfig
    {
        public string BackupPath { get; set; }
        public string RemotePath { get; set; }
        public bool IgnoreExisting { get; set; }
    }
}
