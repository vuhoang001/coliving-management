namespace Coliving.Application.Common;

/// <summary>Sinh mã nghiệp vụ ngắn, dễ đọc (booking, hoá đơn, sự cố...).</summary>
public static class CodeGenerator
{
    /// <summary>Tạo mã dạng PREFIX-yyMMdd-XXXX (XXXX ngẫu nhiên).</summary>
    public static string New(string prefix)
    {
        var rnd = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"{prefix}-{DateTime.UtcNow:yyMMdd}-{rnd}";
    }
}
