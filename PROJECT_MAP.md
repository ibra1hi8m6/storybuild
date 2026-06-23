# لغتي — Project Map
> Full codebase inventory for planning. Last updated: 2026-06-20.

---

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Backend — Domain Entities](#backend--domain-entities)
3. [Backend — Application Layer](#backend--application-layer)
4. [Backend — Infrastructure Layer](#backend--infrastructure-layer)
5. [Backend — API Controllers & Endpoints](#backend--api-controllers--endpoints)
6. [Frontend — Services](#frontend--services)
7. [Frontend — Components by Feature](#frontend--components-by-feature)
8. [Frontend — Models](#frontend--models)
9. [Frontend — Core & Shared](#frontend--core--shared)
10. [Frontend — Routes](#frontend--routes)
11. [Roles & Auth](#roles--auth)
12. [Test Accounts](#test-accounts)

---

## Architecture Overview

```
Backend  (.NET 10 Clean Architecture)
├── Domain/               Pure entities — no dependencies
├── Application/          Interfaces + DTOs + Agent orchestrators
├── Infrastructure/       EF Core, Gemini AI, Cloudinary, Email, RAG
└── storybuild.API/       ASP.NET Core controllers + Program.cs

Frontend (Angular 21 SSR, Standalone Components, Bootstrap 5 RTL)
├── features/             One folder per feature (lazy-loaded)
├── services/             HTTP services + global state
├── models/               TypeScript interfaces
├── shared/               Reusable UI components
└── core/                 Guards + interceptors
```

**Key tech choices:**
- State: Angular Signals (`signal()`, `computed()`)
- HTTP: single `StoryService` (~50 methods) + `AuthService`
- AI: Gemini 2.5 Flash (story/exam/judge/OCR/RAG)
- Storage: Cloudinary (images + audio)
- DB: SQL Server via EF Core
- Styles: Bootstrap 5 RTL (no SCSS, no inline CSS)
- TTS: `window.speechSynthesis` (free, frontend-only, Arabic voices)

---

## Backend — Domain Entities

Path: `Backend/trystorybuild/Domain/Entities/`

| File | Entity | Key Fields |
|------|--------|-----------|
| `User.cs` | `User` | Id, Name, Email, PasswordHash, Role (enum 0–4), IsActive, IsBlocked |
| `Parent.cs` | `Parent` | Id (= UserId), User, Children: List\<Student\> |
| `Teacher.cs` | `Teacher` | Id (= UserId), User, SchoolCode, IsPrivate, Students: List\<Student\> |
| `Student.cs` | `Student` | Id, Name, Age, Username, ImagePin1, ImagePin2, Level, PlacementDone, WeaknessMapJson, ParentId, TeacherId, AvatarUrl |
| `Story.cs` | `Story` | Id, ChildName, Character, Theme, Title, IsApproved, Source (AiGenerated\|UploadedPdf), Pages |
| `StoryPage.cs` | `StoryPage` | Id, StoryId, PageNumber, Sentence, ImagePath, ImagePrompt, IsUnlocked |
| `Lesson.cs` | `Lesson` | Id, Title, Level (1–3), Letter, LetterName, CreatorId, CreatorRole, IsGenerated, Pages |
| `LessonPage.cs` | `LessonPage` | Id, LessonId, PageNumber, Sentence, ImagePath, IsCoverPage, IsUnlocked |
| `Exam.cs` | `Exam` | Id, StoryId?, LessonId?, Questions |
| `Question.cs` | `Question` | Id, ExamId, QuestionNumber, Type (MCQ\|Matching\|DragDrop\|Ordering), Text, OptionA–D, CorrectAnswer, DataJson |
| `StudentAnswer.cs` | `StudentAnswer` | Id, QuestionId, ChildName, ChosenAnswer, IsCorrect, AnsweredAt |
| `StudentProgress.cs` | `StudentProgress` | Id, StoryId?, LessonId?, ChildName, CurrentPage, TotalQuestions, CorrectAnswers, ScorePercentage, ExamCompleted, LastUpdatedAt |
| `WritingAttempt.cs` | `WritingAttempt` | Id, LessonPageId, ChildName, SubmittedImagePath, OcrText, SimilarityScore, Feedback |
| `PlacementQuestion.cs` | `PlacementQuestion` | Id, Part (1\|2\|3), Order, QuestionText, OptionsJson, CorrectAnswer, ImageContent, AudioText |
| `StudentGroup.cs` | `StudentGroup` | Id, Name, TeacherId, Members |
| `StudentGroupMember.cs` | `StudentGroupMember` | GroupId, StudentId, AddedAt |
| `LessonAssignment.cs` | `LessonAssignment` | Id, LessonId, TeacherId, TargetType (Student\|Group), TargetStudentId?, TargetGroupId?, AssignedAt |
| `KnowledgeDocument.cs` | `KnowledgeDocument` | Id, FileName, DocumentType (PDF\|PPTX\|Image), Letter, Level, Tags, ChunkCount, IngestedAt |
| `RagPageChunk.cs` | `RagPageChunk` | Id, SourceFile, PageNumber, Sentence, Level, Letter, LetterName, EmbeddingJson |
| `LevelWordConfig.cs` | `LevelWordConfig` | Id, Level, WordCount, ExampleSentence |
| `PdfDocument.cs` | `PdfDocument` | Id, FileName, Letter, Level, PageCount, Pages |
| `PdfPage.cs` | `PdfPage` | Id, PdfDocumentId, PageNumber, TextContent, ImagePath |
| `Classroom.cs` | `Classroom` | Id, Name, Level, SchoolCode, TeacherId, Students |
| `ClassroomStudent.cs` | `ClassroomStudent` | ClassroomId, StudentId |
| `Message.cs` | `Message` | Id, SenderId, ReceiverId, Content, Type (Text\|Voice), IsRead |
| `Annotation.cs` | `Annotation` | Id, StudentId, PageId, PageType, Type (Highlight\|Underline\|CircleWord), ColorHex, SelectedText, StartOffset, EndOffset |
| `AudioRecording.cs` | `AudioRecording` | Id, StudentId, PageId, PageType, AudioFileUrl, DurationSeconds, Report |
| `FluencyReport.cs` | `FluencyReport` | Id, AudioRecordingId, WCPM, AccuracyScore, ExpectedText, ExtractedText, MispronouncedWordsJson |
| `LessonVocabulary.cs` | `LessonVocabulary` | Id, LessonId, Word, Definition, ImageUrl, Order |
| `WordJournalEntry.cs` | `WordJournalEntry` | Id, StudentId, Word, Definition, ImageUrl |

**Enums:**
- `UserRole`: Student=0, Parent=1, Teacher=2, SchoolAdmin=3, SystemAdmin=4
- `QuizType`: MCQ=0, Matching=1, DragDrop=2, Ordering=3
- `StorySource`: AiGenerated=0, UploadedPdf=1
- `StudentLoginMethod`: ImagePin=0, TextPassword=1

---

## Backend — Application Layer

### AI Agent Orchestrators
Path: `Backend/trystorybuild/Application/Agent/`

| File | Class | Key Method |
|------|-------|-----------|
| `StoryAgent.cs` | `StoryAgent` | `RunAsync(GenerateStoryRequest) → GenerateStoryResponse` — generates story → judge → images |
| `ExamAgent.cs` | `ExamAgent` | `GenerateAsync(storyId)`, `GenerateFromLessonAsync(lessonId)`, `SubmitAsync(SubmitExamRequest)` |
| `LessonGenerationAgent.cs` | `LessonGenerationAgent` | `GenerateAsync(GenerateLessonRequest, ct) → LessonDetailResponse` (RAG-augmented) |
| `WritingCorrectionAgent.cs` | `WritingCorrectionAgent` | `EvaluateAsync(lessonPageId, lessonId, childName, image)`, `EvaluateDirectAsync(base64, expectedText)` |
| `GeminiFluencyAssessorAgent.cs` | fluency agent | Gemini multimodal speech fluency evaluation |

### Key Interfaces
Path: `Backend/trystorybuild/Application/Interfaces/IServices.cs`

```
IAuthService           — RegisterAsync, LoginAsync, CreateStudentAsync, StudentLoginAsync, GetChildrenAsync, GetStudentsAsync
IStoryGeneratorService — GenerateAsync(childName, character, theme)
IExamGeneratorService  — GenerateAsync(storyText), GenerateLessonAsync(lessonText)
IJudgeService          — ValidateAsync(title, sentences, imagePrompts)
IImageGenerationService— GenerateImageAsync(prompt, fileName)
IOcrService            — ExtractArabicTextAsync(imagePath)
ITextSimilarityService — Calculate(expected, actual) → double
IDashboardService      — GetStudentDashboard, GetParentDashboard, GetTeacherDashboard, GetSchoolDashboard, GetLevelProgress
IEmailService          — SendTeacherWelcomeAsync, SendTeacherPasswordResetAsync
IPdfImportService      — ImportBookAsync(level, letter, letterName, title, pdf, ct)
IUploadedStoryService  — Upload/delete/list uploaded PDF stories
IRagIngestionService   — IngestAsync(stream, filename, request, ct)
IRagQueryService       — SearchAsync(query, topK)
IEducationalPdfService — UploadAsync, GenerateEmbeddingsAsync, GetAll, GetDetail, Delete, GetStats
IFluencyAssessorAgent  — Gemini-powered reading fluency evaluation
IAnnotationRepository  — Save/Get/Delete text annotations
IAudioRecordingRepository — Save/Get audio recordings + fluency reports
IMessagingRepository   — Send/Inbox/MarkRead messages
IVocabularyRepository  — Lesson vocabulary + student word journal
```

### DTOs
Path: `Backend/trystorybuild/Application/DTOs/`

| File | Key Records |
|------|-------------|
| `AuthDTOs.cs` | `RegisterRequest`, `LoginRequest`, `StudentLoginRequest`, `CreateStudentRequest`, `AuthResponse`, `StudentAuthResponse`, `UpdateLevelRequest`, `ResetPasswordRequest` |
| `StoryDtos.cs` | `GenerateStoryRequest`, `StoryPageDto`, `GenerateStoryResponse` |
| `PlacementDTOs.cs` | `PlacementQuestionDto`, `PlacementSubmitRequest`, `PlacementResultDto` |
| `DashboardDTOs.cs` | `StudentDashboardDto`, `ParentDashboardDto`, `TeacherDashboardDto`, `SchoolDashboardDto`, `LevelProgressDto` |
| `FluencyDtos.cs` | `FluencyReportDto`, `FluencyHistoryDto` |
| `AnnotationDtos.cs` | `SaveAnnotationRequest`, `AnnotationDto` |
| `MessageDtos.cs` | `SendMessageRequest`, `MessageDto` |
| `RagDTOs.cs` | `IngestDocumentRequest`, `IngestDocumentResponse`, `KnowledgeDocumentDto`, `RagSearchResult`, `RagPageChunkDto` |
| `PdfLibraryDtos.cs` | `PdfDocumentDto`, `PdfDocumentDetailDto`, `PdfLibraryStatsDto`, `EmbedResultDto` |

---

## Backend — Infrastructure Layer

### Authentication
`Infrastructure/Auth/AuthService.cs`
- `RegisterAsync` → creates User + Parent/Teacher/SchoolAdmin rows, returns JWT
- `LoginAsync` → BCrypt verify, returns JWT
- `CreateStudentAsync` → creates Student linked to parent/teacher
- `StudentLoginAsync` → image PIN auth, returns JWT with role=Student
- `GetChildrenAsync(parentId)` / `GetStudentsAsync(teacherId)`
- `UpdateStudentLevelAsync(studentId, level)`
- JWT: 30-day expiry, `sub` = UserId or StudentId, claims include `role`, `level`

### AI Services
`Infrastructure/AI/`

| File | Purpose |
|------|---------|
| `GeminiStoryGeneratorService.cs` | Gemini 2.5 Flash story generation |
| `GeminiExamGeneratorService.cs` | MCQ/Matching/DragDrop/Ordering exam generation |
| `GeminiJudgeService.cs` | Content safety validation |
| `GeminiOcrService.cs` | Arabic text extraction from images |
| `GeminiTextCleanupService.cs` | OCR output cleanup |
| `GeminiFluencyAssessorAgent.cs` | Gemini multimodal fluency assessment |
| `ArabicSimilarityService.cs` | Arabic text similarity (writing correction) |
| `CloudinaryImageService.cs` | AI image generation → Cloudinary |
| `CloudinaryImageStorageService.cs` | Direct image upload to Cloudinary |
| `CloudinaryAudioStorageService.cs` | Audio recording upload to Cloudinary |
| `GeminiEmbeddingService.cs` | Text embeddings for RAG |
| `GeminiVisionDescriptionService.cs` | Page image → text description for RAG |

### RAG Pipeline
`Infrastructure/Rag/`

| File | Purpose |
|------|---------|
| `RagIngestionService.cs` | Orchestrate ingest: extract text → chunk → embed → store in Chroma |
| `RagQueryService.cs` | Embed query → search Chroma → return chunks |
| `ChromaVectorStoreService.cs` | Chroma Cloud HTTP client |
| `ArabicTextChunker.cs` | Sentence-aware chunking for Arabic text |
| `PdfDocumentProcessor.cs` | PDF → text pages |
| `PptxDocumentProcessor.cs` | PowerPoint → text slides |
| `ImageDocumentProcessor.cs` | Image → text via Gemini Vision |
| `EducationalPdfIngestionService.cs` | Per-page ingestion with vision descriptions |

### PDF / Lesson Pipeline
`Infrastructure/Pdf/`

| File | Purpose |
|------|---------|
| `PdfImportService.cs` | PDF → Lesson entity (renders pages, generates images) |
| `UploadedStoryImportService.cs` | PDF → UploadedStory entity |
| `EducationalPdfService.cs` | PDF library CRUD + Chroma embedding |
| `PdfPageRenderer.cs` | PDF page → PNG image (Docnet) |

### Services
`Infrastructure/Services/`

| File | Purpose |
|------|---------|
| `DashboardService.cs` | Aggregates stats for all four dashboards |
| `EmailService.cs` | SMTP via Gmail (teacher welcome + password reset) |

### Data Layer
`Infrastructure/Data/`

| File | Purpose |
|------|---------|
| `AppDbContext.cs` | EF Core context with all DbSet<> properties |
| `DbSeeder.cs` | Seeds SystemAdmin, SchoolAdmin, Teacher, Parent test accounts |
| `EntityConfigurations.cs` | Fluent API: keys, FK cascades, indexes |

---

## Backend — API Controllers & Endpoints

### AuthController — `/api/auth`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/register` | — | Adult (parent/teacher/schoolAdmin) registration. Sends welcome email if role=teacher |
| POST | `/login` | — | Adult login → JWT |
| POST | `/students` | Parent, Teacher | Create student account |
| POST | `/students/login` | — | Student image-PIN login → JWT |
| GET | `/students` | Parent, Teacher | List own children/students |
| GET | `/me` | Any | Current user profile |
| PATCH | `/students/{id}/level` | Teacher, SystemAdmin | Update student level |
| PATCH | `/students/me/level` | Student | Student updates own level after placement |
| POST | `/school/teachers/{id}/reset-password` | SchoolAdmin | Reset teacher password + email |
| GET | `/school/teachers` | SchoolAdmin | List teachers in this school |

### StoryController — `/api/story`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/generate` | — | Generate AI story (3 pages) |
| GET | `/{id}` | — | Get story by ID |
| GET | `` | — | List all AI stories |
| GET | `/mine/{childName}` | — | Stories for specific child |
| DELETE | `/{id}` | — | Delete story |
| GET | `/uploaded` | — | List admin-uploaded PDF stories |
| GET | `/uploaded/{id}` | — | Get uploaded story detail |

### LessonsController — `/api/lessons`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/?level=N` | — | Lessons for a level |
| GET | `/{id}` | — | Lesson detail with pages |
| POST | `/generate` | — | AI-generate lesson from RAG |
| POST | `/manual` | — | Teacher creates lesson manually |
| DELETE | `/{id}` | — | Delete lesson |
| GET | `/my/{creatorId}` | — | Lessons created by a user |

### ExamController — `/api/exam`

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/generate/{storyId}` | Generate MCQ exam from story |
| POST | `/generate/lesson/{lessonId}` | Generate exam from lesson |
| GET | `/story/{storyId}` | Get existing exam for story |
| POST | `/submit` | Submit answers → score → save progress |

### WritingController — `/api/writing`

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/evaluate` | Submit writing image from lesson page → OCR → score → feedback |
| POST | `/canvas` | Submit canvas base64 image → OCR → score (standalone) |

### ProgressController — `/api/progress`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/{storyId}/{childName}` | Get story reading progress |
| PUT | `` | Update story reading progress |
| PUT | `/lesson` | Update lesson exam progress + update WeaknessMapJson |
| GET | `/weakness/{childName}` | Get per-letter weakness map |

### PlacementController — `/api/placement`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/questions` | — | Get 15 placement questions (3 parts × 5) |
| POST | `/submit` | — | Score placement answers → determine level (1/2/3) |
| POST | `/retake` | Student | Request retake (only if all lessons in current level done) |
| GET | `/level-completion/{childName}` | — | Returns completed/total lessons + isLevelComplete |

### DashboardController — `/api/dashboard`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/student/{childName}` | — | Student dashboard stats |
| GET | `/parent/{childName}` | — | Parent dashboard for a child |
| GET | `/teacher` | Teacher | Teacher class stats |
| GET | `/school` | — | School-wide stats |
| GET | `/students` | — | Known child names |
| GET | `/levels/progress/{childName}` | — | Per-level progress (locked/unlocked, % complete) |

### ClassroomsController — `/api/classrooms` [Authorize]

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `` | SchoolAdmin | List school's classrooms |
| GET | `/{id}` | SchoolAdmin, Teacher | Classroom detail + students |
| POST | `` | SchoolAdmin | Create classroom |
| PUT | `/{id}` | SchoolAdmin | Edit classroom |
| DELETE | `/{id}` | SchoolAdmin | Delete classroom |
| POST | `/{id}/students` | SchoolAdmin, Teacher | Add student to classroom |
| DELETE | `/{id}/students/{studentId}` | SchoolAdmin, Teacher | Remove student |
| GET | `/school-students?q=` | SchoolAdmin | Search students to add |
| GET | `/report` | SchoolAdmin | Per-classroom performance report |
| GET | `/my` | Teacher | Teacher's own classrooms |

### GroupsController — `/api/groups`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/teacher/{teacherId}` | — | Teacher's student groups |
| POST | `/teacher/{teacherId}` | — | Create group |
| POST | `/{groupId}/members` | — | Add member to group |
| DELETE | `/{groupId}/members/{studentId}` | — | Remove member |
| DELETE | `/{groupId}` | — | Delete group |
| POST | `/assign` | Teacher, SystemAdmin | Assign lesson to student or group |
| GET | `/assignments/teacher/{teacherId}` | — | Teacher's lesson assignments |
| GET | `/assigned/student/{studentId}` | — | Student's assigned lessons |

### AdminController — `/api/admin`

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/import-book` | Import PDF → Lesson (with OCR + Cloudinary) |
| GET | `/books?level=&page=&pageSize=` | Paginated lesson list |
| GET | `/books/{id}` | Lesson detail |
| DELETE | `/books/{id}` | Delete lesson |
| PATCH | `/books/{bookId}/pages/{pageId}/sentence` | Edit page sentence |
| POST | `/books/manual` | Create lesson manually |
| GET | `/ai-settings` | Get AI config (model names) |
| PUT | `/ai-settings` | Save AI config |
| GET | `/subscriptions/stats` | Subscription analytics |
| GET | `/users` | All users |
| POST | `/users/{id}/block` | Block user |
| POST | `/users/{id}/unblock` | Unblock user |
| GET | `/schools` | List school admins |
| POST | `/schools` | Create school admin account |
| POST | `/uploaded-stories` | Upload admin PDF story |
| GET | `/uploaded-stories` | List uploaded stories |
| DELETE | `/uploaded-stories/{id}` | Delete uploaded story |

### Other Controllers

| Controller | Route | Key Endpoints |
|------------|-------|---------------|
| `RagController` | `/api/rag` | POST `/ingest`, POST `/ingest-educational`, GET `/page-chunks`, GET `/documents`, DELETE `/documents/{id}`, POST `/search` |
| `PdfLibraryController` | `/api/pdf-library` | POST `/upload`, POST `/{id}/embed`, GET ``, GET `/{id}`, DELETE `/{id}`, GET `/stats` |
| `FluencyController` | `/api/fluency` | POST `/evaluate`, GET `/student/{id}`, GET `/page/{pageId}/student/{studentId}` |
| `AnnotationsController` | `/api/annotations` | POST ``, GET `/{studentId}/{pageId}`, DELETE `/{id}/{studentId}` |
| `VocabularyController` | `/api/vocabulary` | GET/POST/DELETE for lesson vocab + student journal |
| `MessagesController` | `/api/messages` | POST `/send`, POST `/send-voice`, GET `/inbox/{userId}`, GET `/unread-count/{userId}`, POST `/{id}/mark-read/{userId}` |
| `ParentPortalController` | `/api/parent-portal` | GET `/child/{studentId}/recordings` |

---

## Frontend — Services

### `services/auth.service.ts`

**Signals (readonly):**
- `currentUser` — logged-in user object
- `token` — JWT string
- `isLoggedIn`, `userRole`, `isStudent`, `isParent`, `isTeacher`, `isSchoolAdmin`, `isAdmin`

**Methods:**
```ts
register(body)                          → Observable<AuthResponse>
login(email, password)                  → Observable<AuthResponse>
studentLogin(username, pin1, pin2)      → Observable<StudentAuthResponse>
createStudent(req)                      → Observable<StudentAuthResponse>
getMyStudents()                         → Observable<StudentSummary[]>
updateChildLevel(studentId, level)      → Observable<StudentAuthResponse>
getSchoolTeachers()                     → Observable<Teacher[]>
resetTeacherPassword(teacherId, pw)     → Observable<{ message }>
logout()                                → void
```

---

### `services/story.ts` — StoryService (main HTTP service)

**Story:**
```ts
generateStory(req)          getStory(id)            getAllStories()
getMyStories(childName)     deleteStory(id)
getUploadedStories()        getUploadedStory(id)    uploadStoryPdf(title, file)
deleteUploadedStory(id)
```

**Lessons:**
```ts
getLessonsByLevel(level)    getLesson(id)           deleteLesson(id)
createManualLesson(req)     generateLesson(req)     getMyLessons(creatorId)
```

**Exams:**
```ts
generateExam(storyId)       generateLessonExam(lessonId)
getOrGenerateExam(storyId)  submitExam(req)
```

**Writing:**
```ts
submitLessonWriting(lessonId, pageId, childName, blob)
evaluateCanvasWriting(base64, expectedText)
```

**Progress:**
```ts
getProgress(storyId, childName)     updateProgress(progress)
updateLessonProgress(req)           getWeaknessMap(childName)
```

**Dashboard:**
```ts
getStudentDashboard(childName)      getParentDashboard(childName)
getTeacherDashboard()               getSchoolDashboard()
getKnownStudentNames()              getLevelProgress(childName)
```

**Placement:**
```ts
getPlacementQuestions()     submitPlacement(request)
requestPlacementRetake()    updateStudentLevel(level)
```

**Classrooms:**
```ts
getSchoolClassrooms()           createClassroom(body)
getMyTeacherClassrooms()        getClassroomDetail(id)
editClassroom(id, body)         deleteClassroom(id)
addStudentToClassroom(cId, sId) removeStudentFromClassroom(cId, sId)
searchSchoolStudents(q)         getClassroomsReport()
```

**Groups & Assignments:**
```ts
getTeacherGroups(teacherId)     createGroup(teacherId, name)
addGroupMember(groupId, sId)    removeGroupMember(groupId, sId)
deleteGroup(groupId)
assignLesson(req)               getAssignedLessons(studentId)
getTeacherAssignments(teacherId)
```

**Admin:**
```ts
importBook(level, letter, letterName, file)
getAllBooksAdmin(level?, page, pageSize)     getBookDetailAdmin(id)
deleteBook(id)                              updateBookPageSentence(bookId, pageId, sentence)
createManualBook(req)
getAiSettings()     saveAiSettings(settings)
getSubscriptionStats()
getAllUsers()        blockUser(id)       unblockUser(id)
getSchools()        createSchool(body)
```

**RAG / PDF Library:**
```ts
ingestDocument(file, letter?, level?, tags?)
getKnowledgeDocuments()     deleteKnowledgeDocument(id)     ragSearch(query)
uploadPdfDocument(file, letter, level)      generatePdfEmbeddings(id)
getPdfDocuments()           getPdfDocument(id)              deletePdfDocument(id)
getPdfLibraryStats()
```

**Vocabulary & Fluency:**
```ts
getLessonVocabulary(lessonId)   addLessonVocabulary(req)    deleteLessonVocabulary(id)
getStudentJournal(studentId)    addToJournal(req)           removeFromJournal(id, studentId)
evaluateFluency(params)         getStudentFluencyHistory(studentId)
```

**Messages:**
```ts
sendMessage(req)            sendVoiceMessage(senderId, receiverId, file)
getInbox(userId)            getUnreadCount(userId)          markRead(messageId, userId)
```

---

### `services/app-state-service.ts` — AppStateService (global signals)

```ts
// Signals
currentUser     childName       currentStory    currentLesson
currentExamResult   currentPage     lessonStarted

// Computed
isLoggedIn  userRole  isRtl  activePage  totalPages  isLastPage  progressPercent

// Methods
setUser(user)           logout()                updateStudentLevel(level, token?)
setChildName(name)      setStory(story)         setLesson(lesson)
goToPage(n)             nextPage()              startLesson()
setExamResult(result)   reset()
lessonProgress(lessonId)                saveLessonProgress(id, page, total, done)
```

---

## Frontend — Components by Feature

### Auth
| Component | Route | Description |
|-----------|-------|-------------|
| `login.component.ts` | `/auth/login` | Email+password adult login or student PIN login |
| `register.component.ts` | `/auth/register` | Adult registration |
| `create-student.component.ts` | `/auth/create-student` | Parent/teacher creates student |

### Placement Test
| Component | Route | Description |
|-----------|-------|-------------|
| `placement-welcome.component.ts` | `/placement` | Welcome screen |
| `placement-question.component.ts` | `/placement/test` | Display question with emoji options, auto-play audio |
| `placement-result.component.ts` | `/placement/result` | Level assignment result |

### Dashboards
| Component | Route | Signals | Key Methods |
|-----------|-------|---------|-------------|
| `student-dashboard.component.ts` | `/dashboard` | `data`, `isLoading`, `weekActivity`, `achievements` | `load(name)`, `openBook(id)`, `openLesson(id)` |
| `parent-dashboard.component.ts` | `/parent` | `childNames`, `activeChild`, `data`, `weaknessMap` | `selectChild(name)`, `weakLetters()`, `weakLessons()` |
| `teacher-dashboard.component.ts` | `/teacher` | teacher stats signals | classroom + student stats |
| `school-dashboard.component.ts` | `/school/dashboard` | school stats signals | school-wide analytics |

### Levels & Lessons
| Component | Route | Description |
|-----------|-------|-------------|
| `levels.component.ts` | `/levels` | Level selection cards (locked/unlocked), placement retake banner |
| `books.component.ts` | `/levels/:level/books` | Lesson grid for a level |
| `lesson-reader.ts` | `/lessons/:id/read` | Read lesson pages with TTS auto-play, writing practice, canvas |
| `lesson-list.ts` | `/lessons` | All lessons list |
| `my-lessons.ts` | `/my-lessons` | Student's AI-generated lessons |
| `assigned-lessons.ts` | `/assigned-lessons` | Teacher-assigned lessons for student |

### Stories
| Component | Route | Description |
|-----------|-------|-------------|
| `story-reader.ts` | `/books/:id/read` | AI story reader with page navigation + fluency |
| `story-generator.ts` | `/ai-story` | Generate AI story (character, theme) |
| `uploaded-stories.component.ts` | `/uploaded-stories` | Browse uploaded PDF stories |
| `uploaded-story-journey.component.ts` | `/uploaded-stories/:id/read` | Read uploaded story → record → exam |
| `my-stories.component.ts` | `/my-stories` | Student's generated stories |

### Exams & Quizzes
| Component | Route | Description |
|-----------|-------|-------------|
| `exam.ts` | `/exam` | MCQ/Matching/DragDrop/Ordering quiz. Handles `?lessonId=` and `?storyId=` params |
| `quiz.component.ts` | `/quiz` | Alternative quiz layout |
| `quiz-result.component.ts` | `/quiz-result` | Score display after exam |

### Fluency Assessment
| Component | Route | Description |
|-----------|-------|-------------|
| `reading-journey-host.component.ts` | embedded | Host for listen/record modes |
| `listen-mode.component.ts` | embedded | TTS playback of page content |
| `record-mode.component.ts` | embedded | Record student, submit to Gemini, show WCPM/accuracy |

### Teacher Features
| Component | Route | Description |
|-----------|-------|-------------|
| `teacher-lessons.component.ts` | `/teacher/lessons` | List + manage lessons |
| `lesson-create.component.ts` | `/teacher/lessons/create` | AI-generate or manual lesson builder |
| `teacher-groups.ts` | `/teacher/groups` | Student groups management |
| `teacher-reports.component.ts` | `/teacher/reports` | Class performance reports |
| `student-detail.component.ts` | `/teacher/students/:id` | Individual student detail |
| `ai-generator.component.ts` | `/teacher/ai-generate` | RAG-based lesson generation UI |

### School Admin Features
| Component | Route | Description |
|-----------|-------|-------------|
| `school-dashboard.component.ts` | `/school/dashboard` | School analytics |
| `school-teachers.component.ts` | `/school/teachers` | Manage teachers, create accounts, reset passwords |
| `school-classrooms.component.ts` | `/school/classrooms` | Manage classrooms + student enrollment |
| `school-reports.component.ts` | `/school/reports` | Per-classroom performance |
| `school-subscription.component.ts` | `/school/subscription` | Subscription status |

### Parent Features
| Component | Route | Description |
|-----------|-------|-------------|
| `parent-dashboard.component.ts` | `/parent` | Child tabs + stats + weakness map |
| `parent-children.component.ts` | `/parent/children` | List & manage children accounts |
| `child-progress.component.ts` | `/parent/child/:id/progress` | Detailed child progress |
| `parent-notifications.component.ts` | `/parent/notifications` | Activity notifications |
| `parent-recordings.component.ts` | `/parent/recordings` | Child's fluency recordings |

### System Admin Features
| Component | Route | Description |
|-----------|-------|-------------|
| `admin-users.component.ts` | `/admin/users` | User list, block/unblock |
| `admin-schools.component.ts` | `/admin/schools` | Create school admin accounts |
| `admin-content.component.ts` | `/admin/content` | Manage stories + lessons |
| `admin-stories.component.ts` | `/admin/stories` | Story management |
| `admin-import.ts` | `/admin/import` | PDF → Lesson import |
| `admin-rag.component.ts` | `/admin/rag` | RAG document management |
| `admin-rag-chunks.ts` | `/admin/rag-chunks` | View RAG chunks |
| `admin-pdf-library.component.ts` | `/admin/pdf-library` | PDF library (upload, embed, delete) |
| `admin-uploaded-stories.component.ts` | `/admin/uploaded-stories` | Manage PDF stories |
| `ai-settings.component.ts` | `/admin/ai-settings` | Gemini model configuration |
| `subscriptions.component.ts` | `/admin/subscriptions` | Subscription stats |

### Educational Tools (eTools)
| Component | Description |
|-----------|-------------|
| `text-highlighter.directive.ts` | Enable text selection → highlight/underline/circle |
| `vocabulary-popup.component.ts` | Show word definition + add to journal |
| `word-journal.component.ts` | Student personal word journal |
| `lesson-reader-toolbar.component.ts` | Annotation + vocab toolbar in reader |

### Mini-Games
| Component | Route | Description |
|-----------|-------|-------------|
| `mini-games-host.component.ts` | `/mini-games` | Game selection |
| `matching-game.component.ts` | embedded | Match Arabic words to images |
| `missing-letter.component.ts` | embedded | Fill the missing letter |
| `ordering-game.component.ts` | embedded | Reorder words to form sentence |

### Messaging
| Component | Route | Description |
|-----------|-------|-------------|
| `student-inbox.component.ts` | `/inbox` | Messages from teacher |
| `teacher-feedback.component.ts` | `/teacher/feedback` | Send feedback to students |

### Other
| Component | Route | Description |
|-----------|-------|-------------|
| `home.component.ts` | `/` | Landing page |
| `levels.component.ts` | `/levels` | Level selection (with placement retake banner) |
| `progress.component.ts` | `/progress` | Student progress overview |
| `achievements.component.ts` | `/achievements` | Badges and achievements |
| `settings.component.ts` | `/settings` | User settings |
| `upgrade.component.ts` | `/upgrade` | Upgrade plan page |
| `not-found.component.ts` | `**` | 404 page |

---

## Frontend — Models

Path: `Frontend/story-app/src/app/models/story.models.ts`

```ts
// Weakness map (per-letter learning tracking)
interface SkillStat  { attempts: number; correct: number; }
interface LessonStat { title: string; letter: string; attempts: number; correct: number; }
interface WeaknessMap { letters: Record<string, SkillStat>; lessons: Record<string, LessonStat>; }

// Story
interface StoryPage  { pageId, pageNumber, sentence, imageUrl, isUnlocked }
interface StoryResponse { id, title, isApproved, pages: StoryPage[] }
interface UploadedStoryDto { id, title, source, pages: StoryPage[] }

// Lesson
interface LessonPage    { pageId, pageNumber, sentence, imageUrl, isCoverPage, isUnlocked }
interface LessonSummary { id, level, letter, letterName, title, coverImageUrl, pageCount }
interface LessonDetail  { id, level, letter, letterName, title, coverImageUrl, pages: LessonPage[] }

// Exam
type QuizType = 0 | 1 | 2 | 3;  // MCQ | Matching | DragDrop | Ordering
interface QuestionDto  { questionId, questionNumber, type: QuizType, text, optionA-D, dataJson }
interface ExamResponse { examId, storyId, questions: QuestionDto[] }
interface ExamResult   { totalQuestions, correctAnswers, scorePercentage, feedback: AnswerFeedback[] }
interface AnswerFeedback { questionId, isCorrect, yourAnswer, correctAnswer }

// Writing
interface WritingCorrectionResponse { extractedText, expectedSentence, similarityScore, isAccepted, message }

// Placement
interface PlacementQuestionDto { id, part, order, questionText, imageContent, options: PlacementOptionDto[], audioText }
interface PlacementOptionDto   { key, emoji, label }

// Progress
interface ProgressResponse { storyId, childName, currentPage, totalQuestions, correctAnswers, scorePercentage, examCompleted }
interface LessonProgressRequest { lessonId, childName, totalQuestions, correctAnswers, scorePercentage, examCompleted }

// Dashboard
interface LevelProgressDto { level, title, subtitle, icon, tag, locked, stars, totalStars, lessonsCompleted, totalLessons, avgScore, unlockCondition }
interface StudentDashboardDto { childName, level, storiesRead, lessonsCompleted, examsCompleted, avgScore, currentStreak, stars, weeklyActivity: number[] }
interface ParentDashboardDto { childName, level, storiesRead, lessonsCompleted, avgScore, weeklyActivity, recentAssignments, skillBars, inProgressLessons }

// Fluency
interface FluencyReportDto { reportId, recordingId, audioFileUrl, wcpm, accuracyScore, expectedText, extractedText, mispronouncedWords, passed, createdAt }

// Groups & Assignments
interface StudentGroupDto      { id, name, teacherId, members }
interface LessonAssignmentDto  { id, lessonId, lessonTitle, teacherId, targetType, targetStudentId, targetGroupId, assignedAt }

// PDF Library
interface PdfDocumentDto       { id, fileName, letter, level, pageCount, createdAt }
interface PdfDocumentDetailDto { ...PdfDocumentDto, pages: PdfPageDto[] }
interface PdfPageDto           { id, pageNumber, sentence, imageUrl, isEmbedded }

// Admin
interface AdminBooksPageDto    { items, total, page, pageSize, totalPages }
interface ImportBookResponse   { lessonId, title, level, letter, letterName, pageCount }
interface KnowledgeDocumentDto { id, fileName, documentType, letter, level, tags, chunkCount, ingestedAt }
interface RagPageChunkDto      { id, sourceFile, pageNumber, sentence, letter, level }
```

---

## Frontend — Core & Shared

### Guards (`core/`)
- `auth.guard.ts` — Redirects to `/auth/login` if not logged in
- Role-based guards: `studentGuard`, `parentGuard`, `teacherGuard`, `schoolAdminGuard`, `adminGuard`

### Interceptors (`core/`)
- `auth.interceptor.ts` — Adds `Authorization: Bearer <token>` to all requests
- `error-interceptor.ts` — Shows error toast on HTTP errors

### Shared Components (`shared/`)
- `navbar.component.ts` — Top bar with user menu, role-based nav links, logout
- `error-toast.ts` — HTTP error notification
- `loading.ts` — Full-screen spinner
- `simple-loading.ts` — Inline spinner

---

## Frontend — Routes

Key routes from `app.routes.ts`:

```
/                           Home (landing page)
/auth/login                 Login (adult or student)
/auth/register              Register (adult)
/auth/create-student        Create student account

/placement                  Placement test welcome
/placement/test             Placement questions
/placement/result           Placement result + level set

/dashboard                  Student dashboard
/levels                     Level selection (+ retake banner)
/levels/:level/books        Books for a level
/books/:id/read             Read a story (AI)
/lessons/:id/read  →        (uses /levels/:level/books → lesson-reader)
/exam                       Exam (?lessonId= or ?storyId=)
/quiz-result                Quiz result

/my-stories                 Student's stories
/my-lessons                 Student's generated lessons
/assigned-lessons           Teacher-assigned lessons
/uploaded-stories           Browse PDF stories
/uploaded-stories/:id/read  Read uploaded story journey
/ai-story                   Generate AI story
/progress                   Progress overview
/achievements               Badges
/mini-games                 Games

/parent                     Parent dashboard
/parent/children            Manage children
/parent/notifications       Notifications
/parent/recordings          Fluency recordings

/teacher                    Teacher dashboard
/teacher/lessons            Manage lessons
/teacher/groups             Student groups
/teacher/reports            Class reports

/school/dashboard           School dashboard
/school/teachers            Manage teachers (+ reset password modal)
/school/classrooms          Classrooms
/school/reports             School reports
/school/subscription        Subscription

/admin                      System admin (sidebar shell)
/admin/users                User management
/admin/schools              School accounts
/admin/content              Content management
/admin/import               PDF import
/admin/ai-settings          AI model config
/admin/pdf-library          PDF library
/admin/subscriptions        Subscription stats

/settings                   User settings
/upgrade                    Upgrade plan
```

---

## Roles & Auth

| Role | Value | Login Type | Can Do |
|------|-------|-----------|--------|
| Student | 0 | Username + ImagePin1 (+ optional Pin2) → `/api/auth/students/login` | Read lessons/stories, take exams, write, record fluency |
| Parent | 1 | Email + Password | View child progress, weakness map, add children |
| Teacher | 2 | Email + Password | Create lessons, manage groups, assign lessons, view class |
| SchoolAdmin | 3 | Email + Password | Create teacher accounts, manage classrooms, school reports |
| SystemAdmin | 4 | Email + Password | All of the above + user management, AI config, content |

**JWT Claims:** `sub` (UserId/StudentId), `role`, `level` (students only)
**Token expiry:** 30 days

---

## Test Accounts

| Role | Email | Password |
|------|-------|---------|
| SystemAdmin | `admin@lughati.com` | `Admin@Lughati2026` |
| SchoolAdmin | `school@lughati.com` | `School@2026` |
| Teacher | `teacher@lughati.com` | `Teacher@2026` |
| Parent | `parent@lughati.com` | `Parent@2026` |

Student accounts are created by parents/teachers via the create-student form.

---

## Config Files

| File | Purpose |
|------|---------|
| `Backend/.../appsettings.json` | DB connection, Gemini API key, Cloudinary, JWT secret, Email SMTP |
| `Frontend/.../environment.ts` | `apiUrl` pointing to backend |
| `Frontend/angular.json` | Build config, budget limits |
| `Frontend/src/styles.css` | Global Bootstrap import + CSS variables (`--primary`, `--bg-base`, `--text-muted`) |
