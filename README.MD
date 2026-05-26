# CodebaseDumper

**CodebaseDumper** là một ứng dụng Windows (WPF) mã nguồn mở, giúp bạn nhanh chóng gom toàn bộ mã nguồn của một dự án thành một file `.txt` duy nhất. 

Công cụ này được thiết kế đặc biệt dành cho các lập trình viên muốn cung cấp toàn bộ ngữ cảnh (context) của dự án cho các mô hình AI (như Claude, ChatGPT, DeepSeek) một cách tiện lợi nhất, không cần dùng đến command line hay WSL phức tạp.

## Tính năng nổi bật

* **Giao diện trực quan (GUI):** Thao tác click-and-go đơn giản, thân thiện với người dùng Windows.
* **Bộ lọc thông minh:** Chọn sẵn các nhóm đuôi file (JS/TS, Python, C#, Go,...) và loại trừ các thư mục không cần thiết (node_modules, bin, .git,...).
* **Xem trước cấu trúc (Preview):** Hiển thị cây thư mục ASCII ngay trong app trước khi xuất file.
* **Ước lượng Token:** Tự động tính toán dung lượng và ước tính số lượng Token (chuẩn cl100k_base), giúp bạn biết trước dữ liệu có vừa với Context Window của AI hay không.
* **Loại trừ file linh hoạt:** Bỏ qua các file sinh tự động (*.min.js, *.map) hoặc lock files (package-lock.json) chỉ với một click.
* **Hoạt động độc lập (Portable):** Chỉ một file `.exe` duy nhất, không cần cài đặt .NET runtime.

## Hướng dẫn cài đặt và sử dụng

Không cần cài đặt, không cần thiết lập môi trường phức tạp!

1. Truy cập vào mục **[Releases](https://github.com/vanhoanguyen241/CodebaseDumper/releases)** của kho lưu trữ này.
2. Tải về file `.zip` của phiên bản mới nhất.
3. Giải nén thư mục vừa tải về.
4. Chạy file **`CodebaseDumper.exe`** bên trong là có thể sử dụng được ngay.

### Cách sử dụng nhanh:

1. Mở ứng dụng, nhấn **Browse...** để chọn thư mục dự án của bạn.
2. Tùy chỉnh các loại file muốn giữ lại (Include) và các thư mục/file muốn bỏ qua (Exclude).
3. Xem trước cấu trúc cây thư mục và số lượng Token ước tính ở cột bên phải.
4. Nhấn **Export** để xuất ra file `.txt` và tải lên cho AI xử lý!

## Tech Stack

Dự án được xây dựng với kiến trúc chuẩn mực MVVM, tập trung vào hiệu năng và độ ổn định:

* **Ngôn ngữ:** C# 13
* **Framework:** .NET 9
* **UI:** WPF (Windows Presentation Foundation)
* **Testing:** xUnit và System.IO.Abstractions (100% logic Engine và ViewModel được bao phủ bởi Unit Test)

## Giấy phép (License)

Dự án này được phân phối dưới giấy phép **MIT License**. Bạn hoàn toàn tự do sử dụng, chỉnh sửa và phân phối lại.
