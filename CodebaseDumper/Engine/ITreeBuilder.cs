// CodebaseDumper/Engine/ITreeBuilder.cs

using System.Collections.Generic;
using CodebaseDumper.Models;

namespace CodebaseDumper.Engine;

/// <summary>
/// Giao diện xây dựng cây thư mục ASCII từ danh sách tệp.
/// </summary>
public interface ITreeBuilder
{
    /// <summary>
    /// Xuất cây thư mục dùng ký tự box-drawing.
    /// Thư mục hiển thị dạng "tên/", tệp hiển thị dạng "tên.mở_rộng".
    /// Đường dẫn gốc được loại bỏ khỏi mọi đường dẫn hiển thị.
    /// </summary>
    /// <param name="files">Danh sách tệp đã sắp xếp theo RelativePath.</param>
    /// <param name="rootPath">Đường dẫn thư mục gốc (sẽ bị loại bỏ khỏi hiển thị).</param>
    /// <returns>Chuỗi biểu diễn cây thư mục, không có ký tự trắng thừa ở cuối.</returns>
    string Build(IReadOnlyList<FileEntry> files, string rootPath);
}