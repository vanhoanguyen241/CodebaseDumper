// CodebaseDumper/Engine/IFileScanner.cs
namespace CodebaseDumper.Engine;

/// <summary>
/// Giao diện quét tệp trong thư mục mã nguồn dựa trên cấu hình.
/// </summary>
public interface IFileScanner
{
    /// <summary>
    /// Quét tệp khớp với <see cref="Models.DumpConfig.IncludeGlobs"/>,
    /// loại trừ tệp nằm trong thư mục cấm, sắp xếp theo đường dẫn tương đối.
    /// </summary>
    /// <param name="config">Cấu hình quét.</param>
    /// <returns>Danh sách tệp (có thể rỗng, không bao giờ null).</returns>
    /// <exception cref="System.IO.DirectoryNotFoundException">
    /// Ném khi <see cref="Models.DumpConfig.RootPath"/> không tồn tại.
    /// </exception>
    IReadOnlyList<Models.FileEntry> Scan(Models.DumpConfig config);
}