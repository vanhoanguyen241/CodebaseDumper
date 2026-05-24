// CodebaseDumper/Engine/FileScanner.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodebaseDumper.Models;

namespace CodebaseDumper.Engine;

/// <summary>
/// Triển khai <see cref="IFileScanner"/> sử dụng trực tiếp System.IO để kiểm tra và truy cập tệp.
/// </summary>
public class FileScanner : IFileScanner
{
    /// <inheritdoc />
    public IReadOnlyList<FileEntry> Scan(DumpConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        if (string.IsNullOrWhiteSpace(config.RootPath))
            throw new ArgumentException("RootPath không được để trống.", nameof(config));

        if (!Directory.Exists(config.RootPath))
            throw new DirectoryNotFoundException($"Thư mục gốc không tồn tại: {config.RootPath}");

        // Bộ sưu tập tệp tuyệt đối (dùng HashSet để loại bỏ trùng lặp)
        var absolutePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // EnumerationOptions thay thế SearchOption.AllDirectories để tránh crash với thư mục hệ thống
        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            MatchCasing = MatchCasing.CaseInsensitive
        };

        // Lặp qua từng mẫu glob
        foreach (var pattern in config.IncludeGlobs)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            try
            {
                var matches = Directory.EnumerateFiles(
                    config.RootPath, pattern, options);
                foreach (var path in matches)
                    absolutePaths.Add(path);
            }
            catch (DirectoryNotFoundException)
            {
                // Root đã kiểm tra, đây là trường hợp thư mục con biến mất khi đang quét → bỏ qua
            }
        }

        // Tạo danh sách FileEntry sau khi lọc ExcludeDirs
        var entries = new List<FileEntry>();
        foreach (var absolutePath in absolutePaths)
        {
            // Đường dẫn tương đối dùng để so khớp segment và làm kết quả sắp xếp
            string relativePath = Path.GetRelativePath(config.RootPath, absolutePath);

            if (IsExcludedByDir(relativePath, config.ExcludeDirs))
                continue;

            long size = 0L;
            try
            {
                var fileInfo = new FileInfo(absolutePath);
                if (fileInfo.Exists)
                    size = fileInfo.Length;
            }
            catch
            {
                // Bỏ qua lỗi truy cập khi lấy kích thước tệp
            }

            entries.Add(new FileEntry(
                RelativePath: relativePath,
                AbsolutePath: absolutePath,
                SizeBytes: size));
        }

        // Sắp xếp tăng dần theo RelativePath (so sánh không phân biệt hoa thường)
        entries.Sort((a, b) =>
            string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));

        return entries.AsReadOnly();
    }

    /// <summary>
    /// Kiểm tra xem đường dẫn tương đối có chứa segment nào khớp chính xác
    /// với danh sách thư mục bị loại trừ hay không.
    /// </summary>
    private static bool IsExcludedByDir(string relativePath, IReadOnlyList<string> excludeDirs)
    {
        if (excludeDirs == null || excludeDirs.Count == 0)
            return false;

        // Tách đường dẫn thành các segment, chấp nhận cả dấu phân cách phổ biến
        string[] segments = relativePath.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            foreach (var exclude in excludeDirs)
            {
                // So khớp chính xác segment, không phân biệt hoa thường
                if (string.Equals(segment, exclude, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}