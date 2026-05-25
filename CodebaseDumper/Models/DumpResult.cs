// CodebaseDumper/Models/DumpResult.cs

namespace CodebaseDumper.Models;

/// <summary>
/// Kết quả của quá trình kết xuất mã nguồn.
/// </summary>
/// <param name="OutputPath">Đường dẫn tệp đầu ra đã được ghi.</param>
/// <param name="FileCount">Số lượng tệp đã xử lý thành công.</param>
/// <param name="EstimatedTokens">Ước lượng số token của tệp đầu ra (dựa trên tổng byte / 4).</param>
/// <param name="TotalBytes">Tổng dung lượng byte của tất cả các tệp.</param>
/// <param name="Duration">Thời gian thực hiện kết xuất.</param>
public record DumpResult(
    string OutputPath,
    int FileCount,
    int EstimatedTokens,
    long TotalBytes,
    TimeSpan Duration
);

/// <summary>
/// Báo cáo tiến độ trong quá trình kết xuất.
/// </summary>
/// <param name="ProcessedFiles">Số tệp đã được xử lý.</param>
/// <param name="TotalFiles">Tổng số tệp cần xử lý.</param>
/// <param name="CurrentFile">Đường dẫn tương đối của tệp vừa xử lý.</param>
public record DumpProgress(int ProcessedFiles, int TotalFiles, string CurrentFile);