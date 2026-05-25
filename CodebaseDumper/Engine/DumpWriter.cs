// CodebaseDumper/Engine/DumpWriter.cs

using System.Diagnostics;
using System.IO;
using CodebaseDumper.Models;

namespace CodebaseDumper.Engine;

/// <summary>
/// Triển khai ghi tệp kết xuất mã nguồn với streaming và hỗ trợ hủy bỏ.
/// </summary>
public class DumpWriter : IDumpWriter
{
    /// <inheritdoc />
    public async Task<DumpResult> WriteAsync(
        DumpConfig config,
        IReadOnlyList<FileEntry> files,
        string asciiTree,
        IProgress<DumpProgress>? progress,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Kiểm tra thư mục đầu ra tồn tại trước khi mở stream
            var outputDir = Path.GetDirectoryName(config.OutputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                throw new IOException($"Thư mục đầu ra '{outputDir}' không tồn tại.");
            }

            // Mở stream ghi với encoding từ cấu hình, không nối thêm
            await using var writer = new StreamWriter(config.OutputPath, false, config.OutputEncoding);

            // Ghi header cấu trúc dự án
            await writer.WriteAsync("===== PROJECT STRUCTURE =====\n");
            await writer.WriteAsync(asciiTree);
            await writer.WriteAsync("\n\n");

            int processed = 0;
            long totalBytes = 0;

            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();

                // Đọc toàn bộ nội dung tệp (cho phép huỷ khi đọc)
                string content = await File.ReadAllTextAsync(file.AbsolutePath, ct);

                // Ghi section cho tệp hiện tại
                await writer.WriteAsync($"===== FILE: {file.RelativePath} =====\n");
                await writer.WriteAsync(content);
                await writer.WriteAsync("\n\n");

                // Đảm bảo dữ liệu được đẩy xuống ổ đĩa trước khi báo cáo tiến độ
                await writer.FlushAsync();

                totalBytes += file.SizeBytes;
                processed++;
                progress?.Report(new DumpProgress(processed, files.Count, file.RelativePath));
            }

            await writer.FlushAsync();
            stopwatch.Stop();

            // Ước lượng token: trung bình cl100k_base ~4 byte cho mỗi token
            int estimatedTokens = totalBytes > 0 ? (int)(totalBytes / 4) : 0;

            return new DumpResult(
                config.OutputPath,
                processed,
                estimatedTokens,
                totalBytes,
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            // Xoá tệp kết xuất chưa hoàn chỉnh nếu tồn tại
            if (File.Exists(config.OutputPath))
            {
                File.Delete(config.OutputPath);
            }
            // Luôn ném OperationCanceledException, ngay cả khi nguồn gốc là TaskCanceledException
            throw new OperationCanceledException("Thao tác bị hủy bỏ.", ct);
        }
    }
}