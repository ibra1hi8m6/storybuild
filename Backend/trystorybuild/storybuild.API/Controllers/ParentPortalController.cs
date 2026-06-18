using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/parent-portal")]
    public class ParentPortalController(IAudioRecordingRepository recordingRepo) : ControllerBase
    {
        [HttpGet("child/{studentId:guid}/recordings")]
        public async Task<IActionResult> GetChildRecordings(Guid studentId)
        {
            var recordings = await recordingRepo.GetByStudentIdAsync(studentId);
            return Ok(recordings.Select(r => new
            {
                r.Id,
                r.StudentId,
                r.PageId,
                r.PageType,
                r.AudioFileUrl,
                r.DurationSeconds,
                r.CreatedAt,
                Report = r.Report == null ? null : new
                {
                    r.Report.WCPM,
                    r.Report.AccuracyScore,
                    r.Report.ExpectedText,
                    r.Report.ExtractedText,
                    r.Report.MispronouncedWordsJson,
                    r.Report.CreatedAt
                }
            }));
        }
    }
}
