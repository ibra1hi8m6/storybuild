# Team 2 — Backend: API, Auth & Controllers

**Owner:** Backend developer  
**Stack:** C#, ASP.NET Core, EF Core, SQL Server, JWT

---

## Your Responsibility

You own all controllers, auth logic, database migrations, repositories, and DTOs inside `storybuild.API/`, `Application/`, and `Infrastructure/Repositories/`.

---

## Current Bugs to Fix

### BUG-1: ParentPortalController has no authorization on child data
**File:** `storybuild.API/Controllers/ParentPortalController.cs`  
**Problem:** Any logged-in user can call `GET /api/parent-portal/child/{anyStudentId}/recordings` and see another student's recordings. There is no check that the requesting parent actually owns this child.  
**Fix needed:**
```csharp
// In GetChildRecordings(), after getting recordings:
var parentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
var student = await studentRepo.GetByIdAsync(studentId);
if (student?.ParentId != parentId)
    return Forbid();
```

### BUG-2: FluencyController — history endpoint returns all students' data
**File:** `storybuild.API/Controllers/FluencyController.cs` → `GetStudentHistory()`  
**Problem:** `GET /api/fluency/student/{studentId}` takes a studentId from the URL with no ownership check. A student could query another student's history.  
**Fix needed:** Add a check that `studentId == CurrentUserId()` or the caller is a teacher/parent of that student.

### BUG-3: AdminController — UploadStory has no file size validation
**File:** `storybuild.API/Controllers/AdminController.cs`  
**Problem:** Large PDF files (50MB+) can be uploaded, causing memory issues during rendering.  
**Fix needed:** Add a file size check before processing:
```csharp
if (pdfFile.Length > 20 * 1024 * 1024)
    return BadRequest(new { error = "File too large. Maximum 20MB." });
```

### BUG-4: DashboardService — missing null handling
**File:** `Infrastructure/Services/DashboardService.cs`  
**Problem:** Dashboard queries sometimes throw `NullReferenceException` when a student has no progress records yet (newly registered).  
**Fix needed:** Add `.DefaultIfEmpty()` and null coalescing where appropriate. Check all `.First()` calls and replace with `.FirstOrDefault()`.

### BUG-5: Placement test has no completion check
**File:** `storybuild.API/Controllers/PlacementController.cs`  
**Problem:** A student can retake the placement test multiple times, overwriting their level.  
**Fix needed:** Check if the student already has a completed placement result before allowing a new submission.

---

## Tasks to Build

### TASK-1: Refresh token support
Currently JWT expires and users get logged out without warning. Add a refresh token mechanism:
- Add `RefreshToken` and `RefreshTokenExpiry` fields to `User` entity
- Add `POST /api/auth/refresh` endpoint
- Generate refresh token on login, store in DB

### TASK-2: Lesson progress endpoint is incomplete
**File:** `storybuild.API/Controllers/ProgressController.cs`  
Verify all progress-tracking endpoints return the correct data for the student dashboard. The `StudentProgress` entity exists but some endpoints return empty arrays.

### TASK-3: Add pagination to story list
`GET /api/story` currently returns all stories with no limit. Add `?page=1&pageSize=10` query params.

### TASK-4: Global error handling — improve error responses
**File:** `storybuild.API/Middleware/ExceptionHandlingMiddleware.cs`  
Currently returns a generic 500 for all unhandled exceptions. Map specific exceptions:
- `InvalidOperationException` → 400 Bad Request
- `KeyNotFoundException` → 404 Not Found
- `UnauthorizedAccessException` → 403 Forbidden

---

## Files You Own

```
storybuild.API/
├── Controllers/
│   ├── AuthController.cs
│   ├── AdminController.cs
│   ├── StoryController.cs
│   ├── LessonsController.cs
│   ├── FluencyController.cs
│   ├── ParentPortalController.cs
│   ├── ExamController.cs
│   ├── WritingController.cs
│   ├── DashboardController.cs
│   ├── ProgressController.cs
│   ├── ClassroomsController.cs
│   ├── GroupsController.cs
│   ├── MessagesController.cs
│   ├── VocabularyController.cs
│   └── PlacementController.cs
└── Middleware/
    └── ExceptionHandlingMiddleware.cs

Application/
├── DTOs/               ← All DTO classes
├── Interfaces/         ← Repository interfaces
└── UseCases/           ← GenerateStoryUseCase.cs

Infrastructure/
├── Data/
│   ├── AppDbContext.cs
│   ├── DbSeeder.cs
│   └── EntityConfigurations.cs
├── Repositories/       ← All repository implementations
└── Migrations/         ← EF Core migrations
```

---

## How to Test

1. Run backend: `dotnet run --project storybuild.API`
2. Open Swagger: `https://localhost:7049/swagger`
3. Login as admin → test each endpoint
4. Try calling `/parent-portal/child/{id}/recordings` with a different user's JWT — should return 403
