using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/audio")]
    public class AudioController(
        ITtsService ttsService,
        ILogger<AudioController> logger) : ControllerBase
    {
        [HttpPost("tts")]
        public async Task<IActionResult> GenerateTts([FromBody] TtsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { error = "text is required" });

            var voice = string.IsNullOrWhiteSpace(request.Voice) ? "Kore" : request.Voice;

            try
            {
                var result = await ttsService.GenerateOrGetAudioAsync(request.Text, voice);
                return Ok(new TtsResponse(result.AudioUrl, result.FromCache));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Gemini:ApiKey"))
            {
                logger.LogError("[TTS] API key not configured");
                return StatusCode(503, new { error = "TTS service is not configured" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[TTS] Failed to generate audio for: '{Text}'", request.Text);
                return StatusCode(500, new { error = "Failed to generate audio. Please try again." });
            }
        }
    }
}
