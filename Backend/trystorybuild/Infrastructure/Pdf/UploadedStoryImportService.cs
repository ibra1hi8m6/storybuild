using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.Pdf;

public class UploadedStoryImportService(
    IPdfPageRenderer pdfRenderer,
    IStoryRepository storyRepository,
    IOptions<PdfImportSettings> settings,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    CloudinaryService cloudinaryService,
    ILogger<UploadedStoryImportService> logger) : IUploadedStoryService
{
    private const int SkipPages = 3;   // skip cover + credits pages
    private const int MaxContentPages = 3; // max story pages to import

    public async Task<UploadedStoryDto> ImportAsync(string title, IFormFile pdfFile, CancellationToken ct = default)
    {
        if (pdfFile.Length == 0)
            throw new InvalidOperationException("PDF file is empty.");
        if (!pdfFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only PDF files are supported.");

        var cfg = settings.Value;
        Directory.CreateDirectory(cfg.PdfUploadDirectory);

        var storyId = Guid.NewGuid();
        var pdfPath = Path.Combine(cfg.PdfUploadDirectory, $"story_{storyId}.pdf");

        await using (var stream = File.Create(pdfPath))
            await pdfFile.CopyToAsync(stream, ct);

        logger.LogInformation("[StoryImport] Saved PDF → {Path}", pdfPath);

        var imagesDir  = Path.Combine(cfg.ImagesOutputDirectory, $"story_{storyId}");
        var imagePaths = await pdfRenderer.RenderPagesAsync(pdfPath, imagesDir, ct);

        if (imagePaths.Count == 0)
            throw new InvalidOperationException("PDF contains no pages.");

        var resolvedTitle = string.IsNullOrWhiteSpace(title)
            ? Path.GetFileNameWithoutExtension(pdfFile.FileName)
            : title.Trim();

        var story = new Story
        {
            Id     = storyId,
            Title  = resolvedTitle,
            Source = StorySource.PdfImport
        };

        // Upload cover (first page) regardless of skip
        string? coverUrl = null;
        if (imagePaths.Count > 0)
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(imagePaths[0], ct);
                coverUrl  = await cloudinaryService.UploadImageBytesAsync(bytes, $"story_{storyId}_cover", "lughati/stories");
                story.CoverImagePath = coverUrl;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[StoryImport] Cover upload failed.");
            }
        }

        // Content pages start after skipping first SkipPages
        var pageNumber = 1;
        for (int i = SkipPages; i < imagePaths.Count && pageNumber <= MaxContentPages; i++)
        {
            ct.ThrowIfCancellationRequested();

            string imageUrl;
            try
            {
                var bytes    = await File.ReadAllBytesAsync(imagePaths[i], ct);
                var publicId = $"story_{storyId}_p{pageNumber}";
                imageUrl     = await cloudinaryService.UploadImageBytesAsync(bytes, publicId, "lughati/stories");
                logger.LogInformation("[StoryImport] Page {N} → Cloudinary", pageNumber);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[StoryImport] Page {N} Cloudinary upload failed — skipping.", pageNumber);
                pageNumber++;
                continue;
            }

            // Try direct PDF text extraction first (free, instant, no API)
            var sentence = ExtractTextFromPdfPage(pdfPath, i);

            if (!string.IsNullOrWhiteSpace(sentence))
            {
                logger.LogInformation("[StoryImport] Page {N} text from PdfPig: {S}", pageNumber, sentence);
            }
            else
            {
                // Fall back to Gemini Vision OCR for image-based PDFs
                if (i > SkipPages)
                    await Task.Delay(4000, ct);
                sentence = await ExtractSentenceAsync(imagePaths[i], ct);
                logger.LogInformation("[StoryImport] Page {N} text from Gemini OCR: {S}", pageNumber, sentence);
            }

            story.Pages.Add(new StoryPage
            {
                StoryId    = storyId,
                PageNumber = pageNumber,
                Sentence   = sentence,
                ImagePath  = imageUrl,
                ImagePrompt = string.Empty,
                IsUnlocked  = true
            });

            pageNumber++;
        }

        if (story.Pages.Count == 0)
            throw new InvalidOperationException("No usable content pages found after skipping the first 3.");

        if (string.IsNullOrEmpty(story.CoverImagePath) && story.Pages.Count > 0)
            story.CoverImagePath = story.Pages[0].ImagePath;

        await storyRepository.SaveAsync(story);
        logger.LogInformation("[StoryImport] Story {Id} saved with {Count} pages.", storyId, story.Pages.Count);

        return MapToDto(story);
    }

    public async Task<List<UploadedStoryDto>> GetAllAsync()
    {
        var stories = await storyRepository.GetAllAsync();
        return stories
            .Where(s => s.Source == StorySource.PdfImport)
            .OrderByDescending(s => s.CreatedAt)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<UploadedStoryDto?> GetByIdAsync(Guid id)
    {
        var story = await storyRepository.GetByIdAsync(id);
        if (story is null || story.Source != StorySource.PdfImport) return null;
        return MapToDto(story);
    }

    public Task<bool> DeleteAsync(Guid id) => storyRepository.DeleteAsync(id);

    private static UploadedStoryDto MapToDto(Story s) => new(
        s.Id,
        s.Title,
        s.CoverImagePath ?? s.Pages.FirstOrDefault()?.ImagePath ?? string.Empty,
        s.Pages.Count,
        s.CreatedAt,
        s.Pages.OrderBy(p => p.PageNumber).Select(p => new StoryPageDto(
            p.Id, p.PageNumber, p.Sentence, p.ImagePath, p.IsUnlocked)).ToList());

    private static string ExtractTextFromPdfPage(string pdfPath, int pageIndex)
    {
        try
        {
            using var doc = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
            // PdfPig pages are 1-based
            var page = doc.GetPage(pageIndex + 1);
            var text = string.Join(" ", page.GetWords().Select(w => w.Text));
            return ExtractArabicOnly(text);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ExtractArabicOnly(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        // Keep Arabic Unicode blocks + whitespace + Arabic/common punctuation
        var result = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"[^؀-ۿݐ-ݿﭐ-﷿ﹰ-﻿\s،؛؟!.,\-\(\)]",
            " ");
        result = System.Text.RegularExpressions.Regex.Replace(result.Trim(), @"\s{2,}", " ");
        return result.Trim();
    }

    private async Task<string> ExtractSentenceAsync(string imagePath, CancellationToken ct)
    {
        var apiKey = configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return string.Empty;

        // Retry delays in ms for 429 responses: 8s, 15s, 30s
        var retryDelaysMs = new[] { 8000, 15000, 30000 };

        // Resize image to JPEG ≤1024px wide to reduce payload size (avoids connection resets on large PNGs)
        byte[] imageBytes;
        try
        {
            var raw = await File.ReadAllBytesAsync(imagePath, ct);
            using var bmp = SKBitmap.Decode(raw);
            int targetW = Math.Min(bmp.Width, 1024);
            int targetH = (int)((double)bmp.Height / bmp.Width * targetW);
            using var resized = bmp.Resize(new SKImageInfo(targetW, targetH), SKFilterQuality.Medium);
            using var img = SKImage.FromBitmap(resized);
            using var encoded = img.Encode(SKEncodedImageFormat.Jpeg, 80);
            imageBytes = encoded.ToArray();
            logger.LogInformation("[StoryImport] OCR image compressed: {Orig}KB → {New}KB",
                raw.Length / 1024, imageBytes.Length / 1024);
        }
        catch
        {
            imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
        }

        for (int attempt = 0; attempt <= retryDelaysMs.Length; attempt++)
        {
            try
            {
                var base64 = Convert.ToBase64String(imageBytes);

                var body = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = "Look at this children's story page image. Extract ONLY the Arabic text (ignore any English text completely). Return ONLY the Arabic sentence or phrase as-is, with no translation, no explanation, and no additional text. If there is no Arabic text on the page, return an empty string." },
                                new { inline_data = new { mime_type = "image/jpeg", data = base64 } }
                            }
                        }
                    }
                };

                var model  = configuration["Gemini:VisionModel"] ?? "gemini-2.5-flash";
                var url    = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var client = httpClientFactory.CreateClient("Gemini");
                var resp   = await client.PostAsJsonAsync(url, body, ct);

                if ((int)resp.StatusCode == 429 && attempt < retryDelaysMs.Length)
                {
                    logger.LogWarning("[StoryImport] Gemini 429 for {Path}, retry {N} in {D}ms", imagePath, attempt + 1, retryDelaysMs[attempt]);
                    await Task.Delay(retryDelaysMs[attempt], ct);
                    continue;
                }

                resp.EnsureSuccessStatusCode();

                var responseBody = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(responseBody);
                var extracted = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()?.Trim() ?? string.Empty;

                return ExtractArabicOnly(extracted);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[StoryImport] Gemini OCR failed for {Path} (attempt {N})", imagePath, attempt + 1);
                return string.Empty;
            }
        }

        logger.LogWarning("[StoryImport] Gemini OCR exhausted all retries for {Path}", imagePath);
        return string.Empty;
    }
}
