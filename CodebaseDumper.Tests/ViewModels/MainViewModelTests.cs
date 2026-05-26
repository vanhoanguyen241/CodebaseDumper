// CodebaseDumper.Tests/ViewModels/MainViewModelTests.cs
using System.Collections.ObjectModel;
using System.Linq;
using CodebaseDumper.Engine;
using CodebaseDumper.Models;
using CodebaseDumper.ViewModels;
using Xunit;

namespace CodebaseDumper.Tests.ViewModels;

/// <summary>
/// Kiểm thử cho MainViewModel.
/// </summary>
public class MainViewModelTests
{
    // Stub cho IPreviewProvider (có thể dùng chung cho các test khác)
    private class StubPreviewProvider : IPreviewProvider
    {
        public System.Threading.Tasks.Task<PreviewData> BuildAsync(DumpConfig config, System.Threading.CancellationToken ct)
        {
            return System.Threading.Tasks.Task.FromResult(
                new PreviewData("", System.Array.Empty<FileEntry>(), 0, 0L));
        }
    }

    // Stub cho IDumpWriter
    private class StubDumpWriter : IDumpWriter
    {
        public System.Threading.Tasks.Task<DumpResult> WriteAsync(
            DumpConfig config,
            System.Collections.Generic.IReadOnlyList<FileEntry> files,
            string asciiTree,
            IProgress<DumpProgress> progress,
            System.Threading.CancellationToken ct)
        {
            return System.Threading.Tasks.Task.FromResult(
                new DumpResult(config.OutputPath!, 0, 0, 0L, System.TimeSpan.Zero));
        }
    }

    // Các test hiện có (giữ nguyên, không xoá)
    // ... (các test trước đó nằm ở đây)

    /// <summary>
    /// Kiểm tra ExcludeFiles mặc định khớp với DumpConfig.ExcludeFiles mặc định.
    /// </summary>
    [Fact]
    public void ExcludeFiles_DefaultValues_MatchDumpConfigDefaults()
    {
        // Arrange
        var vm = new MainViewModel(new StubPreviewProvider(), new StubDumpWriter());
        var defaultConfig = new DumpConfig { RootPath = string.Empty };
        var expectedDefaults = defaultConfig.ExcludeFiles;

        // Act
        var actualExcludeFiles = vm.ExcludeFiles;

        // Assert
        Assert.NotNull(actualExcludeFiles);
        Assert.Equal(expectedDefaults.Count, actualExcludeFiles.Count);
        foreach (var expected in expectedDefaults)
        {
            Assert.Contains(expected, actualExcludeFiles);
        }
    }
}