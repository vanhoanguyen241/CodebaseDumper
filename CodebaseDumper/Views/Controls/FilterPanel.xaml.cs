// CodebaseDumper/Views/Controls/FilterPanel.xaml.cs
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using CodebaseDumper.ViewModels;

namespace CodebaseDumper.Views.Controls;

/// <summary>
/// Bảng lọc với các chip danh sách glob bao gồm, loại trừ thư mục và loại trừ file.
/// </summary>
public partial class FilterPanel : System.Windows.Controls.UserControl
{
    /// <summary>Thuộc tính phụ thuộc cho danh sách glob bao gồm.</summary>
    public static readonly DependencyProperty IncludeGlobsProperty =
        DependencyProperty.Register(
            nameof(IncludeGlobs),
            typeof(ObservableCollection<string>),
            typeof(FilterPanel),
            new PropertyMetadata(null));

    /// <summary>Thuộc tính phụ thuộc cho danh sách thư mục loại trừ.</summary>
    public static readonly DependencyProperty ExcludeDirsProperty =
        DependencyProperty.Register(
            nameof(ExcludeDirs),
            typeof(ObservableCollection<string>),
            typeof(FilterPanel),
            new PropertyMetadata(null));

    public FilterPanel()
    {
        InitializeComponent();
    }

    /// <summary>Danh sách các mẫu glob được bao gồm.</summary>
    public ObservableCollection<string> IncludeGlobs
    {
        get => (ObservableCollection<string>)GetValue(IncludeGlobsProperty);
        set => SetValue(IncludeGlobsProperty, value);
    }

    /// <summary>Danh sách các thư mục bị loại trừ.</summary>
    public ObservableCollection<string> ExcludeDirs
    {
        get => (ObservableCollection<string>)GetValue(ExcludeDirsProperty);
        set => SetValue(ExcludeDirsProperty, value);
    }

    /// <summary>Xử lý nhấn nút preset JS/TS – thay thế toàn bộ danh sách IncludeGlobs.</summary>
    private void JsTs_Click(object sender, RoutedEventArgs e)
    {
        SetIncludeGlobs(new[] { "*.js", "*.ts", "*.jsx", "*.tsx", "*.json" });
    }

    /// <summary>Xử lý nhấn nút preset Python.</summary>
    private void Python_Click(object sender, RoutedEventArgs e)
    {
        SetIncludeGlobs(new[] { "*.py", "*.toml", "*.cfg", "*.ini" });
    }

    /// <summary>Xử lý nhấn nút preset C#.</summary>
    private void CSharp_Click(object sender, RoutedEventArgs e)
    {
        SetIncludeGlobs(new[] { "*.cs", "*.csproj", "*.sln", "*.json" });
    }

    /// <summary>Xử lý nhấn nút preset Go.</summary>
    private void Go_Click(object sender, RoutedEventArgs e)
    {
        SetIncludeGlobs(new[] { "*.go", "*.mod", "*.sum" });
    }

    /// <summary>
    /// Nút [Generated]: đặt lại danh sách file được tạo tự động phổ biến.
    /// </summary>
    private void Generated_Click(object sender, RoutedEventArgs e)
    {
        var vm = (MainViewModel)DataContext;
        vm.ExcludeFiles.Clear();
        foreach (var p in new[] { "*.min.js", "*.min.css", "*.map", "*.d.ts", "*.snap" })
            vm.ExcludeFiles.Add(p);
    }

    /// <summary>
    /// Nút [Lock files]: đặt lại danh sách lock file package manager phổ biến.
    /// </summary>
    private void LockFiles_Click(object sender, RoutedEventArgs e)
    {
        var vm = (MainViewModel)DataContext;
        vm.ExcludeFiles.Clear();
        foreach (var p in new[] { "package-lock.json", "yarn.lock", "*.lock" })
            vm.ExcludeFiles.Add(p);
    }

    /// <summary>
    /// Xoá toàn bộ chip hiện có và thêm tập hợp mới vào danh sách IncludeGlobs.
    /// </summary>
    private void SetIncludeGlobs(string[] newGlobs)
    {
        var vm = (MainViewModel)DataContext;
        vm.IncludeGlobs.Clear();
        foreach (var g in newGlobs) vm.IncludeGlobs.Add(g);
    }
}