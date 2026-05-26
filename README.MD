# CodebaseDumper

Xuất toàn bộ codebase thành một file `.txt` duy nhất để chia sẻ context với các công cụ AI như Claude hay ChatGPT — không cần bash, không cần WSL, không cần kiến thức dòng lệnh.

## Tính năng

- 📁 Chọn thư mục bằng giao diện click
- 🔍 Lọc file theo glob pattern (JS/TS, Python, C#, Go có sẵn preset)
- 🚫 Loại trừ thư mục và file theo tên hoặc glob
- 👁️ Xem trước cây thư mục + ước tính số token trước khi xuất
- 📤 Xuất file với thanh tiến trình, hỗ trợ huỷ giữa chừng

## Yêu cầu

- Windows 10 / 11
- [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) — hoặc dùng bản self-contained bên dưới

## Build

```powershell
git clone https://github.com/vanhoanguyen241/CodebaseDumper
cd CodebaseDumper
dotnet build
dotnet test
```

Tạo file `.exe` tự chứa (không cần cài .NET):

```powershell
dotnet publish CodebaseDumper -f net9.0-windows -r win-x64 -p:PublishSingleFile=true --self-contained
```

File xuất ra tại `CodebaseDumper/bin/Release/net9.0-windows/win-x64/publish/`.

## Giấy phép

[MIT](LICENSE)
