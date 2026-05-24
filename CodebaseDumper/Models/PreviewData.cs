// CodebaseDumper/Models/PreviewData.cs

namespace CodebaseDumper.Models;

/// <summary>
/// Dữ liệu xem trước kết quả kết xuất mã nguồn.
/// </summary>
/// <param name="AsciiTree">Biểu diễn cây thư mục dạng ASCII.</param>
/// <param name="Files">Danh sách tệp sẽ được xử lý.</param>
/// <param name="EstimatedTokens">Ước lượng số token (mô hình cl100k_base) dựa trên tổng dung lượng byte.</param>
/// <param name="TotalBytes">Tổng dung lượng byte của tất cả tệp.</param>
public record PreviewData(
    string AsciiTree,
    IReadOnlyList<FileEntry> Files,
    int EstimatedTokens,
    long TotalBytes
);