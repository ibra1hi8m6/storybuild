using Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces
{
    // ── PDF Rendering ──────────────────────────────────────────────────────────
    public interface IPdfPageRenderer
    {
        Task<List<string>> RenderPagesAsync(string pdfPath, string outputDirectory, CancellationToken ct = default);
    }

    // ── PDF Import (Admin book/story pipeline) ─────────────────────────────────
    public interface IPdfImportService
    {
        Task<LessonDetailResponse> ImportBookAsync(
            int level,
            string letter,
            string letterName,
            string title,
            IFormFile pdfFile,
            CancellationToken ct = default);
    }

    public interface IUploadedStoryService
    {
        Task<UploadedStoryDto> ImportAsync(string title, IFormFile pdfFile, CancellationToken ct = default);
        Task<List<UploadedStoryDto>> GetAllAsync();
        Task<UploadedStoryDto?> GetByIdAsync(Guid id);
        Task<bool> DeleteAsync(Guid id);
    }
}
