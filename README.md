# Internet Lock

**Internet Lock** là ứng dụng Windows Desktop viết bằng **C#**, **WPF** và **.NET 8** theo kiến trúc **MVVM**, dùng để bật hoặc vô hiệu hóa nhanh toàn bộ kết nối mạng (Ethernet, Wi-Fi, VPN, USB adapter, virtual adapter) trên máy tính Windows.

---

## 1. Mục tiêu & Tính năng chính

- **Bật / Tắt kết nối Internet toàn hệ thống**: Vô hiệu hóa trực tiếp các card mạng (Network Adapters) thực tế thay vì chỉ chỉnh sửa DNS hay Proxy.
- **Bảo mật khi mở mạng (Chế độ ON)**: Yêu cầu xác nhận mật khẩu trước khi kích hoạt lại bất kỳ card mạng nào.
- **Không cần mật khẩu khi khóa (Chế độ OFF)**: Cho phép khóa nhanh kết nối mạng bằng một thao tác với hộp thoại xác nhận.
- **Tự động lưu trạng thái card mạng**: Nhớ danh sách các card mạng do chính ứng dụng tắt và **chỉ bật lại đúng các card mạng đó**.
- **Chống dò mật khẩu (Anti-Bruteforce)**: Tự động khóa tính năng mở mạng trong 30 giây sau 5 lần nhập sai liên tiếp, kèm đồng hồ đếm ngược.
- **Bảo mật thông tin**: Mật khẩu được mã hóa băm PBKDF2 (SHA-256) với Salt ngẫu nhiên và bảo vệ bằng **Windows DPAPI** (`DataProtectionScope.CurrentUser`).
- **Ghi log chuyên sâu**: Tự động lưu nhật ký thao tác và lỗi kỹ thuật tại thư mục AppData.

---

## 2. Yêu cầu hệ thống

- **Hệ điều hành**: Windows 10 hoặc Windows 11 (64-bit).
- **Runtime**: [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) trở lên (hoặc chạy trực tiếp file Release Self-Contained không cần cài thêm .NET SDK).
- **Quyền hạn**: Quyền Administrator (Yêu cầu qua File Manifest).

---

## 3. Hướng dẫn Biên dịch & Local Release Build

### Cách 1: Sử dụng Script Build tự động (Khuyên dùng)
Chạy script PowerShell `build-release.ps1` để tự động restore, build release `win-x64` self-contained và nén thành ZIP:

```powershell
.\scripts\build-release.ps1
```
File ZIP kết quả sẽ được tạo tại: `artifacts/InternetLock-win-x64.zip`.

### Cách 2: Sử dụng Command Line (`dotnet CLI`)
1. Restore dependency:
   ```bash
   dotnet restore src/InternetLock/InternetLock.csproj
   ```
2. Build hoặc Publish bản Release Windows x64 Single-file Self-contained:
   ```bash
   dotnet publish src/InternetLock/InternetLock.csproj `
     --configuration Release `
     --runtime win-x64 `
     --self-contained true `
     -p:PublishSingleFile=true `
     -p:IncludeNativeLibrariesForSelfExtract=true `
     -p:DebugType=None `
     -p:DebugSymbols=false `
     --output ./publish/win-x64
   ```

### Cách 3: Bằng Visual Studio 2022
1. Mở file `InternetLock.sln` trong Visual Studio 2022 (phiên bản 17.8 trở lên).
2. Chọn cấu hình `Release` và platform `Any CPU` / `x64`.
3. Nhấn `Ctrl + Shift + B` để Rebuild Solution.

---

## 4. Tự động hóa với GitHub Actions (CI/CD)

Repository được tích hợp quy trình build tự động thông qua GitHub Actions (`.github/workflows/build-windows.yml`).

