# Architecture Refactor Report
> Safe refactor — no behavior changes, no DB schema changes, no route changes.
> All builds verified at 0 errors after each step.

---

## What Was Done

| Step | Change | Files Affected |
|------|--------|----------------|
| B1 | Split `IServices.cs` (234 lines, 20+ interfaces) into 12 focused interface files | 1 deleted → 12 created; 0 consumer changes |
| B2 | Split `StoryDtos.cs` (270 lines, 40+ records) into 6 focused DTO files | 1 deleted → 6 created; 0 consumer changes |
| B3 | Rename `Application/Agent/` → `Application/Agents/` | 4 agent namespaces + 7 consumer `using` statements |

**Safety technique used for B1 and B2:** All new files keep the same flat namespace
(`Application.Interfaces` and `Application.DTOs` respectively). In C#, a namespace can
span multiple files — consumers never needed new `using` statements.

---

## Interface Map (`Application/Interfaces/`)

### New — AI interfaces
`AI/IAiServices.cs` — namespace `Application.Interfaces`
| Interface | Purpose |
|-----------|---------|
| `IStoryGeneratorService` | Gemini: generate 3-page children's story |
| `IExamGeneratorService` | Gemini: generate exam from story / lesson |
| `IJudgeService` | Gemini: approve/reject story for safety |
| `IImageGenerationService` | Cloudflare AI → Cloudinary: text-to-image |
| `IOcrService` | Gemini Vision: extract Arabic text from image |
| `ITextSimilarityService` | Arabic string similarity scoring |
| `IAiTextCleanupService` | Gemini: clean OCR-extracted Arabic text |

`AI/IPdfProcessingServices.cs` — namespace `Application.Interfaces`
| Interface | Purpose |
|-----------|---------|
| `IPdfPageRenderer` | Render PDF page to image |
| `IPdfImportService` | Import PDF lesson into DB |
| `IUploadedStoryService` | Import uploaded PDF story into DB |

### New — Repository interfaces
`Repositories/IContentRepositories.cs`
→ `IStoryRepository`, `ILessonRepository`, `IExamRepository`, `IStudentProgressRepository`

`Repositories/IUserRepositories.cs`
→ `IUserRepository`, `IStudentRepository`

`Repositories/IWritingRepository.cs`
→ `IWritingAttemptRepository`

`Repositories/IConfigRepository.cs`
→ `ILevelWordConfigRepository`, `IRagPageChunkRepository`

`Repositories/IGroupRepository.cs`
→ `IStudentGroupRepository`, `ILessonAssignmentRepository`, `IAssignmentSubmissionRepository`

### New — Service interfaces
`Services/IAuthService.cs`
→ `IAuthService` (Register, Login, CreateStudent, StudentLogin, GetChildren, GetStudents, CreateSchoolAdmin, UpdateStudentLevel)

`Services/IEmailService.cs`
→ `IEmailService` (SendTeacherWelcomeAsync, SendTeacherPasswordResetAsync)

`Services/IDashboardService.cs`
→ `IDashboardService` (GetStudentDashboard, GetParentDashboard, GetTeacherDashboard, GetSchoolDashboard, GetKnownChildNames, GetLevelProgress)

`Services/IAnalyticsService.cs`
→ `IAnalyticsService` (GetWeakLetters, UpsertWeakLetter, GetClassAnalytics)

`Services/IPdfIngestionService.cs`
→ `IEducationalPdfIngestionService`

### Pre-existing (untouched)
| File | Contains |
|------|---------|
| `IFluencyInterfaces.cs` | Fluency assessment, audio/image storage |
| `IRagInterfaces.cs` | Embedding, vector store, vision description, RAG ingestion/query |
| `IAnnotationInterfaces.cs` | Annotation + vocabulary repositories |
| `IMessagingInterfaces.cs` | Messaging repository |
| `IPlacementRepository.cs` | Placement test repository |
| `IEducationalPdfService.cs` | Educational PDF library service |

---

## DTO Map (`Application/DTOs/`)

### New — split from `StoryDtos.cs`

#### `Stories/StoryDtos.cs` — namespace `Application.DTOs`
`GenerateStoryRequest`, `GenerateStoryResponse`, `StoryPageDto`, `UploadedStoryDto`,
`AiStoryOutput`, `AiStoryPage`, `JudgeResult`

