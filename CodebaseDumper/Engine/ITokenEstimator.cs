// CodebaseDumper/Engine/ITokenEstimator.cs

namespace CodebaseDumper.Engine;

/// <summary>
/// Giao diện ước tính số lượng token dựa trên tổng dung lượng byte của tệp.
/// </summary>
public interface ITokenEstimator
{
    /// <summary>
    /// Ước tính token cl100k_base: <c>ceil(totalBytes / 4.0)</c>.
    /// Không đọc nội dung tệp, chỉ dựa vào tổng byte.
    /// </summary>
    /// <param name="totalBytes">Tổng dung lượng byte của các tệp.</param>
    /// <returns>Số token ước lượng.</returns>
    int Estimate(long totalBytes);
}