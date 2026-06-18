# Team 1 — Backend: AI Pipeline & OCR

**Owner:** Backend developer with AI/API experience  
**Stack:** C#, ASP.NET Core, Gemini API, Tesseract, PdfPig, SkiaSharp

---

## Your Responsibility

You own everything related to AI services, OCR, fluency evaluation, and the RAG pipeline inside `Infrastructure/AI/`, `Infrastructure/Pdf/`, and `Infrastructure/Rag/`.

---

## Current Bugs to Fix

### BUG-1: Gemini Vision OCR returns 429 on every upload
**File:** `Infrastructure/Pdf/UploadedStoryImportService.cs`  
**Problem:** The free Gemini API key hits its rate limit when uploading story PDFs. Even with retry logic (8s / 15s / 30s), the quota is daily-exhausted.  
**Fix needed:**
- Add a log line showing the API key being used (masked) so we know which key is active
- Consider splitting OCR into a background job (queue the 3 pages, process one per minute)
- OR expose an admin endpoint `POST /api/admin/story/{id}/reprocess-ocr` so OCR can be retried on demand without re-uploading the PDF

### BUG-2: PdfPig returns empty for image-based PDFs
**File:** `Infrastructure/Pdf/UploadedStoryImportService.cs` → `ExtractTextFromPdfPage()`  
**Problem:** Many Arabic story PDFs are image-based (no embedded text). PdfPig always returns empty, so Gemini OCR is always called and hits 429.  
**Fix needed:**
- Add a log: `[StoryImport] PdfPig extracted {N} chars from page {P}` so we know when it works
- If PdfPig consistently returns empty for this PDF, skip the PdfPig attempt entirely on retry

### BUG-3: Fluency STT returns empty transcription
**File:** `Infrastructure/AI/GeminiFluencyAssessorAgent.cs`  
**Problem:** When Gemini transcribes audio and returns empty text, the accuracy score is 0% even if the student read correctly.  
**Fix needed:**
- Log the raw Gemini JSON response when transcription is empty so we can debug
- If all 3 STT attempts return empty, return a special `FluencyReportDto` with `accuracyScore = -1` so the frontend can show "التقييم غير متاح حالياً" instead of 0%

### BUG-4: Image compression silently falls back to uncompressed PNG
**File:** `Infrastructure/Pdf/UploadedStoryImportService.cs` → `ExtractSentenceAsync()`  
**Problem:** If SkiaSharp resize fails (e.g. corrupt image), we fall back to the raw bytes. A 3MB PNG still causes connection resets with Gemini.  
**Fix needed:**
- In the catch block, try a simpler JPEG encode without resize before giving up
- Log the fallback clearly: `[StoryImport] Compression failed, using raw bytes ({N}KB) — may hit Gemini size limit`

### BUG-5: WritingAttempt judge always uses Gemini Vision but has no retry
**File:** `Infrastructure/AI/GeminiJudgeService.cs`  
**Problem:** Writing evaluation sends a canvas image to Gemini Vision with no retry on 429.  
**Fix needed:** Add the same 3-attempt retry pattern used in `GeminiFluencyAssessorAgent`.

---

## Tasks to Build

### TASK-1: Admin endpoint to manually set a page sentence
Sometimes OCR fails and the admin needs to manually enter the sentence for a story page.

```
PUT /api/admin/story-pages/{pageId}/sentence
Body: { "sentence": "ذهب الأرنب إلى الغابة" }
```

Add this to `AdminController.cs`.

### TASK-2: OCR status field on StoryPage
Add a field `OcrStatus` (enum: `Pending`, `Success`, `Failed`) to `StoryPage` entity so the admin dashboard can show which pages have missing text.

### TASK-3: RAG query endpoint — fix Arabic chunking
**File:** `Infrastructure/Rag/ArabicTextChunker.cs`  
Test with real Arabic lesson content and verify chunks are meaningful (not splitting mid-word). Add a unit test.

---

## Files You Own

```
Infrastructure/AI/
├── GeminiFluencyAssessorAgent.cs     ← STT + scoring
├── GeminiJudgeService.cs             ← Writing evaluation
├── GeminiOcrService.cs               ← Lesson OCR
├── GeminiStoryGeneratorService.cs    ← Story text generation
├── GeminiExamGeneratorService.cs     ← MCQ generation
├── GeminiTextCleanupService.cs       ← Text post-processing
├── CloudinaryService.cs              ← Shared upload helper
├── CloudinaryAudioStorageService.cs
└── CloudinaryImageStorageService.cs

Infrastructure/Pdf/
├── UploadedStoryImportService.cs     ← PDF story import + OCR
├── PdfImportService.cs               ← Lesson PDF import
└── PdfPageRenderer.cs                ← PDF → images

Infrastructure/Rag/
├── RagIngestionService.cs
├── RagQueryService.cs
├── ArabicTextChunker.cs
└── OllamaEmbeddingService.cs
```

---

## How to Test

1. Upload a PDF story via `/admin/uploaded-stories`
2. Watch backend logs for `[StoryImport]` lines
3. Check that sentences appear in `/uploaded-stories/{id}/journey`
4. Record a reading → check fluency score appears in parent portal
