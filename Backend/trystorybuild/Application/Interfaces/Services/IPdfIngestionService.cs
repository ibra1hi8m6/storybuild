using Application.DTOs;

namespace Application.Interfaces
{
    // ── Educational PDF Ingestion Service ──────────────────────────────────────
    public interface IEducationalPdfIngestionService
    {
        Task<IngestDocumentResponse> IngestAsync(
            Stream pdfStream,
            string fileName,
            int level,
            string letter,
            string letterName,
            CancellationToken ct = default);
    }
}
