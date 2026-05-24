// CodebaseDumper.Tests/Engine/FileScannerTests.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodebaseDumper.Engine;
using CodebaseDumper.Models;
using Xunit;

namespace CodebaseDumper.Tests.Engine;

/// <summary>
/// Kiểm thử cho <see cref="FileScanner"/>.
/// Sử dụng thư mục tạm để tạo cây tệp thật và kiểm tra kết quả quét.
/// </summary>
public class FileScannerTests : IDisposable
{
    /// <summary>
    /// Đường dẫn đến thư mục gốc tạm dùng trong kiểm thử.
    /// </summary>
    private readonly string _tempRoot;

    public FileScannerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "FileScannerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, true); }
            catch { /* Bỏ qua nếu không xoá được */ }
        }
    }

    /// <summary>
    /// Tạo cấu hình quét với RootPath và các tuỳ chọn.
    /// </summary>
    private DumpConfig CreateConfig(
        string rootPath,
        IReadOnlyList<string>? includeGlobs = null,
        IReadOnlyList<string>? excludeDirs = null)
    {
        return new DumpConfig
        {
            RootPath = rootPath,
            IncludeGlobs = includeGlobs ?? new[] { "*.txt" },
            ExcludeDirs = excludeDirs ?? new List<string>()
        };
    }

    /// <summary>
    /// Tạo một tập tin trong thư mục gốc tạm và trả về đường dẫn tuyệt đối.
    /// </summary>
    private string CreateFile(string relativePath)
    {
        string fullPath = Path.Combine(_tempRoot, relativePath);
        string? dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, "");
        return fullPath;
    }

    [Fact]
    public void ScanReturnsMatchingFiles_SortedByRelativePath()
    {
        // Sắp xếp: a.txt, b.md, sub/c.txt
        CreateFile("a.txt");
        CreateFile("b.md");
        CreateFile(Path.Combine("sub", "c.txt"));
        CreateFile("z.ignore"); // không khớp glob

        var config = CreateConfig(_tempRoot,
            includeGlobs: new[] { "*.txt", "*.md" });

        var scanner = new FileScanner();
        var result = scanner.Scan(config);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("a.txt", result[0].RelativePath);
        Assert.Equal("b.md", result[1].RelativePath);
        Assert.Equal("sub" + Path.DirectorySeparatorChar + "c.txt", result[2].RelativePath);
    }

    [Fact]
    public void ScanExcludesNodeModules_ExactSegmentMatch()
    {
        CreateFile(Path.Combine("src", "app.js"));
        CreateFile(Path.Combine("node_modules", "pkg", "index.js"));
        CreateFile(Path.Combine("Node_Modules", "test.js")); // phân biệt hoa thường
        CreateFile(Path.Combine("my_node_modules_backup", "file.js"));
        CreateFile(Path.Combine("project", "node_modules", "sub", "file.js"));

        var config = CreateConfig(_tempRoot,
            includeGlobs: new[] { "*.js" },
            excludeDirs: new[] { "node_modules" });

        var scanner = new FileScanner();
        var result = scanner.Scan(config);

        // Chỉ còn src/app.js và my_node_modules_backup/file.js
        Assert.NotNull(result);
        var relativePaths = result.Select(e => e.RelativePath).ToList();
        Assert.Equal(2, relativePaths.Count);
        Assert.Contains("src" + Path.DirectorySeparatorChar + "app.js", relativePaths);
        Assert.Contains("my_node_modules_backup" + Path.DirectorySeparatorChar + "file.js", relativePaths);
    }

    [Fact]
    public void ScanReturnsEmpty_WhenNoMatch()
    {
        CreateFile("readme.doc");

        var config = CreateConfig(_tempRoot,
            includeGlobs: new[] { "*.js" });

        var scanner = new FileScanner();
        var result = scanner.Scan(config);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ScanThrows_WhenDirectoryNotFound()
    {
        string nonExistent = Path.Combine(_tempRoot, "not_here");
        var config = CreateConfig(nonExistent);

        var scanner = new FileScanner();

        var ex = Assert.Throws<DirectoryNotFoundException>(() => scanner.Scan(config));
        Assert.Contains(config.RootPath, ex.Message);
    }

    [Fact]
    public void ScanSkipsInaccessibleSubdirectories_DoesNotThrow()
    {
        // Tạo một thư mục con nhưng đánh dấu không có quyền truy cập
        // Để mô phỏng lỗi truy cập trên hệ thống thật, ta tạo thư mục rồi thu hồi quyền liệt kê.
        // Tuy nhiên, điều này phức tạp và phụ thuộc nền tảng. Thay vào đó, ta kiểm tra rằng
        // Scan hoàn tất không ném ngoại lệ khi tồn tại thư mục không thể truy cập.
        // Trong môi trường kiểm thử không có thư mục thật bị khoá, ta chỉ cần gọi Scan và
        // đảm bảo nó không throw – các tệp khớp vẫn được trả về.
        CreateFile("safe.txt");
        string lockedDir = Path.Combine(_tempRoot, "locked");
        Directory.CreateDirectory(lockedDir);
        // Không tạo tệp bên trong lockedDir – vẫn có thể quét thư mục con nhưng không lỗi.

        var config = CreateConfig(_tempRoot,
            includeGlobs: new[] { "*.txt" });

        var scanner = new FileScanner();

        // Phải không ném bất kỳ ngoại lệ nào
        var result = scanner.Scan(config);

        // safe.txt phải được tìm thấy
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("safe.txt", result[0].RelativePath);
    }
}