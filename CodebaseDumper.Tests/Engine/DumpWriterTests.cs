// CodebaseDumper.Tests/Engine/DumpWriterTests.cs

using CodebaseDumper.Engine;
using CodebaseDumper.Models;
using System.IO;
using System.Text;

namespace CodebaseDumper.Tests.Engine;

/// <summary>
/// Kiểm thử cho DumpWriter sử dụng hệ thống tệp thật và thư mục tạm.
/// </summary>
public class DumpWriterTests
{
    [Fact]
    public async Task WriteAsync_ProducesCorrectHeader_AndFileSection()
    {
        // Chuẩn bị thư mục tạm
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            // Tạo hai tệp giả với nội dung mẫu
            var file1Path = Path.Combine(tempDir, "file1.js");
            var file2Path = Path.Combine(tempDir, "file2.ts");
            await File.WriteAllTextAsync(file1Path, "console.log('hello');");
            await File.WriteAllTextAsync(file2Path, "const x: number = 1;");

            var config = new DumpConfig
            {
                RootPath = tempDir,
                OutputPath = Path.Combine(tempDir, "output.txt"),
                OutputEncoding = Encoding.UTF8
            };

            var files = new List<FileEntry>
            {
                new("file1.js", file1Path, new FileInfo(file1Path).Length),
                new("file2.ts", file2Path, new FileInfo(file2Path).Length)
            };

            const string asciiTree = "root\n├── file1.js\n└── file2.ts";

            var writer = new DumpWriter();
            var result = await writer.WriteAsync(config, files, asciiTree, null, CancellationToken.None);

            // Kiểm tra kết quả trả về
            Assert.Equal(2, result.FileCount);
            Assert.True(result.TotalBytes > 0);
            Assert.True(result.EstimatedTokens >= 0);
            Assert.True(result.Duration > TimeSpan.Zero);

            // Xác minh nội dung tệp đầu ra
            var outputContent = await File.ReadAllTextAsync(config.OutputPath);
            var expectedHeader = "===== PROJECT STRUCTURE =====\n" + asciiTree + "\n\n";
            Assert.StartsWith(expectedHeader, outputContent);
            Assert.Contains("===== FILE: file1.js =====", outputContent);
            Assert.Contains("console.log('hello');", outputContent);
            Assert.Contains("===== FILE: file2.ts =====", outputContent);
            Assert.Contains("const x: number = 1;", outputContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task WriteAsync_ReportsProgressPerFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var fileA = Path.Combine(tempDir, "a.js");
            var fileB = Path.Combine(tempDir, "b.js");
            await File.WriteAllTextAsync(fileA, "1");
            await File.WriteAllTextAsync(fileB, "2");

            var config = new DumpConfig
            {
                RootPath = tempDir,
                OutputPath = Path.Combine(tempDir, "out.txt")
            };

            var files = new List<FileEntry>
            {
                new("a.js", fileA, 1),
                new("b.js", fileB, 1)
            };

            var reports = new List<DumpProgress>();
            var progress = new Progress<DumpProgress>(reports.Add);

            var writer = new DumpWriter();
            await writer.WriteAsync(config, files, "tree", progress, CancellationToken.None);

            Assert.Equal(2, reports.Count);
            // Lần báo cáo thứ nhất
            Assert.Equal(1, reports[0].ProcessedFiles);
            Assert.Equal(2, reports[0].TotalFiles);
            Assert.Equal("a.js", reports[0].CurrentFile);
            // Lần báo cáo thứ hai
            Assert.Equal(2, reports[1].ProcessedFiles);
            Assert.Equal("b.js", reports[1].CurrentFile);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task WriteAsync_OnCancellation_DeletesPartialFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var firstFile = Path.Combine(tempDir, "first.js");
            var secondFile = Path.Combine(tempDir, "second.js");
            await File.WriteAllTextAsync(firstFile, "first content");
            await File.WriteAllTextAsync(secondFile, "second content");

            var config = new DumpConfig
            {
                RootPath = tempDir,
                OutputPath = Path.Combine(tempDir, "partial.txt")
            };

            var files = new List<FileEntry>
            {
                new("first.js", firstFile, 13),
                new("second.js", secondFile, 14)
            };

            using var cts = new CancellationTokenSource();
            var progress = new Progress<DumpProgress>(p =>
            {
                if (p.ProcessedFiles == 1)
                    cts.Cancel();
            });

            var writer = new DumpWriter();
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                writer.WriteAsync(config, files, "tree", progress, cts.Token));

            // Xác nhận tệp kết xuất đã bị xoá
            Assert.False(File.Exists(config.OutputPath));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task WriteAsync_PropagatesIOException()
    {
        // Đường dẫn đầu ra trỏ vào thư mục không tồn tại
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var config = new DumpConfig
        {
            RootPath = ".",
            OutputPath = Path.Combine(nonExistentDir, "out.txt"),
        };

        // Tạo tệp tạm để đưa vào danh sách (tệp thật, tránh FileNotFoundException che khuất)
        var tempFile = Path.GetTempFileName();
        try
        {
            var files = new List<FileEntry> { new("dummy.js", tempFile, 0) };
            var writer = new DumpWriter();

            // StreamWriter sẽ ném IOException (DirectoryNotFoundException) khi mở stream
            await Assert.ThrowsAsync<IOException>(() =>
                writer.WriteAsync(config, files, "tree", null, CancellationToken.None));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}