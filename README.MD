# CodebaseDumper

Export codebase thành một file .txt duy nhất để chia sẻ context với AI.

## Yêu cầu
- Windows 10/11
- .NET 9 Runtime (hoặc dùng bản self-contained không cần cài)

## Build
```powershell
dotnet build
dotnet test
dotnet publish CodebaseDumper -f net9.0-windows -r win-x64 -p:PublishSingleFile=true --self-contained
```

## Trạng thái
Đang phát triển — xem `docs/design.md` để biết chi tiết.