### Cơ chế hoạt động của Workflow:
- Tự động chạy khi có commit push hoặc Pull Request vào nhánh `main`.
- Cho phép kích hoạt thủ công từ giao diện GitHub (Tab **Actions** -> **Build Windows Desktop App** -> **Run workflow**).
- Tự động cài đặt .NET 8 SDK, restore và publish ứng dụng Windows x64 dưới dạng Self-Contained.
- Nén sản phẩm đóng gói thành file `InternetLock-win-x64.zip` và upload lên mục **Artifacts** của Workflow Run.
- **Lưu ý quan trọng**: GitHub Actions **chỉ đóng vai trò biên dịch và đóng gói (Build & Package)**. Workflow **không thực thi hay kiểm thử thao tác tắt/bật card mạng** trên runner của GitHub để đảm bảo an toàn cho môi trường CI.

### Cách tải file ứng dụng từ GitHub Actions:
1. Truy cập vào Repository của bạn trên GitHub.
2. Nhấn vào tab **Actions**.
3. Chọn workflow run mới nhất.
4. Cuộn xuống phần **Artifacts** ở cuối trang và tải file **`InternetLock-win-x64.zip`**.

### Cách chạy ứng dụng đã tải:
1. Giải nén toàn bộ nội dung file `InternetLock-win-x64.zip` ra một thư mục cố định trên máy.
2. Nhấp đúp chuột vào file `InternetLock.exe` để khởi chạy.

> **Cảnh báo Windows SmartScreen / Defender**:
> Khi khởi chạy ứng dụng tải từ Internet/GitHub lần đầu, Windows SmartScreen có thể hiển thị cảnh báo *"Windows protected your PC"* do file thực thi chưa được ký số (Digital Certificate / Code Signing). Bạn chỉ cần chọn **More info** -> **Run anyway** để chạy ứng dụng.

---

## 5. Cấu hình Quyền Administrator & UAC

Do việc **bật/tắt card mạng hệ thống (Network Adapter)** tác động trực tiếp đến phần cứng và dịch vụ mạng của Windows, ứng dụng **bắt buộc phải có quyền quản trị (Administrator)**.

