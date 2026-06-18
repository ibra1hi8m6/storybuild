using Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces
{
    public interface IAudioStorageService
    {
        Task<string> UploadAudioAsync(IFormFile audioBlob, string folder = "lughati/audio");
    }

    public interface IImageStorageService
    {
        Task<string> UploadImageAsync(byte[] bytes, string fileName, string folder = "lughati/images");
    }

    public interface IFluencyAssessorAgent
    {
        Task<FluencyReportDto> EvaluateReadingAsync(
            Guid studentId,
            Guid pageId,
            string pageType,
            IFormFile audio,
            string expectedText);
    }

    public interface IAudioRecordingRepository
    {
        Task<Domain.Entities.AudioRecording> SaveAsync(Domain.Entities.AudioRecording recording);
        Task<Domain.Entities.FluencyReport> SaveReportAsync(Domain.Entities.FluencyReport report);
        Task<List<Domain.Entities.AudioRecording>> GetByStudentIdAsync(Guid studentId);
        Task<List<Domain.Entities.AudioRecording>> GetByPageIdAsync(Guid pageId, Guid studentId);
    }
}
