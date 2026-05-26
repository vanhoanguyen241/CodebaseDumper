// CodebaseDumper/Views/Controls/ActionBar.xaml.cs
using System.Diagnostics;
using System.Windows;
using CodebaseDumper.ViewModels;

namespace CodebaseDumper.Views.Controls;

/// <summary>
/// Code-behind cho ActionBar – CHỈ chứa handler mở file.
/// Không chứa logic nghiệp vụ.
/// </summary>
public partial class ActionBar
{
    public ActionBar()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Mở Explorer và highlight tệp đầu ra được chỉ định trong OutputPath.
    /// </summary>
    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && !string.IsNullOrEmpty(vm.OutputPath))
        {
            try
            {
                Process.Start("explorer.exe", $"/select,\"{vm.OutputPath}\"");
            }
            catch (Exception ex)
            {
                // Ghi lỗi ra Debug output thay vì nuốt hoàn toàn
                System.Diagnostics.Debug.WriteLine($"Không thể mở Explorer: {ex.Message}");
            }
        }
    }
}