File `app.manifest` của ứng dụng được cấu hình:
```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

> **Lưu ý về UAC (User Account Control)**:
> Khi bạn mở ứng dụng, Windows sẽ hiển thị hộp thoại xác nhận **UAC** (`Do you want to allow this app to make changes to your device?`). Đây là hộp thoại bảo mật tiêu chuẩn của Windows nhằm cấp quyền chạy ứng dụng dưới quyền Administrator, **không phải là mật khẩu mở Internet của ứng dụng**.

---

## 6. Vị trí lưu trữ Dữ liệu & Log

Tất cả dữ liệu của ứng dụng được lưu tại thư mục hệ thống:
```text
%LocalAppData%\InternetLock\
```
(Đường dẫn chi tiết: `C:\Users\<Tên_User>\AppData\Local\InternetLock\`)

- **State File (`adapter-state.json`)**: Lưu danh sách và trạng thái card mạng trước khi bị ứng dụng tắt. Sử dụng cơ chế ghi đè an toàn (Atomic File Write).
- **Password File (`password.dat`)**: Lưu trữ Salt và Hash mật khẩu đã được mã hóa bằng Windows DPAPI.
- **Nhật ký thao tác (`Logs\InternetLock_yyyy-MM-dd.log`)**: Lưu thời gian, thao tác, card mạng tác động, trạng thái thành công/thất bại và lỗi kỹ thuật. (Không lưu mật khẩu dạng plain-text hay hash).

---

## 7. Hướng dẫn Sử dụng Mật khẩu

1. **Lần đầu khởi chạy**: Ứng dụng tự động hiển thị màn hình **Tạo mật khẩu lần đầu** (mật khẩu tối thiểu 6 ký tự).
2. **Khóa Internet**: Chuyển công tắc sang **OFF** hoặc nhấn nút **Tắt toàn bộ** -> Xác nhận -> Hệ thống ngắt mạng.
3. **Mở lại Internet**: Chuyển công tắc sang **ON** hoặc nhấn nút **Bật lại card mạng** -> Nhập đúng mật khẩu -> Hệ thống phục hồi mạng.
4. **Đổi mật khẩu**: Nhấn nút **Đổi mật khẩu** góc trên bên phải -> Nhập mật khẩu hiện tại và mật khẩu mới.

---

## 8. Cảnh báo & Hướng dẫn Khôi phục Thủ công

### Cảnh báo
Khi kích hoạt chế độ **OFF (Khóa Internet)**, tất cả các card mạng hợp lệ (Ethernet, Wi-Fi, VPN, Hyper-V vEthernet, USB Network) sẽ bị disable. Thao tác này sẽ làm ngắt toàn bộ kết nối Internet, mạng nội bộ (LAN), kết nối máy in mạng và dịch vụ VPN đang chạy.

### Cách Khôi phục Thủ công (Trường hợp ứng dụng gặp lỗi ngẫu nhiên)

Nếu ứng dụng gặp sự cố ngoài ý muốn khiến mạng bị khóa và không thể mở lại bằng giao diện, bạn có thể khôi phục kết nối mạng thủ công bằng 1 trong các cách sau:

#### Cách 1: Sử dụng PowerShell (Khuyên dùng - Nhanh nhất)
1. Mở **PowerShell** dưới quyền Administrator (Nhấn `Windows + X` -> chọn `Terminal (Admin)` hoặc `Windows PowerShell (Admin)`).
2. Chạy lệnh sau để bật lại toàn bộ card mạng:
   ```powershell
   Get-NetAdapter | Enable-NetAdapter -Confirm:$false
   ```

#### Cách 2: Sử dụng Windows Settings
1. Nhấn `Windows + I` để mở **Settings**.
2. Chọn **Network & internet** -> **Advanced network settings**.
3. Tìm các card mạng bị Disabled và nhấn nút **Enable**.

#### Cách 3: Sử dụng Device Manager
1. Nhấn `Windows + X` -> chọn **Device Manager**.
2. Mở mục **Network adapters**.
3. Nhấp chuột phải vào từng card mạng có biểu tượng mũi tên chỉ xuống -> chọn **Enable device**.

---

## 9. Cấu trúc Solution

```text
InternetLock/
├── .github/
│   └── workflows/
│       ├── build-windows.yml
│       └── release-windows.yml
├── scripts/
│   └── build-release.ps1
├── .gitignore
├── InternetLock.sln
├── README.md
└── src/
    └── InternetLock/
        ├── App.xaml
        ├── App.xaml.cs
        ├── app.manifest
        ├── InternetLock.csproj
        ├── Commands/
        │   ├── AsyncRelayCommand.cs
        │   └── RelayCommand.cs
        ├── Converters/
        │   ├── BooleanToStatusConverter.cs
        │   ├── InverseBooleanConverter.cs
        │   ├── NullToVisibilityConverter.cs
        │   └── StatusToBrushConverter.cs
        ├── Helpers/
        │   ├── AppPaths.cs
        │   └── JsonFileHelper.cs
        ├── Models/
        │   ├── AdapterSavedState.cs
        │   ├── ApplicationState.cs
        │   ├── NetworkAdapterInfo.cs
        │   └── OperationResult.cs
        ├── Services/
        │   ├── AdministratorService.cs
        │   ├── FileLoggerService.cs
        │   ├── ILoggerService.cs
        │   ├── INetworkAdapterService.cs
        │   ├── IPasswordService.cs
        │   ├── IStateStorageService.cs
        │   ├── NetworkAdapterService.cs
        │   ├── PasswordService.cs
        │   └── StateStorageService.cs
        ├── ViewModels/
        │   ├── BaseViewModel.cs
        │   └── MainViewModel.cs
        └── Views/
            ├── ChangePasswordWindow.xaml
            ├── ChangePasswordWindow.xaml.cs
            ├── MainWindow.xaml
            ├── MainWindow.xaml.cs
            ├── PasswordConfirmWindow.xaml
            ├── PasswordConfirmWindow.xaml.cs
            ├── PasswordSetupWindow.xaml
            └── PasswordSetupWindow.xaml.cs
```
