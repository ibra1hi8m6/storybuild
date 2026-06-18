using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.AI
{
    public class CloudinaryService(
        IConfiguration configuration,
        ILogger<CloudinaryService> logger)
    {
        private Cloudinary CreateClient()
        {
            var cloudName = configuration["Cloudinary:CloudName"]
                ?? throw new InvalidOperationException("Cloudinary:CloudName is missing.");
            var apiKey = configuration["Cloudinary:ApiKey"]
                ?? throw new InvalidOperationException("Cloudinary:ApiKey is missing.");
            var apiSecret = configuration["Cloudinary:ApiSecret"]
                ?? throw new InvalidOperationException("Cloudinary:ApiSecret is missing.");

            var account = new Account(cloudName, apiKey, apiSecret);
            return new Cloudinary(account) { Api = { Secure = true } };
        }

        public async Task<string> UploadImageBytesAsync(byte[] bytes, string publicId, string folder)
        {
            var cld = CreateClient();
            using var ms = new MemoryStream(bytes);

            var uploadParams = new ImageUploadParams
            {
                File      = new FileDescription(publicId + ".png", ms),
                PublicId  = $"{folder}/{publicId}",
                Overwrite = true
            };

            var result = await cld.UploadAsync(uploadParams);

            if (result.Error is not null)
                throw new InvalidOperationException($"Cloudinary upload error: {result.Error.Message}");

            var url = result.SecureUrl.ToString();
            logger.LogInformation("[Cloudinary] Image uploaded → {Url}", url);
            return url;
        }

        public async Task<string> UploadRawBytesAsync(byte[] bytes, string fileName, string folder)
        {
            var cld = CreateClient();
            using var ms = new MemoryStream(bytes);

            var uploadParams = new RawUploadParams
            {
                File      = new FileDescription(fileName, ms),
                PublicId  = $"{folder}/{Path.GetFileNameWithoutExtension(fileName)}",
                Overwrite = true
            };

            var result = await cld.UploadAsync(uploadParams);

            if (result.Error is not null)
                throw new InvalidOperationException($"Cloudinary raw upload error: {result.Error.Message}");

            var url = result.SecureUrl.ToString();
            logger.LogInformation("[Cloudinary] Raw uploaded → {Url}", url);
            return url;
        }
    }
}
