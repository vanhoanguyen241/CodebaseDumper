// CodebaseDumper/Models/DumpConfig.cs
using System.Collections.Generic;
using System.Text;

namespace CodebaseDumper.Models;

/// <summary>
/// Đại diện cho một tệp tin trong cây mã nguồn.
/// </summary>
/// <param name="RelativePath">Đường dẫn tương đối tính từ thư mục gốc.</param>
/// <param name="AbsolutePath">Đường dẫn tuyệt đối đến tệp.</param>
/// <param name="SizeBytes">Kích thước tệp tính bằng byte.</param>
public record FileEntry(
    string RelativePath,
    string AbsolutePath,
    long SizeBytes
);

/// <summary>
/// Cấu hình để thu thập mã nguồn và xuất ra tệp văn bản.
/// </summary>
public record DumpConfig
{
    /// <summary>
    /// Đường dẫn đến thư mục gốc chứa mã nguồn (bắt buộc).
    /// </summary>
    public required string RootPath { get; init; }

    /// <summary>
    /// Danh sách các mẫu glob để chọn tệp.
    /// </summary>
    public IReadOnlyList<string> IncludeGlobs { get; init; } = new[]
    {
        "*.js", "*.ts", "*.jsx", "*.tsx", "*.json"
    };

    /// <summary>
    /// Danh sách thư mục sẽ bỏ qua.
    /// </summary>
    public IReadOnlyList<string> ExcludeDirs { get; init; } = new[]
    {
        "node_modules", "dist", ".git", "bin", "obj", "__pycache__"
    };

    /// <summary>
    /// Đường dẫn đến tệp đầu ra.
    /// </summary>
    public string OutputPath { get; init; } = "codebase_dump.txt";

    /// <summary>
    /// Bảng mã ký tự cho tệp đầu ra.
    /// </summary>
    public Encoding OutputEncoding { get; init; } = Encoding.UTF8;
}