using Coliving.Application.Common;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

/// <summary>Tải ảnh (toà nhà, sự cố, hợp đồng...) lên MinIO, trả URL công khai.</summary>
[Authorize]
public class UploadsController : BaseApiController
{
    private readonly IFileStorage _storage;
    private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxBytes = 5 * 1024 * 1024; // 5MB

    public UploadsController(IFileStorage storage) => _storage = storage;

    [HttpPost("image")]
    [RequestSizeLimit(MaxBytes)]
    public async Task<ActionResult<object>> UploadImage(IFormFile file, [FromQuery] string folder = "uploads")
    {
        if (file is null || file.Length == 0)
            throw new AppException("Chưa chọn tệp.");
        if (file.Length > MaxBytes)
            throw new AppException("Ảnh vượt quá 5MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(ext))
            throw new AppException("Định dạng ảnh không hỗ trợ (chỉ jpg, png, webp, gif).");

        await using var stream = file.OpenReadStream();
        var url = await _storage.SaveImageAsync(stream, file.Length, file.FileName, file.ContentType, folder);
        return Ok(new { url });
    }
}
