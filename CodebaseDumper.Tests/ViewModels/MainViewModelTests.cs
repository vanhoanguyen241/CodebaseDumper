// CodebaseDumper.Tests/ViewModels/MainViewModelTests.cs

using System.ComponentModel;
using System.Windows.Input;
using CodebaseDumper.Engine;
using CodebaseDumper.Models;
using CodebaseDumper.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace CodebaseDumper.Tests.ViewModels;

/// <summary>
/// Kiểm thử máy trạng thái của MainViewModel.
/// </summary>
public class MainViewModelTests
{
    /// <summary>
    /// Trạng thái khởi tạo phải là Idle.
    /// </summary>
    [Fact]
    public void InitialState_IsIdle()
    {
        var vm = CreateViewModel(new FakePreviewProvider());
        Assert.Equal(AppState.Idle, vm.State);
    }

    /// <summary>
    /// ExportCommand chỉ khả dụng khi State == Previewed.
    /// </summary>
    [Fact]
    public async Task ExportCommand_CanExecuteOnlyInPreviewed()
    {
        var fakePreview = new FakePreviewProvider();
        var vm = CreateViewModel(fakePreview);

        // Ban đầu chưa thể kết xuất
        Assert.False(vm.ExportCommand.CanExecute(null));

        // Kích hoạt quét và chờ hoàn tất
        vm.RootPath = "C:\\test";
        await WaitForScanAsync(vm);

        // Sau khi quét thành công → có thể kết xuất
        Assert.Equal(AppState.Previewed, vm.State);
        Assert.True(vm.ExportCommand.CanExecute(null));
    }

    /// <summary>
    /// CancelCommand chỉ khả dụng khi State == Exporting.
    /// </summary>
    [Fact]
    public async Task CancelCommand_CanExecuteOnlyInExporting()
    {
        var fakePreview = new FakePreviewProvider();
        var vm = CreateViewModel(fakePreview);

        // Chưa kết xuất → không thể huỷ
        Assert.False(vm.CancelCommand.CanExecute(null));

        // Đưa về trạng thái Previewed
        vm.RootPath = "C:\\test";
        await WaitForScanAsync(vm);

        // Bắt đầu kết xuất
        vm.ExportCommand.Execute(null);
        // Ngay sau khi gọi, trạng thái chuyển sang Exporting
        Assert.Equal(AppState.Exporting, vm.State);
        Assert.True(vm.CancelCommand.CanExecute(null));

        // Huỷ kết xuất
        vm.CancelCommand.Execute(null);
        // Đợi task kết xuất hoàn thành việc hủy (sẽ quay về Previewed)
        await WaitForExportAsync(vm);

        Assert.Equal(AppState.Previewed, vm.State);
        Assert.False(vm.CancelCommand.CanExecute(null));
    }

    /// <summary>
    /// Khi thay đổi RootPath, máy trạng thái tự động chuyển sang Scanning.
    /// </summary>
    [Fact]
    public async Task RootPathChange_TriggersScan_TransitionsToScanning()
    {
        var fakePreview = new FakePreviewProvider();
        var vm = CreateViewModel(fakePreview);

        vm.RootPath = "C:\\some\\folder";

        // Ngay sau khi gán, trạng thái phải là Scanning
        Assert.Equal(AppState.Scanning, vm.State);

        await WaitForScanAsync(vm);

        Assert.Equal(AppState.Previewed, vm.State);
    }

    /// <summary>
    /// Khi gặp lỗi, State == Error và ErrorMessage phải khác null.
    /// </summary>
    [Fact]
    public async Task ErrorState_HasNonNullErrorMessage()
    {
        var errorPreview = new FakePreviewProvider(throwOnBuild: true);
        var vm = CreateViewModel(errorPreview);

        vm.RootPath = "C:\\invalid";
        await WaitForScanAsync(vm);

        Assert.Equal(AppState.Error, vm.State);
        Assert.NotNull(vm.ErrorMessage);
        Assert.NotEmpty(vm.ErrorMessage);
    }

    // ---------- helper ----------

    private MainViewModel CreateViewModel(IPreviewProvider previewProvider, IDumpWriter? dumpWriter = null)
    {
        dumpWriter ??= new FakeDumpWriter();
        return new MainViewModel(previewProvider, dumpWriter);
    }

    private static async Task WaitForScanAsync(MainViewModel vm)
    {
        var scanTask = vm.CurrentScanTask;
        if (scanTask != null)
        {
            await scanTask;
        }
        else
        {
            // fallback an toàn
            await Task.Delay(500);
        }
    }

    private static async Task WaitForExportAsync(MainViewModel vm)
    {
        var exportTask = vm.CurrentExportTask;
        if (exportTask != null)
        {
            await exportTask;
        }
        else
        {
            // fallback an toàn
            await Task.Delay(500);
        }
    }

    // ---------- fakes ----------

    private class FakePreviewProvider : IPreviewProvider
    {
        private readonly bool _throwOnBuild;

        public FakePreviewProvider(bool throwOnBuild = false)
        {
            _throwOnBuild = throwOnBuild;
        }

        public async Task<PreviewData> BuildAsync(DumpConfig config, CancellationToken ct)
        {
            // Task.Yield() buộc yield thật sự — cho phép assert Scanning trước khi hoàn tất
            await Task.Yield();

            if (_throwOnBuild)
                throw new InvalidOperationException("Lỗi giả lập trong quá trình quét.");

            return new PreviewData(
                AsciiTree: "cây giả",
                Files: new List<FileEntry>(),
                EstimatedTokens: 100,
                TotalBytes: 1024);
        }
    }

    private class FakeDumpWriter : IDumpWriter
    {
        public async Task<DumpResult> WriteAsync(
            DumpConfig config,
            IReadOnlyList<FileEntry> files,
            string asciiTree,
            IProgress<DumpProgress>? progress,
            CancellationToken ct)
        {
            // CÂU GIỜ BẰNG Task.Delay KÈM THEO TOKEN (ct)
            // Thay vì dùng Task.Yield() quá nhanh, ta bắt nó đợi 200ms.
            // Nếu trong lúc đợi mà test gọi CancelCommand, Task.Delay sẽ NGAY LẬP TỨC
            // ném ra lỗi TaskCanceledException (kế thừa từ OperationCanceledException).
            // Nhờ đó ViewModel bắt được lỗi Hủy và chuyển về Previewed.
            await Task.Delay(200, ct);

            return new DumpResult(
                OutputPath: config.OutputPath,
                FileCount: files.Count,
                EstimatedTokens: 0,
                TotalBytes: 0,
                Duration: TimeSpan.Zero);
        }
    }
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