#### `Booklets/BookletDtos.cs` — namespace `Application.DTOs`
`ImportBookResponse`, `LessonSummaryDto`, `LessonPageDto`, `LessonDetailResponse`,
`AdminBooksPageDto`, `ManualPageDto`, `CreateManualBookRequest`, `UpdatePageSentenceRequest`

#### `Assessments/ExamDtos.cs` — namespace `Application.DTOs`
`ExamResponse`, `QuestionDto`, `AiExamOutput`, `AiQuestion`, `AiMatchPair`,
`SubmitExamRequest`, `SubmitAnswer`, `ExamResultResponse`, `AnswerFeedback`

#### `Writing/WritingDtos.cs` — namespace `Application.DTOs`
`WritingCorrectionResponse`, `WritingMistakeDto`, `WritingAttemptHistoryDto`,
`ReadingAttemptHistoryDto`

#### `Progress/ProgressDtos.cs` — namespace `Application.DTOs`
`ProgressResponse`, `LessonProgressRequest`, `MarkPageRequest`,
`LessonPageProgressResponse`, `CurrentLessonResponse`

#### `Groups/GroupDtos.cs` — namespace `Application.DTOs`
`StudentGroupDto`, `StudentGroupMemberDto`, `CreateGroupRequest`, `AddGroupMemberRequest`,
`AssignLessonRequest`, `LessonAssignmentDto`, `LevelWordConfigDto`

### Pre-existing (untouched)
| File | Domain |
|------|--------|
| `AuthDTOs.cs` | Auth: register, login, token |
| `DashboardDTOs.cs` | Student, parent, teacher, school dashboards |
| `FluencyDtos.cs` | Fluency assessment, WCPM |
| `PlacementDTOs.cs` | Placement test |
| `LearningDTOs.cs` | Letters, words, sentences learning content |
| `AnnotationDtos.cs` | Vocabulary annotations |
| `MessageDtos.cs` | Teacher-student messaging |
| `RagDTOs.cs` | RAG query/context |
| `PdfLibraryDtos.cs` | Educational PDF library |

---

## Agent Map (`Application/Agents/`)

> Folder was renamed from `Application/Agent/` in Step B3.
> Namespace changed from `Application.Agent` → `Application.Agents`.

| File | Agent | Gemini Call | Prompt Source |
|------|-------|-------------|---------------|
| `StoryAgent.cs` | `StoryAgent` | `GenerateAsync` → story JSON | `AgentPrompts.StorySystemPrompt` / `StoryUserPrompt` |
| `ExamAgent.cs` | `ExamAgent` | `GenerateAsync` → exam JSON | `AgentPrompts.ExamSystemPrompt` / `ExamUserPrompt` |
| `ExamAgent.cs` | `ExamAgent` | `GenerateLessonAsync` → lesson exam JSON | `AgentPrompts.LessonExamSystemPrompt` / `LessonExamUserPrompt` |
| `WritingCorrectionAgent.cs` | `WritingCorrectionAgent` | OCR → similarity → feedback | `AgentPrompts.OcrCleanupSystemPrompt` + `IOcrService` + `ITextSimilarityService` |
| `LessonGenerationAgent.cs` | `LessonGenerationAgent` | Gemini + Cloudflare image | Internal prompt (not in AgentPrompts) |

> **Architecture note:** `WritingCorrectionAgent` and `LessonGenerationAgent` live in
> `Application/Agents/` but internally call `IHttpClientFactory` (an infrastructure
> concern). They were not moved during this refactor to minimize risk. Candidates for
> a future pass if strict Clean Architecture is required.

---

## Prompt Map (`Application/Prompts/`)

