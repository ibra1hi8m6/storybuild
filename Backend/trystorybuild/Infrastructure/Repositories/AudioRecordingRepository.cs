using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AudioRecordingRepository(AppDbContext db) : IAudioRecordingRepository
    {
        public async Task<AudioRecording> SaveAsync(AudioRecording recording)
        {
            db.AudioRecordings.Add(recording);
            await db.SaveChangesAsync();
            return recording;
        }

        public async Task<FluencyReport> SaveReportAsync(FluencyReport report)
        {
            db.FluencyReports.Add(report);
            await db.SaveChangesAsync();
            return report;
        }

        public async Task<List<AudioRecording>> GetByStudentIdAsync(Guid studentId) =>
            await db.AudioRecordings
                .Include(r => r.Report)
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<List<AudioRecording>> GetByPageIdAsync(Guid pageId, Guid studentId) =>
            await db.AudioRecordings
                .Include(r => r.Report)
                .Where(r => r.PageId == pageId && r.StudentId == studentId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<List<AudioRecording>> GetByChildNameAsync(string childName, int take = 50)
        {
            // Join via Student to find recordings by child name
            var studentIds = await db.Students
                .Where(s => s.Name == childName)
                .Select(s => s.Id)
                .ToListAsync();

            if (studentIds.Count == 0) return [];

            return await db.AudioRecordings
                .Include(r => r.Report)
                .Where(r => studentIds.Contains(r.StudentId))
                .OrderByDescending(r => r.CreatedAt)
                .Take(take)
                .ToListAsync();
        }
    }
}
