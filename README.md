# 📦 SyncDB — Rclone Sync Manager

> WPF desktop app (MVVM) hỗ trợ giao diện hiện đại cho **rclone**, giúp đồng bộ backup lên Google Drive / OneDrive / S3 mà không cần thao tác command line.

---

## ✨ Tính năng

| Tính năng | Mô tả |
|-----------|-------|
| 🎨 **Dark Theme UI** | Giao diện WPF hiện đại, dark navy palette, rounded cards |
| 👤 **Multi-Profile** | Tạo / xóa / chuyển đổi nhiều profile đồng bộ |
| 🔄 **3 chế độ sync** | `copy` · `sync` · `move` — chọn trực tiếp trên UI |
| ⚙ **Cấu hình rclone đầy đủ** | Transfers, Checkers, Bandwidth, Log Level, Extra Flags, Dry Run |
| 👁 **Watch Mode** | Tự động phát hiện file mới (FileSystemWatcher + debounce) và sync |
| 📝 **Real-time Log** | Xem output rclone trực tiếp trong app, auto-scroll |
| 🔌 **Test kết nối** | Kiểm tra remote trước khi sync (`rclone lsd`) |
| 💾 **Auto-save config** | Cấu hình lưu JSON, tự khôi phục khi mở lại |

---

## 🏗 Kiến trúc

```
SyncDB/
├── Core/                          # MVVM Infrastructure
│   ├── RelayCommand.cs            # ICommand implementation
│   ├── ViewModelBase.cs           # INotifyPropertyChanged base
│   └── Converters.cs              # WPF value converters
│
├── Model/
│   └── AppConfig.cs               # AppConfig + SyncProfile + RcloneSyncMode
│
├── Service/
│   ├── RcloneService.cs           # Wrapper rclone process, real-time streaming
│   ├── ConfigService.cs           # JSON load/save (Newtonsoft.Json)
│   └── WatcherService.cs          # FileSystemWatcher + async debounce
│
├── ViewModels/
│   └── MainViewModel.cs           # MVVM orchestrator — commands, bindings
│
├── Themes/
│   └── Styles.xaml                # Dark theme resource dictionary
│
├── MainWindow.xaml / .xaml.cs     # Shell UI (minimal code-behind)
├── App.xaml / .xaml.cs            # Application entry point
└── SyncDB.csproj                  # .NET Framework 4.6.2 WPF
```

**Pattern**: MVVM strict — ViewModel không biết View, View không có logic, data binding only.

---

## 📋 Yêu cầu hệ thống

| Yêu cầu | Chi tiết |
|----------|----------|
| **OS** | Windows Server 2012 R2+ / Windows 7+ |
| **Runtime** | .NET Framework 4.6.2 |
| **rclone** | `rclone.exe` (đặt cùng thư mục với SyncDB.exe) |
| **Cloud** | Google Drive / OneDrive / S3 / bất kỳ remote nào rclone hỗ trợ |

---

## 🚀 Cài đặt & Sử dụng

### 1. Cấu hình rclone (1 lần duy nhất)

```bash
# Mở terminal, chạy rclone config wizard
rclone config

# Tạo remote, ví dụ: ggdrive (Google Drive)
# Sau khi xong, verify:
rclone lsd ggdrive:
```

### 2. Chạy SyncDB

1. Đặt `rclone.exe` vào cùng thư mục `SyncDB.exe`
2. Chạy `SyncDB.exe`
3. Cấu hình trên UI:

| Trường | Ví dụ |
|--------|-------|
| **Thư mục backup** | `D:\DB_Backup` |
| **Remote Path** | `ggdrive:DB_Backup` |
| **Chế độ** | `Copy` (mặc định) |

4. Click **▶ Chạy Sync** hoặc bật **Watch Mode** để tự động sync khi có file mới

---

## ⚙ Cấu hình Rclone (qua UI)

### Tab "Đồng bộ"

| Option | Mô tả |
|--------|-------|
| **Chế độ sync** | `Copy` (chỉ copy mới) · `Sync` (mirror) · `Move` (copy rồi xóa source) |
| **Bỏ qua file đã tồn tại** | `--ignore-existing` |
| **Dry Run** | Chạy thử, không copy thật (`--dry-run`) |

### Tab "Cấu hình Rclone"

| Option | Mô tả | Mặc định |
|--------|-------|----------|
| **Transfers** | Số file copy đồng thời | `4` |
| **Checkers** | Số file check đồng thời | `8` |
| **Bandwidth** | Giới hạn băng thông (vd: `1M`, `500k`) | Không giới hạn |
| **Log Level** | `DEBUG` · `INFO` · `NOTICE` · `ERROR` | `INFO` |
| **Extra Flags** | Thêm flag tùy ý (vd: `--exclude "*.tmp"`) | — |

### Watch Mode

| Option | Mô tả | Mặc định |
|--------|-------|----------|
| **Debounce** | Chờ N giây sau file cuối trước khi sync | `15s` |
| **File Filter** | Chỉ watch các file matching (vd: `*.bak;*.txt`) | `*.bak;*.txt` |

---

## 📂 Multi-Profile

- Click **＋** để tạo profile mới
- Mỗi profile có cấu hình riêng biệt (source, remote, options)
- Chuyển profile bằng dropdown trên header
- Config lưu tại `config.json` cùng thư mục app

**Ví dụ use-case**:
- Profile "DB Production" → `D:\SQL_Backup` → `ggdrive:Prod_DB`
- Profile "DB Staging" → `D:\SQL_Staging` → `ggdrive:Staging_DB`

---

## 🔄 Flow hoạt động

```
[Người dùng]
     │
     │ Cấu hình trên UI (source, remote, options)
     ▼
[SyncDB WPF]
     │
     ├── Lưu config.json
     ├── Ghi log header vào logs/rclone.log
     ▼
[rclone.exe]
     │
     │ copy/sync/move local_folder → remote:path
     │ --transfers N --checkers N --bwlimit X
     │ --ignore-existing --log-file --log-level
     ▼
[Google Drive / OneDrive / S3]

     ─── Watch Mode ───
[FileSystemWatcher]
     │
     │ Phát hiện file mới (.bak, .txt, ...)
     │ Debounce 15s
     ▼
[Auto trigger rclone sync]
```

---

## 🔨 Build từ source

### Yêu cầu
- Visual Studio 2019+ hoặc MSBuild 15+
- .NET Framework 4.6.2 SDK

### Build

```bash
# Sử dụng MSBuild
msbuild SyncDB/SyncDB.csproj /p:Configuration=Release

# Output: SyncDB/bin/Release/SyncDB.exe
```

### Deploy

Copy toàn bộ thư mục `bin/Release/` + `rclone.exe` lên server đích.

---

## 📝 Logging

SyncDB ghi 2 loại log trong thư mục `logs/`:

| File | Nội dung |
|------|----------|
| `app.log` | Log nội bộ app (start, stop, errors) |
| `rclone.log` | Log chi tiết do rclone ghi (file copied, errors, stats) |

Mỗi lần chạy có header:
```
===== RUN 2026-03-23 14:30:15 =====
```

---

## ⚠ Lưu ý

- SyncDB **không tự refresh token** Google Drive. Nếu hết hạn:
  ```bash
  rclone config reconnect ggdrive:
  ```
- Không chỉnh sửa `rclone.conf` khi SyncDB đang chạy
- **Watch Mode** chạy trên background thread, không block UI
- Config tự lưu khi Start sync hoặc click nút Lưu

---

## 📄 License

Internal tool — sử dụng nội bộ.
