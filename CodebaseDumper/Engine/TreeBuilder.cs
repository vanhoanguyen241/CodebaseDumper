// CodebaseDumper/Engine/TreeBuilder.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CodebaseDumper.Models;

namespace CodebaseDumper.Engine;

/// <summary>
/// Triển khai mặc định của <see cref="ITreeBuilder"/>.
/// Thứ tự hiển thị các mục con phụ thuộc vào thứ tự xuất hiện trong danh sách đầu vào,
/// giúp kết quả luôn nhất quán với cách sắp xếp của <c>FileScanner</c>.
/// </summary>
public sealed class TreeBuilder : ITreeBuilder
{
    /// <inheritdoc />
    public string Build(IReadOnlyList<FileEntry> files, string rootPath)
    {
        // Danh sách rỗng trả về thư mục gốc rỗng
        if (files.Count == 0)
        {
            return "./";
        }

        // Chuẩn hoá đường dẫn hiển thị: thay dấu phân cách hệ thống bằng '/'
        List<string> displayPaths = files
            .Select(f => NormalizePath(f.RelativePath, rootPath))
            .ToList();

        // Xây dựng cây từ danh sách đường dẫn (giữ nguyên thứ tự chèn)
        TreeNode root = BuildTree(displayPaths);

        var sb = new StringBuilder();

        // Nếu chỉ có một thư mục con ở gốc, dùng thư mục đó làm gốc hiển thị
        if (root.Children.Count == 1 && root.Children[0].IsDirectory)
        {
            var topDir = root.Children[0];
            sb.AppendLine(topDir.Name + "/");
            RenderChildren(sb, topDir.Children, string.Empty);
        }
        else
        {
            // Có nhiều hơn một mục hoặc mục duy nhất là tệp → hiển thị "./"
            sb.AppendLine("./");
            RenderChildren(sb, root.Children, string.Empty);
        }

        // Loại bỏ dòng trống cuối cùng (AppendLine để lại ký tự xuống dòng)
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Chuẩn hoá đường dẫn tương đối: loại bỏ tiền tố gốc, thay \ bằng /.
    /// </summary>
    private static string NormalizePath(string relativePath, string rootPath)
    {
        string path = relativePath;

        // Đảm bảo rootPath không có dấu phân cách cuối để so sánh chính xác
        string cleanRoot = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (path.StartsWith(cleanRoot, StringComparison.OrdinalIgnoreCase))
        {
            path = path[cleanRoot.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        // Thay thế dấu phân cách hệ thống bằng '/'
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// Xây dựng cây từ danh sách đường dẫn đã chuẩn hoá.
    /// Các nút được thêm vào theo đúng thứ tự duyệt danh sách đầu vào.
    /// </summary>
    private static TreeNode BuildTree(List<string> paths)
    {
        var root = new TreeNode(string.Empty, isDirectory: true);

        foreach (string path in paths)
        {
            string[] segments = path.Split('/');
            TreeNode current = root;

            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];
                bool isLast = i == segments.Length - 1;

                // Thư mục nếu chưa phải phần tử cuối, ngược lại là tệp
                TreeNode? child = current.Children.FirstOrDefault(c =>
                    c.Name.Equals(segment, StringComparison.Ordinal) && c.IsDirectory == !isLast);

                if (child == null)
                {
                    child = new TreeNode(segment, isDirectory: !isLast);
                    current.Children.Add(child);
                }

                current = child;
            }
        }

        return root;
    }

    /// <summary>
    /// Duyệt và in ra các dòng của cây, giữ nguyên thứ tự con đã được thêm vào.
    /// </summary>
    /// <param name="sb">StringBuilder nhận kết quả.</param>
    /// <param name="children">Danh sách con theo thứ tự xuất hiện trong dữ liệu gốc.</param>
    /// <param name="indent">Tiền tố thụt đầu dòng từ cha.</param>
    private static void RenderChildren(StringBuilder sb, List<TreeNode> children, string indent)
    {
        for (int i = 0; i < children.Count; i++)
        {
            TreeNode child = children[i];
            bool isLast = i == children.Count - 1;
            string connector = isLast ? "└── " : "├── ";
            string suffix = child.IsDirectory ? "/" : string.Empty;

            sb.AppendLine(indent + connector + child.Name + suffix);

            if (child.IsDirectory)
            {
                string childIndent = indent + (isLast ? "    " : "│   ");
                RenderChildren(sb, child.Children, childIndent);
            }
        }
    }

    /// <summary>
    /// Nút trong cây thư mục.
    /// </summary>
    private sealed class TreeNode
    {
        public string Name { get; }
        public bool IsDirectory { get; }
        public List<TreeNode> Children { get; } = new();

        public TreeNode(string name, bool isDirectory)
        {
            Name = name;
            IsDirectory = isDirectory;
        }
    }
}