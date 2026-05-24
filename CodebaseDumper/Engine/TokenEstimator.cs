// CodebaseDumper/Engine/TokenEstimator.cs

namespace CodebaseDumper.Engine;

/// <summary>
/// Triển khai đơn giản cho <see cref="ITokenEstimator"/>,
/// áp dụng công thức cl100k_base: ceil(tổngByte / 4.0).
/// </summary>
public class TokenEstimator : ITokenEstimator
{
    /// <inheritdoc />
    public int Estimate(long totalBytes)
    {
        // Công thức cố định: tổng byte chia 4, làm tròn lên
        return (int)Math.Ceiling(totalBytes / 4.0);
    }
}