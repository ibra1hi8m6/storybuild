# Lughati — Arabic AI Educational Platform

A full-stack AI-powered platform for teaching Arabic to children aged 3–6. Built with ASP.NET Core (.NET 10), Angular, and SQL Server.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core (.NET 10), Clean Architecture, EF Core, SQL Server |
| Frontend | Angular (Standalone Components, SSR) |
| AI | Gemini 2.5 Flash (text, vision OCR, speech-to-text) |
| Storage | Cloudinary (images + audio) |
| RAG | ChromaDB + Ollama embeddings |
| OCR (lessons) | Tesseract (Arabic) |

---

## Architecture

```
Backend/trystorybuild/
├── Domain/           # Entities only — no dependencies
├── Application/      # DTOs, Interfaces, Use Cases, Prompts
├── Infrastructure/   # EF Core, AI services, PDF, Cloudinary, RAG
└── storybuild.API/   # Controllers, Middleware, Program.cs
```

---

## Roles

| Role | Access |
|---|---|
| Student | Lessons, stories, exams, writing practice, fluency recording |
| Teacher | Create/assign lessons, manage groups, view student progress |
| Parent | View child recordings, fluency scores, progress |
| School Admin | Manage classrooms, teachers, students |
| System Admin | Full platform control, PDF library, RAG, user management |

---

## Core Features

### 1. Lesson System (PDF Workbooks)
- Admin uploads Arabic letter workbooks (PDF)
- Tesseract OCR extracts sentences from each page
- Structured lesson books with cover + pages stored in DB
- Students unlock pages by completing handwriting practice
- RAG pipeline (ChromaDB) indexes lesson content for AI queries

### 2. AI Story Generator
- Student selects child name, character, theme
- Gemini 2.5 Flash generates 5-page Arabic story
- Cloudinary stores per-page cartoon images
- Story reader with page-by-page navigation
- MCQ exam auto-generated from story content

### 3. Uploaded Story Journey (PDF Stories)
- Admin uploads a story PDF
- PdfPig extracts embedded text; Gemini Vision OCR as fallback
- 3 content pages imported (skipping cover/credits)
- Images uploaded to Cloudinary
- Journey: Listen (Arabic TTS) → Read → Record & Evaluate

### 4. Fluency Evaluation
- Student records reading via microphone (WebM audio)
- Audio uploaded to Cloudinary + Gemini Files API
- Gemini 2.5 Flash transcribes Arabic speech
- Accuracy score, WCPM, mispronounced words calculated
- Parent can view all recordings with scores

### 5. Writing Practice
- Canvas component using Pointer API (mouse / touch / stylus)
- Student draws Arabic letter
- Gemini Vision evaluates similarity to expected letter
- Page unlocks on pass

### 6. Placement Test
- Entry-level quiz to determine student level (1–4)
- Drives which lessons and books are available

### 7. Messaging & Vocabulary
- In-app messages between teacher and student/parent
- Word journal: students save vocabulary from lessons

### 8. Classrooms & Groups
- Teachers create classrooms and student groups
- Assign specific lessons to groups

---

## API Endpoints (summary)

| Controller | Routes |
|---|---|
| AuthController | POST /api/auth/login, register, create-student |
| StoryController | GET/POST /api/story, /api/story/uploaded |
| LessonsController | GET/POST /api/lessons, /api/lessons/:id/pages |
| FluencyController | POST /api/fluency/evaluate, GET history |
| AdminController | CRUD for stories, users, PDF library |
| ParentPortalController | GET /api/parent-portal/child/:id/recordings |
| ExamController | GET/POST /api/exam |
| WritingController | POST /api/writing/evaluate |
| RagController | POST /api/rag/query |
| MessagesController | GET/POST /api/messages |

---

## Database Entities

`User`, `Student`, `Teacher`, `Parent`, `Classroom`, `StudentGroup`, `StudentGroupMember`, `LessonAssignment`, `Story`, `StoryPage`, `Lesson`, `LessonPage`, `PdfDocument`, `PdfPage`, `Exam`, `Question`, `StudentAnswer`, `StudentProgress`, `AudioRecording`, `FluencyReport`, `WritingAttempt`, `PlacementQuestion`, `RagPageChunk`, `KnowledgeDocument`, `Annotation`, `WordJournalEntry`, `LessonVocabulary`, `Message`, `LevelWordConfig`

---

## Known Issues & Bugs

See team task files:
- [TEAM-1-Backend-AI.md](TEAM-1-Backend-AI.md) — AI pipeline & OCR bugs
- [TEAM-2-Backend-API.md](TEAM-2-Backend-API.md) — API, Auth, Controllers
- [TEAM-3-Frontend-Student.md](TEAM-3-Frontend-Student.md) — Student-facing UI
- [TEAM-4-Frontend-Teacher-Parent.md](TEAM-4-Frontend-Teacher-Parent.md) — Teacher & Parent UI
- [TEAM-5-DevOps-QA.md](TEAM-5-DevOps-QA.md) — Testing, deployment, config

---

## Setup

### Backend
```bash
cd Backend/trystorybuild
# Fill appsettings.Development.json with:
# - ConnectionStrings:DefaultConnection (SQL Server)
# - Gemini:ApiKey
# - Cloudinary:CloudName, ApiKey, ApiSecret
dotnet run --project storybuild.API
```

### Frontend
```bash
cd Frontend/story-app
npm install
ng serve
```

### Required services
- SQL Server (remote or local)
- Cloudinary account
- Gemini API key (gemini-2.5-flash)
- ChromaDB instance (for RAG)

---

**GitHub:** github.com/ibra1hi8m6/storybuild
