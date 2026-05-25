using CodebaseDumper.Engine;
using CodebaseDumper.ViewModels;
using CodebaseDumper.Views;
using System.Windows;

namespace CodebaseDumper;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Khởi tạo các dependency cấp thấp (Tuyệt đối không dùng null)
        IFileScanner scanner = new FileScanner();
        ITreeBuilder treeBuilder = new TreeBuilder();
        ITokenEstimator estimator = new TokenEstimator();

        // 2. Tiêm chúng vào IPreviewProvider
        IPreviewProvider previewProvider = new PreviewProvider(scanner, treeBuilder, estimator);

        // 3. Khởi tạo IDumpWriter (Task 9A)
        IDumpWriter dumpWriter = new DumpWriter();

        // 4. Tạo ViewModel chính với đầy đủ dependency
        var mainViewModel = new MainViewModel(previewProvider, dumpWriter);

        // 5. Gán DataContext cho MainWindow và hiển thị
        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };

        mainWindow.Show();
    }
}