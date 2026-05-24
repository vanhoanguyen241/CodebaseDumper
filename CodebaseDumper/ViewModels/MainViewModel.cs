// CodebaseDumper/ViewModels/MainViewModel.cs

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CodebaseDumper.Engine;
using CodebaseDumper.Models;

// Cho phép dự án kiểm thử truy cập thành viên internal (CurrentScanTask).
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("CodebaseDumper.Tests")]

namespace CodebaseDumper.ViewModels;

/// <summary>
/// ViewModel chính điều khiển luồng trạng thái quét – xem trước – kết xuất.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly IPreviewProvider _previewProvider;
    private readonly IDumpWriter _dumpWriter;

    private AppState _state = AppState.Idle;
    private string _rootPath = string.Empty;
    private PreviewData? _preview;
    private string? _errorMessage;
    private int _exportProgress;

    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _exportCts;
    private TaskCompletionSource? _scanCompletionSource;

    /// <summary>
    /// Tác vụ quét hiện tại – chỉ dùng cho kiểm thử để chờ hoàn tất quét.
    /// Trả về một task hoàn thành khi quá trình quét kết thúc (thành công hoặc lỗi).
    /// Task này không bao giờ ném ngoại lệ.
    /// </summary>
    internal Task? CurrentScanTask => _scanCompletionSource?.Task;

    public MainViewModel(IPreviewProvider previewProvider, IDumpWriter dumpWriter)
    {
        _previewProvider = previewProvider ?? throw new ArgumentNullException(nameof(previewProvider));
        _dumpWriter = dumpWriter ?? throw new ArgumentNullException(nameof(dumpWriter));

        ScanCommand = new RelayCommand(_ => TriggerScan(), _ => true);
        ExportCommand = new RelayCommand(_ => ExportAsync(), _ => State == AppState.Previewed);
        CancelCommand = new RelayCommand(_ => CancelExport(), _ => State == AppState.Exporting);
    }

    /// <summary>Trạng thái hiện tại của ViewModel.</summary>
    public AppState State
    {
        get => _state;
        private set
        {
            if (_state == value) return;
            _state = value;
            OnPropertyChanged();
            // Cập nhật trạng thái khả dụng của lệnh
            ((RelayCommand)ExportCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Đường dẫn thư mục gốc cần quét.
    /// Mỗi khi thay đổi (khác rỗng) tự động kích hoạt quét.
    /// </summary>
    public string RootPath
    {
        get => _rootPath;
        set
        {
            if (_rootPath == value) return;
            _rootPath = value ?? string.Empty;
            OnPropertyChanged();

            if (!string.IsNullOrWhiteSpace(_rootPath))
            {
                // Tự động kích hoạt lệnh quét khi đường dẫn thay đổi
                ScanCommand.Execute(null);
            }
        }
    }

    /// <summary>Dữ liệu xem trước sau khi quét thành công.</summary>
    public PreviewData? Preview
    {
        get => _preview;
        private set { _preview = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Thông báo lỗi khi ở trạng thái Error (không bao giờ null ở trạng thái đó).
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set { _errorMessage = value; OnPropertyChanged(); }
    }

    /// <summary>Tiến trình kết xuất (0 – 100).</summary>
    public int ExportProgress
    {
        get => _exportProgress;
        private set { _exportProgress = value; OnPropertyChanged(); }
    }

    /// <summary>Lệnh quét (cũng được tự động gọi khi RootPath thay đổi).</summary>
    public ICommand ScanCommand { get; }

    /// <summary>Lệnh kết xuất – chỉ khả dụng khi đã có dữ liệu xem trước.</summary>
    public ICommand ExportCommand { get; }

    /// <summary>Lệnh huỷ – chỉ khả dụng khi đang kết xuất.</summary>
    public ICommand CancelCommand { get; }

    /// <summary>Kích hoạt quét thư mục và dựng dữ liệu xem trước.</summary>
    private async void TriggerScan()
    {
        // Huỷ lần quét trước đó nếu có
        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        // Tạo một TaskCompletionSource để báo hiệu khi quá trình quét hoàn tất
        var tcs = new TaskCompletionSource();
        _scanCompletionSource = tcs;

        // Reset trạng thái lỗi trước khi bắt đầu
        ErrorMessage = null;
        State = AppState.Scanning;

        var config = new DumpConfig { RootPath = _rootPath };
        var scanTask = Task.Run(async () =>
        {
            var data = await _previewProvider.BuildAsync(config, ct);
            Preview = data;
            State = AppState.Previewed;
        }, ct);

        try
        {
            await scanTask;
        }
        catch (OperationCanceledException)
        {
            // Người dùng huỷ hoặc đường dẫn thay đổi → quay về Idle
            State = AppState.Idle;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            State = AppState.Error;
        }
        finally
        {
            // Báo hiệu hoàn tất (không ném ngoại lệ ra ngoài)
            tcs.TrySetResult();
        }
    }

    /// <summary>Thực hiện kết xuất (dùng bản nháp IDumpWriter).</summary>
    private async void ExportAsync()
    {
        if (State != AppState.Previewed) return;

        // Huỷ tác vụ kết xuất trước đó (nếu có)
        _exportCts?.Cancel();
        _exportCts?.Dispose();
        _exportCts = new CancellationTokenSource();
        var ct = _exportCts.Token;

        State = AppState.Exporting;
        ExportProgress = 0;

        try
        {
            // Mô phỏng tiến trình kết xuất (sẽ thay bằng IDumpWriter thực sau)
            for (int i = 0; i <= 100; i += 10)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(50, ct); // giả lập công việc
                ExportProgress = i;
            }

            State = AppState.Done;
            ExportProgress = 100;
        }
        catch (OperationCanceledException)
        {
            // Huỷ → trở về trạng thái Previewed
            State = AppState.Previewed;
            ExportProgress = 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            State = AppState.Error;
            ExportProgress = 0;
        }
    }

    /// <summary>Huỷ kết xuất đang chạy và quay về Previewed.</summary>
    private void CancelExport()
    {
        _exportCts?.Cancel();
        // Việc quay về Previewed sẽ do khối catch trong ExportAsync xử lý
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    /// <summary>
    /// Lớp RelayCommand đơn giản, không phụ thuộc WPF ngoài System.Windows.Input.
    /// </summary>
    private class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?> _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        }

        public bool CanExecute(object? parameter) => _canExecute(parameter);

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}