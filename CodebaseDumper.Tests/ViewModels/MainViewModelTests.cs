// CodebaseDumper.Tests/ViewModels/MainViewModelTests.cs

using System.ComponentModel;
using System.Windows.Input;
using CodebaseDumper.Engine;
using CodebaseDumper.Models;
using CodebaseDumper.ViewModels;

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
        // Chờ một chút để quá trình huỷ hoàn tất (sẽ quay về Previewed)
        await Task.Delay(200);
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

    // ---------- fakes ----------

    private class FakePreviewProvider : IPreviewProvider
    {
        private readonly bool _throwOnBuild;

        public FakePreviewProvider(bool throwOnBuild = false)
        {
            _throwOnBuild = throwOnBuild;
        }

        public Task<PreviewData> BuildAsync(DumpConfig config, CancellationToken ct)
        {
            if (_throwOnBuild)
                throw new InvalidOperationException("Lỗi giả lập trong quá trình quét.");

            var data = new PreviewData(
                AsciiTree: "cây giả",
                Files: new List<FileEntry>(),
                EstimatedTokens: 100,
                TotalBytes: 1024
            );
            return Task.FromResult(data);
        }
    }

    private class FakeDumpWriter : IDumpWriter
    {
        public Task<DumpResult> WriteAsync(
            DumpConfig config,
            IReadOnlyList<FileEntry> files,
            string asciiTree,
            IProgress<DumpProgress>? progress,
            CancellationToken ct)
        {
            var result = new DumpResult(
                OutputPath: config.OutputPath,
                FileCount: files.Count,
                EstimatedTokens: 0,
                TotalBytes: 0,
                Duration: TimeSpan.Zero);
            return Task.FromResult(result);
        }
    }
}