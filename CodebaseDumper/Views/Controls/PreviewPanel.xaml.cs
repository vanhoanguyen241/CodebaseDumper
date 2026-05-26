// CodebaseDumper/Views/Controls/PreviewPanel.xaml.cs
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using CodebaseDumper.Models;
using CodebaseDumper.ViewModels;

namespace CodebaseDumper.Views.Controls;

/// <summary>
/// Panel xem trước hiển thị nội dung theo trạng thái quét / kết xuất.
/// </summary>
public partial class PreviewPanel : System.Windows.Controls.UserControl
{
    private DispatcherTimer? _dotsTimer;
    private int _dotCount;
    private MainViewModel? _viewModel;

    public PreviewPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            _viewModel = vm;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            // Nếu đang ở trạng thái Scanning ngay khi load, khởi động hiệu ứng
            if (_viewModel.State == AppState.Scanning)
            {
                StartDotsAnimation();
            }
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel = null;
        }
        StopDotsAnimation();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.State) && _viewModel != null)
        {
            if (_viewModel.State == AppState.Scanning)
            {
                StartDotsAnimation();
            }
            else
            {
                StopDotsAnimation();
            }
        }
    }

    /// <summary>
    /// Khởi động bộ đếm hiển thị dấu chấm chuyển động khi quét.
    /// </summary>
    private void StartDotsAnimation()
    {
        if (_dotsTimer != null) return;

        _dotCount = 0;
        _dotsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _dotsTimer.Tick += (s, e) =>
        {
            _dotCount = (_dotCount + 1) % 4; // 0 -> 1 -> 2 -> 3 -> 0
            DotsRun.Text = new string('.', _dotCount);
        };
        _dotsTimer.Start();
    }

    /// <summary>
    /// Dừng hiệu ứng dấu chấm và xoá nội dung.
    /// </summary>
    private void StopDotsAnimation()
    {
        _dotsTimer?.Stop();
        _dotsTimer = null;
        DotsRun.Text = string.Empty;
    }
}

/// <summary>
/// Bộ chuyển đổi định dạng thông tin thống kê thành chuỗi hiển thị trên thanh trạng thái.
/// </summary>
internal class PreviewStatsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PreviewData preview)
        {
            // Số lượng tệp
            int fileCount = preview.Files.Count;
            // Số token ước lượng: làm tròn về đơn vị nghìn (K)
            int tokenK = (int)Math.Round(preview.EstimatedTokens / 1000.0, MidpointRounding.AwayFromZero);
            // Dung lượng: làm tròn về KB
            int kb = (int)Math.Round(preview.TotalBytes / 1024.0, MidpointRounding.AwayFromZero);
            return $"{fileCount} files  ·  ~{tokenK}K tokens  ·  {kb} KB";
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}