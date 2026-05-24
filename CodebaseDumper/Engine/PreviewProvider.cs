// CodebaseDumper/Engine/PreviewProvider.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodebaseDumper.Models;

namespace CodebaseDumper.Engine;

/// <summary>
/// Triển khai <see cref="IPreviewProvider"/> phối hợp quét, tạo cây và ước lượng token.
/// </summary>
public class PreviewProvider : IPreviewProvider
{
    private readonly IFileScanner _scanner;
    private readonly ITreeBuilder _treeBuilder;
    private readonly ITokenEstimator _estimator;

    /// <summary>
    /// Khởi tạo với các phụ thuộc cần thiết.
    /// </summary>
    public PreviewProvider(
        IFileScanner scanner,
        ITreeBuilder treeBuilder,
        ITokenEstimator estimator)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _treeBuilder = treeBuilder ?? throw new ArgumentNullException(nameof(treeBuilder));
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
    }

    /// <inheritdoc />
    public async Task<PreviewData> BuildAsync(DumpConfig config, CancellationToken ct)
    {
        // Kiểm tra token huỷ ngay từ đầu, đảm bảo ném OperationCanceledException
        ct.ThrowIfCancellationRequested();

        try
        {
            // Quét danh sách tệp – chạy ngoài UI thread
            IReadOnlyList<FileEntry> files;
            try
            {
                files = await Task.Run(() => _scanner.Scan(config), ct).ConfigureAwait(false);
            }
            catch (DirectoryNotFoundException)
            {
                // Lan truyền ngoại lệ gốc, không nuốt
                throw;
            }

            // Xây cây thư mục – chạy ngoài UI thread
            var tree = await Task.Run(() => _treeBuilder.Build(files, config.RootPath), ct)
                                  .ConfigureAwait(false);

            // Tổng dung lượng byte, tính từ thuộc tính của FileEntry, không đọc tệp
            long totalBytes = files.Sum(f => f.SizeBytes);

            // Ước lượng token chỉ dựa trên tổng byte, không đọc nội dung
            int estimatedTokens = _estimator.Estimate(totalBytes);

            return new PreviewData(tree, files, estimatedTokens, totalBytes);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Chuyển đổi ngoại lệ huỷ (TaskCanceledException) về OperationCanceledException chuẩn
            // để đáp ứng mong đợi của test và các caller ở tầng trên
            throw new OperationCanceledException(ct);
        }
    }
}