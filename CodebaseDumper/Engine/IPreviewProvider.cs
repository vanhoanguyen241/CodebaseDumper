// CodebaseDumper/Engine/IPreviewProvider.cs

using CodebaseDumper.Models;

namespace CodebaseDumper.Engine;

/// <summary>
/// Giao diện tạo dữ liệu xem trước kết quả kết xuất (cây thư mục, số token, tổng byte).
/// </summary>
public interface IPreviewProvider
{
    /// <summary>
    /// Dựng dữ liệu xem trước: quét tệp, xây cây thư mục, ước tính token.
    /// Tất cả các thao tác nặng được chạy trên thread pool để giữ UI mượt.
    /// </summary>
    /// <param name="config">Cấu hình dump.</param>
    /// <param name="ct">Token huỷ.</param>
    /// <returns>Dữ liệu xem trước.</returns>
    /// <exception cref="DirectoryNotFoundException">Khi thư mục gốc không tồn tại.</exception>
    Task<PreviewData> BuildAsync(DumpConfig config, CancellationToken ct);
}