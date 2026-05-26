using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CodebaseDumper.ViewModels;

namespace CodebaseDumper.Views.Controls;

/// <summary>
/// Điều khiển chọn thư mục gốc – bao gồm TextBox cho phép nhập tay
/// và nút Browse mở FolderBrowserDialog.
/// </summary>
public partial class FolderPickerControl : System.Windows.Controls.UserControl
{
    public FolderPickerControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Mở hộp thoại chọn thư mục; nếu người dùng chọn thành công
    /// thì gán đường dẫn vào ViewModel (kích hoạt quét ngay lập tức).
    /// </summary>
    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Chọn thư mục gốc của dự án",
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null)
            {
                // Gán trực tiếp, bypass UpdateSourceTrigger=LostFocus
                vm.RootPath = dialog.SelectedPath;
            }
        }
    }

    /// <summary>
    /// Đảm bảo nút Browse phản hồi với phím Space và Enter khi đang focus.
    /// </summary>
    private void BrowseButton_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Space || e.Key == Key.Enter)
        {
            BrowseButton_Click(sender, null);
            e.Handled = true;
        }
    }
}