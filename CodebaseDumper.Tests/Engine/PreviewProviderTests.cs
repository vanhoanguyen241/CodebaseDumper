// CodebaseDumper.Tests/Engine/PreviewProviderTests.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodebaseDumper.Engine;
using CodebaseDumper.Models;
using Xunit;

namespace CodebaseDumper.Tests.Engine;

public class TokenEstimatorTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 1)]
    [InlineData(5, 2)]
    [InlineData(8, 2)]
    [InlineData(400, 100)]
    public void TokenEstimator_BoundaryValues(long totalBytes, int expectedTokens)
    {
        var estimator = new TokenEstimator();
        int result = estimator.Estimate(totalBytes);
        Assert.Equal(expectedTokens, result);
    }
}

public class PreviewProviderTests
{
    // Giả lập IFileScanner trả về danh sách cố định
    private class FakeFileScanner : IFileScanner
    {
        private readonly IReadOnlyList<FileEntry> _files;
        public FakeFileScanner(IReadOnlyList<FileEntry> files) => _files = files;

        public IReadOnlyList<FileEntry> Scan(DumpConfig config) => _files;
    }

    // Giả lập ITreeBuilder trả về chuỗi cố định
    private class FakeTreeBuilder : ITreeBuilder
    {
        public string Build(IReadOnlyList<FileEntry> files, string rootPath) => "cây giả";
    }

    private static DumpConfig CreateTestConfig(string root = @"C:\fake") =>
        new() { RootPath = root };

    [Fact]
    public async Task PreviewProvider_ReturnsCorrectFileCount()
    {
        // Chuẩn bị dữ liệu giả: 3 tệp với kích thước cụ thể
        var files = new List<FileEntry>
        {
            new("a.cs", @"C:\fake\a.cs", 100),
            new("b.cs", @"C:\fake\b.cs", 200),
            new("c.cs", @"C:\fake\c.cs", 300)
        };
        var scanner = new FakeFileScanner(files);
        var treeBuilder = new FakeTreeBuilder();
        var estimator = new TokenEstimator(); // triển khai thật, không I/O

        var provider = new PreviewProvider(scanner, treeBuilder, estimator);
        var config = CreateTestConfig();

        PreviewData preview = await provider.BuildAsync(config, CancellationToken.None);

        // Số lượng tệp đúng
        Assert.Equal(3, preview.Files.Count);
        // Tổng byte = 100 + 200 + 300 = 600
        Assert.Equal(600, preview.TotalBytes);
        // Token ước tính = ceil(600/4) = 150
        Assert.Equal(150, preview.EstimatedTokens);
        // Cây thư mục giả
        Assert.Equal("cây giả", preview.AsciiTree);
    }

    [Fact]
    public async Task PreviewProvider_EstimatedTokens_BasedOnSizeBytesOnly()
    {
        // Đảm bảo token được tính từ SizeBytes, không đọc file thật
        var files = new List<FileEntry>
        {
            new("x.txt", @"C:\fake\x.txt", 1),   // ceil(1/4)=1
            new("y.txt", @"C:\fake\y.txt", 2)    // ceil(2/4)=1
        };
        var scanner = new FakeFileScanner(files);
        var provider = new PreviewProvider(scanner, new FakeTreeBuilder(), new TokenEstimator());

        PreviewData preview = await provider.BuildAsync(CreateTestConfig(), CancellationToken.None);

        // Tổng byte = 3, token = ceil(3/4)=1
        Assert.Equal(3, preview.TotalBytes);
        Assert.Equal(1, preview.EstimatedTokens);
    }

    [Fact]
    public async Task PreviewProvider_PropagatesCancellation()
    {
        // Tạo token đã huỷ trước khi gọi BuildAsync
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var scanner = new FakeFileScanner(new List<FileEntry>());
        var provider = new PreviewProvider(scanner, new FakeTreeBuilder(), new TokenEstimator());

        // BuildAsync phải ném OperationCanceledException khi token đã bị huỷ
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => provider.BuildAsync(CreateTestConfig(), cts.Token)
        );
    }
}