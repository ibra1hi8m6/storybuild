using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.AI
{
    public class CloudinaryImageService(
        CloudinaryService cloudinaryService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CloudinaryImageService> logger) : IImageGenerationService
    {
        private const string Model = "@cf/black-forest-labs/flux-2-klein-9b";

        public async Task<string> GenerateImageAsync(string prompt, string fileName)
        {
            var accountId = configuration["Cloudflare:AccountId"];
            var apiToken  = configuration["Cloudflare:ApiToken"];

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(apiToken))
            {
                logger.LogWarning("[CloudinaryImage] Missing Cloudflare credentials — uploading placeholder.");
                return await UploadPlaceholderAsync(fileName);
            }

            try
            {
                var client = httpClientFactory.CreateClient("Cloudflare");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);

                var url = $"https://api.cloudflare.com/client/v4/accounts/{accountId}/ai/run/{Model}";

                var safePrompt = ContainsArabic(prompt)
                    ? "cartoon style, bright colors, child-friendly, cute animal characters in a sunny garden"
                    : "cartoon style, bright colors, child-friendly, " + prompt;

                var numSteps = int.TryParse(configuration["Cloudflare:NumSteps"], out var s) ? s : 8;
                var width    = int.TryParse(configuration["Cloudflare:Width"],    out var w) ? w : 512;
                var height   = int.TryParse(configuration["Cloudflare:Height"],   out var h) ? h : 512;

                var form = new MultipartFormDataContent();
                form.Add(new StringContent(safePrompt),          "prompt");
                form.Add(new StringContent(numSteps.ToString()), "num_steps");
                form.Add(new StringContent(width.ToString()),    "width");
                form.Add(new StringContent(height.ToString()),   "height");

                logger.LogInformation("[CloudinaryImage] Generating — {File}", fileName);
                var resp = await client.PostAsync(url, form);
                resp.EnsureSuccessStatusCode();

                var result = await resp.Content.ReadFromJsonAsync<CfImageResponse>();
                if (result?.Result?.Image is null)
                    throw new InvalidOperationException("Cloudflare returned no image data.");

                var bytes    = Convert.FromBase64String(result.Result.Image);
                var publicId = Path.GetFileNameWithoutExtension(fileName);
                var imageUrl = await cloudinaryService.UploadImageBytesAsync(bytes, publicId, "lughati/stories");

                logger.LogInformation("[CloudinaryImage] Stored on Cloudinary → {Url}", imageUrl);
                return imageUrl;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[CloudinaryImage] Failed for {File} — uploading placeholder", fileName);
                return await UploadPlaceholderAsync(fileName);
            }
        }

        private async Task<string> UploadPlaceholderAsync(string fileName)
        {
            var png = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
            var publicId = Path.GetFileNameWithoutExtension(fileName);
            return await cloudinaryService.UploadImageBytesAsync(png, publicId, "lughati/placeholders");
        }

        private static bool ContainsArabic(string text) =>
            text.Any(c => c >= '؀' && c <= 'ۿ');

        private sealed class CfImageResponse
        {
            [JsonPropertyName("result")]  public CfImageResult? Result  { get; set; }
            [JsonPropertyName("success")] public bool           Success { get; set; }
        }

        private sealed class CfImageResult
        {
            [JsonPropertyName("image")] public string? Image { get; set; }
        }
    }
}