### `AgentPrompts.cs` — active, used by agents
| Constant / Method | Used By | Purpose |
|-------------------|---------|---------|
| `StorySystemPrompt` | `StoryAgent` | System instruction: write 3-page children's Arabic story, return JSON |
| `StoryUserPrompt(name, character, theme)` | `StoryAgent` | Per-request user message |
| `ExamSystemPrompt` | `ExamAgent.GenerateAsync` | 4-question exam from story sentences (MCQ, Matching, DragDrop, Ordering) |
| `ExamUserPrompt(sentences)` | `ExamAgent.GenerateAsync` | Per-request: inject story sentences |
| `LessonExamSystemPrompt` | `ExamAgent.GenerateLessonAsync` | Simpler 4-question exam for letter/word lessons |
| `LessonExamUserPrompt(sentences)` | `ExamAgent.GenerateLessonAsync` | Per-request: inject lesson sentences |
| `JudgeSystemPrompt` | `IJudgeService` impl | Content moderation: approve/reject story for child safety |
| `JudgeUserPrompt(title, sentences, prompts)` | `IJudgeService` impl | Per-request: inject story to review |
| `OcrCleanupSystemPrompt` | `IAiTextCleanupService` impl | Clean OCR-extracted Arabic text |
| `OcrCleanupUserPrompt(text)` | `IAiTextCleanupService` impl | Per-request: inject raw OCR text |

### `StoryPrompts.cs` — legacy, likely superseded
Contains `SystemPrompt` and `BuildUserPrompt` — an older version of the story generation
prompt targeting Qwen3. `AgentPrompts.StorySystemPrompt` is the active version used by
`StoryAgent`. `StoryPrompts.cs` is not referenced by any agent or controller.
**Candidate for deletion in a future cleanup pass.**

---

## Consumers Updated (Step B3)

| File | Change |
|------|--------|
| `storybuild.API/Program.cs` | `using Application.Agent` → `using Application.Agents`; fully-qualified reference updated |
| `Controllers/StoryController.cs` | `using Application.Agent` → `using Application.Agents` |
| `Controllers/ExamController.cs` | `using Application.Agent` → `using Application.Agents` |
| `Controllers/LessonsController.cs` | `using Application.Agent` → `using Application.Agents` |
| `Controllers/RagController.cs` | `using Application.Agent` → `using Application.Agents` |
| `Controllers/WritingController.cs` | `using Application.Agent` → `using Application.Agents` |
| `Infrastructure/InfrastructureExtensions.cs` | `using Application.Agent` → `using Application.Agents` |

---

## What Was NOT Changed

- Zero DB schema changes — no migrations
- Zero API route changes — all endpoints identical
- Zero consumer `using` changes for DTOs or Interfaces (namespace preservation technique)
- All pre-existing interface files left untouched
- `WritingCorrectionAgent` and `LessonGenerationAgent` internal logic untouched
- `StoryPrompts.cs` left in place (flagged as legacy, not deleted)

---

## Frontend Refactor (Complete)

All three frontend steps done. Angular build verified at 0 errors, 57 static routes after each step.

### F1 — Model Split (`story.models.ts` 589 lines → 11 domain files)

**Safety technique:** `story.models.ts` converted to a barrel re-export of all 11 new files.
All existing component imports of `'../models/story.models'` resolve identically — zero consumer changes.

| File | Types |
|------|-------|
| `story-content.models.ts` | `GenerateStoryRequest`, `StoryPage`, `StoryResponse`, `UploadedStoryDto` |
| `lesson.models.ts` | `LessonPage`, `LessonSummary`, `LessonDetail`, `ImportBookResponse`, `AdminBooksPageDto`, `ManualPageDto`, `CreateManualBookRequest` |
| `exam.models.ts` | `QuizType`, `MatchPair`, `QuestionDto`, `ExamResponse`, `SubmitAnswer`, `SubmitExamRequest`, `AnswerFeedback`, `ExamResult` |
| `writing.models.ts` | `WritingMistake`, `WritingCorrectionResponse`, `WritingAttemptHistory`, `ReadingAttemptHistory` |
| `progress.models.ts` | `ProgressResponse` |
| `rag.models.ts` | `KnowledgeDocumentDto`, `RagSearchResult`, `GenerateLessonRequest`, `IngestDocumentResponse`, `RagPageChunkDto`, `IngestEducationalPdfRequest`, `GenerateLessonV2Request` |
| `dashboard.models.ts` | `TopContentDto`, `ExamHistoryDto`, `RecentActivityDto`, `PerformanceBandDto`, `SkillBarDto`, `ClassroomStatsDto`, `LevelDistributionDto`, `LevelProgressDto`, `StudentSummaryDto`, `StudentDashboardDto`, `ParentDashboardDto`, `TeacherDashboardDto`, `SchoolDashboardDto` |
| `analytics.models.ts` | `SkillStat`, `LessonStat`, `WeaknessMap`, `WeakLetterDto`, `StudentAnalyticsDto`, `AnalyticsSummaryDto` |
| `groups.models.ts` | `StudentGroupMemberDto`, `StudentGroupDto`, `CreateGroupRequest`, `AddGroupMemberRequest` |
| `assignments.models.ts` | `AssignLessonRequest`, `LessonAssignmentDto`, `AssignmentDto`, `AssignmentSubmissionDto`, `TeacherAssignmentOverview` |
| `pdf-library.models.ts` | `PdfDocumentDto`, `PdfPageDto`, `PdfDocumentDetailDto`, `EmbedResultDto`, `PdfLibraryStatsDto` |

