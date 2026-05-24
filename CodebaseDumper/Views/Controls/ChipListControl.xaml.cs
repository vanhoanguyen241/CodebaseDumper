// CodebaseDumper/Views/Controls/ChipListControl.xaml.cs
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CodebaseDumper.Views.Controls;

/// <summary>
/// Điều khiển hiển thị danh sách chip có thể xoá và thêm mới.
/// </summary>
public partial class ChipListControl : System.Windows.Controls.UserControl
{
    /// <summary>Thuộc tính phụ thuộc cho danh sách nguồn (ItemsSource).</summary>
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(ChipListControl),
            new PropertyMetadata(null));

    /// <summary>Lệnh xoá chip, có thể bind từ bên ngoài (nội bộ sẽ tự khởi tạo).</summary>
    public static readonly DependencyProperty RemoveCommandProperty =
        DependencyProperty.Register(
            nameof(RemoveCommand),
            typeof(ICommand),
            typeof(ChipListControl),
            new PropertyMetadata(null));

    public ChipListControl()
    {
        InitializeComponent();
        // Gán lệnh xoá mặc định
        RemoveCommand = new RelayCommand(RemoveChip);
    }

    /// <summary>Danh sách các phần tử hiển thị dưới dạng chip.</summary>
    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Lệnh thực thi khi nhấn nút "×" trên mỗi chip.</summary>
    public ICommand RemoveCommand
    {
        get => (ICommand)GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }

    /// <summary>Xoá chip khỏi danh sách nguồn.</summary>
    /// <param name="parameter">Chuỗi văn bản của chip cần xoá.</param>
    private void RemoveChip(object? parameter)
    {
        if (parameter is string chip && ItemsSource is IList list)
        {
            list.Remove(chip);
        }
    }

    /// <summary>Xử lý sự kiện nhấn phím trong ô nhập liệu: nếu là Enter thì thêm chip mới.</summary>
    private void AddTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            string text = AddTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(text) && ItemsSource is IList list)
            {
                list.Add(text);
                AddTextBox.Clear();
            }
            e.Handled = true;
        }
    }

    /// <summary>Lớp RelayCommand nội bộ, dùng cho lệnh xoá.</summary>
    private class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        public RelayCommand(Action<object?> execute) => _execute = execute;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter);
    }
}