// CodebaseDumper/ViewModels/AppState.cs

namespace CodebaseDumper.ViewModels;

/// <summary>
/// Trạng thái của MainViewModel trong quy trình kết xuất.
/// </summary>
public enum AppState
{
    /// <summary>Chưa có thao tác nào đang chạy.</summary>
    Idle,

    /// <summary>Đang quét thư mục và dựng dữ liệu xem trước.</summary>
    Scanning,

    /// <summary>Đã có dữ liệu xem trước, sẵn sàng kết xuất.</summary>
    Previewed,

    /// <summary>Đang ghi tệp kết xuất.</summary>
    Exporting,

    /// <summary>Kết xuất thành công.</summary>
    Done,

    /// <summary>Gặp lỗi trong quá trình quét hoặc kết xuất.</summary>
    Error
}