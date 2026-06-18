using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Infrastructure.AI
{
    public class CloudinaryAudioStorageService(
        CloudinaryService cloudinaryService,
        ILogger<CloudinaryAudioStorageService> logger) : IAudioStorageService
    {
        public async Task<string> UploadAudioAsync(IFormFile audioBlob, string folder = "lughati/audio")
        {
            using var ms = new MemoryStream();
            await audioBlob.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var ext      = Path.GetExtension(audioBlob.FileName).TrimStart('.');
            var fileName = $"{Guid.NewGuid()}.{(string.IsNullOrEmpty(ext) ? "webm" : ext)}";

            logger.LogInformation("[AudioStorage] Uploading {Size}KB audio to Cloudinary", bytes.Length / 1024);
            return await cloudinaryService.UploadRawBytesAsync(bytes, fileName, folder);
        }
    }
}
