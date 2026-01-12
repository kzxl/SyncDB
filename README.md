# SyncDB – Rclone UI Helper

## Giới thiệu
**SyncDB** là phần mềm WinForms hỗ trợ giao diện (UI) cho **rclone 1.63.1**, giúp người dùng:
- Kết nối Google Drive thông qua rclone đã cấu hình sẵn
- Đồng bộ (copy) thư mục backup lên Google Drive
- Ghi log chi tiết cho từng lần chạy
- Tránh phải thao tác command line trực tiếp

Phần mềm **không thay thế rclone**, mà đóng vai trò **wrapper UI + log viewer**.

---

## Kiến trúc & Flow hoạt động

### 1. Cấu hình rclone (bên ngoài)
Người dùng **bắt buộc** phải cấu hình Google Drive bằng rclone trước:

```bash
rclone config
```

Sau khi cấu hình xong, rclone sẽ lưu thông tin vào:
- `%USERPROFILE%\.config\rclone\rclone.conf`

SyncDB **không can thiệp** vào bước này.

---

### 2. Flow chạy của SyncDB

```text
[User]
   |
   | (Nhập đường dẫn backup, remote path, option)
   v
[SyncDB UI]
   |
   |-- Lưu config (local)
   |-- Ghi log header
   v
[Run rclone.exe 1.63.1]
   |
   | copy local_folder -> ggdrive:remote_folder
   | --ignore-existing (tuỳ chọn)
   | --log-file
   v
[Google Drive]
```

---

## Chức năng chính

### 1. Giao diện UI
- Chọn thư mục backup (local)
- Nhập remote path (vd: `ggdrive:DB_Backup`)
- Checkbox:
  - Ignore existing file
- Nút:
  - **Test kết nối** (dùng `rclone lsd`)
  - **Run sync**

---

### 2. Test kết nối Google Drive
- Sử dụng lệnh:
```bash
rclone lsd ggdrive:
```
- Mục đích:
  - Kiểm tra token
  - Kiểm tra remote tồn tại
- Không ghi dữ liệu, chỉ đọc

---

### 3. Đồng bộ dữ liệu
- Lệnh rclone được sinh tự động:
```bash
rclone copy "LOCAL_PATH" ggdrive:REMOTE_PATH --ignore-existing --log-file="log.txt" --log-level INFO
```

- Hỗ trợ:
  - Subfolder (rclone copy mặc định **đệ quy**)
  - File mới phát sinh sẽ được copy ở lần chạy tiếp theo

---

### 4. Log
- Mỗi lần chạy sẽ có header riêng:
```text
===== RUN 2026-01-12 09:30:15 =====
```

- Log chi tiết do **rclone ghi trực tiếp**
- SyncDB chỉ thêm:
  - Thời điểm chạy
  - Exit code
  - Error khi start process

---

### 5. Lưu cấu hình
- Các thông tin được lưu local (config file):
  - Đường dẫn backup
  - Remote path
  - Option ignore-existing
- Mục đích:
  - Mở lại phần mềm không cần nhập lại

---

## Yêu cầu hệ thống
- Windows 10 / Windows Server
- .NET Framework 4.6+
- `rclone.exe` **phiên bản 1.63.1**
- Google Drive account đã cấu hình với rclone

---

## Lưu ý quan trọng
- SyncDB **không tự refresh token**
- Nếu Google Drive hết hạn token:
```bash
rclone config reconnect ggdrive:
```
- Không chỉnh sửa file `rclone.conf` khi SyncDB đang chạy

---

## Định hướng mở rộng
- Schedule (Task Scheduler)
- Nhiều profile rclone
- Hỗ trợ OneDrive / S3
- UI xem log realtime

---

## License
Internal tool – sử dụng nội bộ.
