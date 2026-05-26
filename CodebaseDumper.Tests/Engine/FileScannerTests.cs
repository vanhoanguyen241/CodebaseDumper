using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodebaseDumper.Engine;
using CodebaseDumper.Models;
using Xunit;

namespace CodebaseDumper.Tests.Engine;

/// <summary>
/// Kiểm thử cho FileScanner.
/// </summary>
public class FileScannerTests : IDisposable
{
    private readonly string _tempRoot;

    public FileScannerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }

    // ============================
    // Các test có sẵn (giả định)
    // ============================

    [Fact]
    public void Scan_ReturnsFilesMatchingGlobs()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "a.js"), "x");
        File.WriteAllText(Path.Combine(_tempRoot, "b.txt"), "x");
        var config = new DumpConfig
        {
            RootPath = _tempRoot,
            IncludeGlobs = new[] { "*.js" },
            ExcludeDirs = Array.Empty<string>(),
            OutputPath = "out.txt",
            ExcludeFiles = Array.Empty<string>()  // đảm bảo không lọc thêm
        };
        var scanner = new FileScanner();
        var result = scanner.Scan(config);

        Assert.Single(result);
        Assert.EndsWith("a.js", result[0].RelativePath);
    }

    [Fact]
    public void Scan_ExcludesDirectories()
    {
        var subDir = Path.Combine(_tempRoot, "node_modules");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "lib.js"), "x");
        File.WriteAllText(Path.Combine(_tempRoot, "app.js"), "x");

        var config = new DumpConfig
        {
            RootPath = _tempRoot,
            IncludeGlobs = new[] { "*.js" },
            ExcludeDirs = new[] { "node_modules" },
            OutputPath = "out.txt",
            ExcludeFiles = Array.Empty<string>()
        };
        var scanner = new FileScanner();
        var result = scanner.Scan(config);

        Assert.Single(result);
        Assert.DoesNotContain(result, e => e.RelativePath.Contains("node_modules"));
    }

    [Fact]
    public void Scan_ReturnsEmptyListWhenNoMatch()
    {
        var config = new DumpConfig
        {
            RootPath = _tempRoot,
            IncludeGlobs = new[] { "*.xyz" },
            OutputPath = "out.txt",
            ExcludeFiles = Array.Empty<string>()
        };
        var scanner = new FileScanner();
        var result = scanner.Scan(config);

        Assert.Empty(result);
    }

    // ============================
    // Các test mới cho ExcludeFiles
    // ============================

    [Fact]
    public void ScanExcludesFile_ByExactName()
    {
        // Tạo hai tệp, một tệp nằm trong danh sách loại trừ (exact name)
        File.WriteAllText(Path.Combine(_tempRoot, "package-lock.json"), "data");
        File.WriteAllText(Path.Combine(_tempRoot, "index.js"), "code");

        var config = new DumpConfig
        {
            RootPath = _tempRoot,
            IncludeGlobs = new[] { "*.js", "*.json" },
            ExcludeDirs = Array.Empty<string>(),
            OutputPath = "out.txt",
            ExcludeFiles = new[] { "package-lock.json" }  // exact match
        };

        var scanner = new FileScanner();
        var result = scanner.Scan(config);

        Assert.Single(result);
        Assert.EndsWith("index.js", result[0].RelativePath);
    }

    [Fact]
    public void ScanExcludesFile_ByGlobPattern()
    {
        // Tạo hai tệp JS, một bản minified và một bản thường
        File.WriteAllText(Path.Combine(_tempRoot, "app.min.js"), "min");
        File.WriteAllText(Path.Combine(_tempRoot, "app.js"), "normal");

        var config = new DumpConfig
        {
            RootPath = _tempRoot,
            IncludeGlobs = new[] { "*.js" },
            ExcludeDirs = Array.Empty<string>(),
            OutputPath = "out.txt",
            ExcludeFiles = new[] { "*.min.js" }   // loại trừ các tệp min.js
        };

        var scanner = new FileScanner();
        var result = scanner.Scan(config);

        Assert.Single(result);
        Assert.EndsWith("app.js", result[0].RelativePath);
    }

    [Fact]
    public void ScanKeepsFile_WhenExcludeFilesIsEmpty()
    {
        // Không có mẫu loại trừ nào, tất cả tệp khớp IncludeGlobs đều được giữ lại
        File.WriteAllText(Path.Combine(_tempRoot, "app.js"), "code");
        File.WriteAllText(Path.Combine(_tempRoot, "style.css"), "css");

        var config = new DumpConfig
        {
            RootPath = _tempRoot,
            IncludeGlobs = new[] { "*.js", "*.css" },
            ExcludeDirs = Array.Empty<string>(),
            OutputPath = "out.txt",
            ExcludeFiles = Array.Empty<string>() // hoặc null, nhưng mặc định rỗng
        };

        var scanner = new FileScanner();
        var result = scanner.Scan(config);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.RelativePath.EndsWith("app.js"));
        Assert.Contains(result, e => e.RelativePath.EndsWith("style.css"));
    }
}