// CodebaseDumper/Engine/IDumpWriter.cs
using CodebaseDumper.Models;

namespace CodebaseDumper.Engine;

/// <summary>
/// Giao diện ghi kết quả kết xuất mã nguồn ra tệp.
/// </summary>
public interface IDumpWriter
{
    /// <summary>
    /// Ghi toàn bộ nội dung kết xuất ra tệp theo cấu hình.
    /// </summary>
    Task<DumpResult> WriteAsync(
        DumpConfig config,
        IReadOnlyList<FileEntry> files,
        string asciiTree,
        IProgress<DumpProgress>? progress,
        CancellationToken ct);
}