`models/index.ts` — barrel for new consumers going forward (excludes `learning.models.ts` to avoid name collision with its own `WritingCorrectionResponse`).

`learning.models.ts` — pre-existing, standalone, untouched.

### F2 — Service Split (`story.ts` 559 lines → 12 feature services)

**Safety technique:** `story.ts` converted to a facade `StoryService` that injects all 12 new services and delegates every method one-for-one. All existing component injections of `StoryService` continue to work — zero consumer changes.

| Service file | Domain | Methods |
|---|---|---|
| `story-content.service.ts` | Story generation + uploaded stories | generateStory, getStory, getAllStories, getMyStories, deleteStory, uploadStoryPdf, getUploadedStories, getUploadedStory, deleteUploadedStory |
| `exam.service.ts` | Exam generation + submission | generateExam, generateLessonExam, getOrGenerateExam, submitExam |
| `writing.service.ts` | Writing + reading history | submitLessonWriting, evaluateCanvasWriting, getWritingHistory, getReadingHistory |
| `lesson.service.ts` | Lesson CRUD + generation | getLessonsByLevel, getLesson, deleteLesson, createManualLesson, generateLesson, getMyLessons |
| `admin.service.ts` | Admin books + users + schools | importBook, importBookV2, getAllBooksAdmin, getBookDetailAdmin, deleteBook, publishLesson, unpublishLesson, publishStory, unpublishStory, updateBookPageSentence, createManualBook, getAiSettings, saveAiSettings, getSubscriptionStats, getAllUsers, blockUser, unblockUser, getSchools, createSchool |
| `progress.service.ts` | Progress tracking | getProgress, updateProgress, updateLessonProgress, markPageDone, getLessonPageProgress, getCurrentLesson, getWeaknessMap |
| `rag.service.ts` | RAG + knowledge docs + educational PDF | ingestDocument, getKnowledgeDocuments, deleteKnowledgeDocument, ragSearch, generateRagLesson, ingestEducationalPdf, getRagPageChunks, uploadKnowledgeDocument |
| `dashboard.service.ts` | Dashboards + classrooms + placement | getStudentDashboard, getParentDashboard, getTeacherDashboard, getSchoolDashboard, getKnownStudentNames, getLevelProgress, requestPlacementRetake, updateStudentLevel, getPlacementQuestions, submitPlacement, getSchoolClassrooms, createClassroom, addStudentToClassroom, getMyTeacherClassrooms, getClassroomDetail, editClassroom, deleteClassroom, removeStudentFromClassroom, searchSchoolStudents, getClassroomsReport |
| `pdf-library.service.ts` | PDF Library | uploadPdfDocument, generatePdfEmbeddings, getPdfDocuments, getPdfDocument, deletePdfDocument, getPdfLibraryStats |
| `groups.service.ts` | Student groups + lesson assignment | getTeacherGroups, createGroup, addGroupMember, removeGroupMember, deleteGroup, assignLesson, getAssignedLessons, getTeacherAssignments |
| `assignments.service.ts` | Assignment submissions | getStudentAssignments, submitAssignment, getAssignmentSubmissions, getTeacherAssignmentOverview |
| `analytics.service.ts` | Analytics + activity recording | getStudentWeakLetters, getClassAnalytics, recordActivity |

### F3 — Verification

- Angular build: **0 errors, 57 static routes prerendered** ✅
- Zero component files modified across F1 and F2
- All existing `StoryService` injections and `story.models` imports unaffected
