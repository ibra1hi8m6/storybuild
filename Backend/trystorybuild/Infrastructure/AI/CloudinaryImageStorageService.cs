using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.AI
{
    public class CloudinaryImageStorageService(
        CloudinaryService cloudinaryService,
        ILogger<CloudinaryImageStorageService> logger) : IImageStorageService
    {
        public async Task<string> UploadImageAsync(byte[] bytes, string fileName, string folder = "lughati/images")
        {
            var publicId = Path.GetFileNameWithoutExtension(fileName);
            logger.LogInformation("[ImageStorage] Uploading {Size}KB image to Cloudinary/{Folder}", bytes.Length / 1024, folder);
            return await cloudinaryService.UploadImageBytesAsync(bytes, publicId, folder);
        }
    }
}
