# TTS Implementation Report
> Gemini TTS audio system with Cloudinary cache — replaces browser speechSynthesis across the platform.

---

## Gemini TTS Model

`gemini-2.5-flash-preview-tts`

This model is used exclusively for TTS. All other Gemini calls (writing evaluation, OCR, story/exam generation, fluency assessment) continue to use `gemini-2.5-flash` as configured in `Gemini:Model` appsettings.

---

## Cache Strategy

1. Text is normalized (trim + collapse whitespace).
2. A SHA-256 hash is computed: `SHA256(normalizedText|voice|provider)` — where `provider = "gemini"`.
3. Before calling Gemini TTS, the hash is looked up in the `TtsAudioCaches` SQL table.
4. **Cache hit:** return existing `AudioUrl` immediately. Increment `UsageCount` and update `LastUsedAt`.
5. **Cache miss:**
   - Call Gemini TTS → receive PCM L16 24000 Hz mono bytes
   - Convert PCM → WAV (44-byte RIFF header + data)
   - Upload WAV to Cloudinary
   - Save record to `TtsAudioCaches`
   - Return new `AudioUrl`

**Result:** Each unique Arabic phrase is generated exactly once, even across server restarts. Identical text with tashkeel and without tashkeel hash differently (intentional — they sound different when spoken).

---

## Cloudinary Folder

TTS audio files are stored at: `lughati/tts/`

| Folder | Content |
|---|---|
| `lughati/tts/` | System-generated TTS WAV files |
| `lughati/audio/` | Student fluency recordings |
| `lughati/writing/` | Student handwriting images |

TTS files are **never mixed** with student recordings.

---

## Database Table

`TtsAudioCaches` — added via migration `20260624120000_AddTtsAudioCache`

| Column | Type | Notes |
|---|---|---|
| `Id` | uniqueidentifier | PK |
| `Text` | nvarchar(max) | Original text as submitted |
| `NormalizedText` | nvarchar(max) | Trimmed + whitespace collapsed |
| `TextHash` | nvarchar(64) | SHA-256 hex — unique index |
| `Voice` | nvarchar(50) | e.g. `Kore` |
| `Provider` | nvarchar(50) | `gemini` |
| `MimeType` | nvarchar(50) | `audio/wav` |
| `AudioUrl` | nvarchar(max) | Cloudinary HTTPS URL |
| `PublicId` | nvarchar(max) | Cloudinary public ID |
| `CreatedAt` | datetime2 | First generation time |
| `LastUsedAt` | datetime2 | Last cache hit time |
| `UsageCount` | int | Number of plays (including cache hits) |

---

## Backend API

```
POST /api/audio/tts
Content-Type: application/json

{
  "text": "هٰذَا خُفَّاشٌ",
  "voice": "Kore"
}
```

Response:
```json
{
  "audioUrl": "https://res.cloudinary.com/.../lughati/tts/tts_abc123def456.wav",
  "fromCache": false
}
```

Voice defaults to `"Kore"` if omitted or empty. No authentication required — this is called by our own Angular frontend.

---

## Frontend Components Updated

All `speechSynthesis` TTS calls replaced with `TtsService.play()`:

| Component | File | Method replaced |
|---|---|---|
| Listen Mode | `features/fluency/reading-journey/listen-mode.component.ts` | `speak()` |
| Lesson Reader | `features/lesson-reader/lesson-reader.ts` | `speakText()`, `speakFeedback()`, `autoPlayCurrentPage()` |
| Story Reader | `features/story-reader/story-reader.ts` | `playAudio()`, `speakText()` |
| Letter Lesson | `features/learning/letters/letter-lesson.component.ts` | `speak()` |
| Letter Recognition | `features/learning/letters/letter-recognition.component.ts` | `speak()` |
| Word Practice | `features/learning/words/word-practice.component.ts` | `speak()` |
| Sentence Practice | `features/learning/sentences/sentence-practice.component.ts` | `listenOption()` |
| Placement Question | `features/placement/question/placement-question.component.ts` | `speakQuestion()` |
| Vocabulary Popup | `features/etools/components/vocabulary-popup/vocabulary-popup.component.ts` | `speak()` |
| Word Journal | `features/etools/word-journal/word-journal.component.ts` | `speak()` |

