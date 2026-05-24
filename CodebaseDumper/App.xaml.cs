using System.Windows;
using CodebaseDumper.Engine;
using CodebaseDumper.Models;
using CodebaseDumper.ViewModels;
using CodebaseDumper.Views;

namespace CodebaseDumper;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Khởi tạo thủ công các phụ thuộc (không dùng container DI)
        var fileScanner = new FileScanner();
        var treeBuilder = new TreeBuilder();
        var tokenEstimator = new TokenEstimator();
        var previewProvider = new PreviewProvider(fileScanner, treeBuilder, tokenEstimator);

        // Bản nháp IDumpWriter – sẽ được thay bằng bản thật ở giai đoạn xuất
        IDumpWriter dumpWriter = new DumpWriterStub();

        var mainViewModel = new MainViewModel(previewProvider, dumpWriter);

        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };

        // Ứng dụng tắt khi cửa sổ chính đóng
        Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        mainWindow.Show();
    }

    /// <summary>
    /// Bản nháp tạm thời cho IDumpWriter, chưa hỗ trợ xuất thực sự.
    /// </summary>
    private class DumpWriterStub : IDumpWriter
    {
        public System.Threading.Tasks.Task WriteAsync(DumpConfig config, System.Threading.CancellationToken ct)
        {
            // Tạm thời ném ngoại lệ – sẽ triển khai đầy đủ sau
            throw new System.NotImplementedException();
        }
    }
}