### Components NOT touched (recording/playback only)

- `record-mode.component.ts` — microphone recording
- `web-audio.service.ts` — MediaRecorder service
- `teacher-feedback.component.ts` — voice message recording
- `student-inbox.component.ts` — voice message playback
- `parent-recordings.component.ts` — fluency recording playback

---

## TtsService (`services/tts.service.ts`)

```typescript
play(text: string, voice?: string): Promise<void>
stop(): void
```

**Flow:**
1. Check in-memory session cache (key = `text|voice`)
2. If not cached: `POST /api/audio/tts` → receive `audioUrl` → store in session cache
3. Play via `HTMLAudioElement`
4. **On any error:** fall back to browser `speechSynthesis` (Arabic `ar-SA`)

**The fallback is never removed.** If the backend is unreachable, all 10 components continue to function via browser TTS.

---

## Fallback Behavior

| Condition | Behavior |
|---|---|
| Backend unreachable / 500 | `TtsService` catches the error and calls `speechSynthesis` |
| Audio playback error (CORS, codec) | `onerror` handler resolves the promise silently; playback ends |
| Empty text | `play()` returns immediately, no request made |
| Missing Gemini API key | Backend returns 503; frontend falls back to `speechSynthesis` |

---

## How to Test

### Test 1 — Basic Arabic without tashkeel
```
POST /api/audio/tts
{ "text": "هذا خفاش", "voice": "Kore" }
```
Expected: Returns `audioUrl`, `fromCache: false`. Second call returns `fromCache: true`.

### Test 2 — Same text with full tashkeel (different hash)
```
POST /api/audio/tts
{ "text": "هٰذَا خُفَّاشٌ", "voice": "Kore" }
```
Expected: Different hash → different cache entry. Gemini should produce more accurate pronunciation with tashkeel.

### Test 3 — Cache hit
Call Test 1 again. `fromCache` should be `true`. Response time should be <100ms.

### Test 4 — Frontend integration
Open any lesson page. Sentence should play automatically without the browser's default Arabic TTS. Check browser DevTools Network tab — you should see a `POST /api/audio/tts` request followed by audio file playback.

### Test 5 — Fallback
Temporarily disable the backend and open a lesson. Browser `speechSynthesis` should still play the sentence.

---

## Files Added / Modified

### Backend — New files
| File | Purpose |
|---|---|
| `Domain/Entities/TtsAudioCache.cs` | Entity for cache table |
| `Application/Interfaces/AI/ITtsService.cs` | TTS service interface |
| `Application/Interfaces/Repositories/ITtsAudioCacheRepository.cs` | Cache repository interface |
| `Application/DTOs/TtsDtos.cs` | `TtsRequest`, `TtsResponse` records |
| `Infrastructure/AI/GeminiTtsService.cs` | Gemini TTS call, PCM→WAV, Cloudinary upload |
| `Infrastructure/Repositories/TtsAudioCacheRepository.cs` | EF Core repository |
| `storybuild.API/Controllers/AudioController.cs` | `POST /api/audio/tts` endpoint |
| `Infrastructure/Migrations/20260624120000_AddTtsAudioCache.cs` | DB migration |

### Backend — Modified files
| File | Change |
|---|---|
| `Infrastructure/Data/AppDbContext.cs` | Added `DbSet<TtsAudioCache>` + unique index |
| `Infrastructure/InfrastructureExtensions.cs` | Registered `ITtsAudioCacheRepository` + `ITtsService` |

### Frontend — New files
| File | Purpose |
|---|---|
| `services/tts.service.ts` | TTS service with session cache + fallback |

### Frontend — Modified files (10 components)
All listed in the table above.
