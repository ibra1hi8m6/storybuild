# لغتي — توثيق المشروع الشامل
> آخر تحديث: 2026-06-17 | للاستخدام في التفكير مع Gemini وتحليل ما لدينا وما ينقص

---

## جدول المحتويات

1. نظرة عامة
2. Backend — Domain Entities
3. Backend — Controllers and Routes
4. Backend — Agents and Business Logic
5. Backend — Infrastructure Services
6. Backend — DTOs
7. Frontend — Angular Routes
8. Frontend — Guards
9. Frontend — Services
10. Frontend — Models
11. Frontend — Components
12. Data Flow — تدفق البيانات
13. قاعدة البيانات
14. مصفوفة ما لدينا وما ينقص

---

## 1. نظرة عامة

**المشروع:** منصة تعليمية للأطفال لتعلم اللغة العربية بالذكاء الاصطناعي.

| الجانب | التقنية |
|--------|--------|
| Backend | .NET 10 Clean Architecture (Domain/Application/Infrastructure/API) |
| Frontend | Angular 21 SSR Standalone Components Signals |
| Database | SQL Server Remote db55967.public.databaseasp.net |
| AI توليد | Gemini + Cloudflare FLUX |
| AI OCR | Gemini Vision |
| Vector DB | Chroma Cloud |
| Auth | JWT HS256 30 يوم |
| CSS | Bootstrap 5 + CSS Variables RTL |

**الأدوار:**
- Student = 0
- Parent = 1
- Teacher = 2
- SchoolAdmin = 3
- SystemAdmin = 4

**كود المدرسة (School Code):**
- C#: userId.ToString("N")[..8].ToUpper()
- TS: userId.replace(/-/g,"").substring(0,8).toUpperCase()

---

## 2. Backend — Domain Entities

المسار: Backend/trystorybuild/Domain/Entities/

### User.cs
- Id: Guid
- Name: string max 100
- Email: string max 200 Unique Index
- PasswordHash: string max 500
- Role: UserRole enum (Student=0, Parent=1, Teacher=2, SchoolAdmin=3, SystemAdmin=4)
- CreatedAt: DateTime
- IsActive: bool
- IsBlocked: bool
- Parent: navigation one-to-one
- Teacher: navigation one-to-one

### Student.cs
- Id: Guid
- Name: string max 100
- Age: int
- Username: string max 50 Unique
- LoginMethod: enum (ImagePin=0, TextPassword=1)
- ImagePin1: int (رقم emoji 0-19)
- ImagePin2: int? (اختياري)
- PasswordHash: string?
- AvatarUrl: string?
- Level: int (1,2,3)
- PlacementDone: bool
- ParentId: Guid?
- TeacherId: Guid?
- CreatedAt: DateTime

### Teacher.cs
- Id: Guid (same as User)
- IsPrivate: bool (true = معلم خاص)
- SchoolCode: string? (كود المدرسة)
- User: User navigation
- Students: List<Student>

### Parent.cs
- Id: Guid (same as User)
- User: User navigation
- Children: List<Student>

### Story.cs
- Id: Guid
- Title: string max 200
- ChildName: string max 100
- Character: string max 100 (الشخصية: أسد، قط...)
- Theme: string max 100 (الموضوع: غابة، بحر...)
- IsApproved: bool (اجتازت Judge)
- CreatedAt: DateTime
- Pages: List<StoryPage>
- Exams: List<Exam>
- Progress: List<StudentProgress>

### StoryPage.cs
- Id: Guid
- StoryId: Guid
- PageNumber: int (1-3)
- Sentence: string max 500
- ImagePrompt: string max 1000
- ImagePath: string max 500
- IsUnlocked: bool

### Lesson.cs
- Id: Guid
- Level: int (1-4)
- Letter: string max 10 (أ، ب، ت...)
- LetterName: string max 100 (ألف، باء...)
- Title: string max 200
- CoverImagePath: string max 500
- PromptText: string
- IsGenerated: bool
- CreatorId: Guid?
- CreatorRole: string (Admin/Teacher/Student)
- CreatedAt: DateTime
- Pages: List<LessonPage>
- Assignments: List<LessonAssignment>

### LessonPage.cs
- Id: Guid
- LessonId: Guid
- PageNumber: int
- Sentence: string max 500
- ImagePath: string max 500
- ImagePrompt: string
- IsCoverPage: bool
- IsUnlocked: bool
- WritingAttempts: List<WritingAttempt>

### Exam.cs
- Id: Guid
- StoryId: Guid? (اختبار قصة)
- LessonId: Guid? (اختبار درس)
- CreatedAt: DateTime
- Questions: List<Question>

### Question.cs
- Id: Guid
- ExamId: Guid
- QuestionNumber: int
- Type: QuizType (MCQ=0, Matching=1, DragDrop=2, Ordering=3)
- Text: string max 500
- OptionA/B/C/D: string? max 200 (للـ MCQ)
- CorrectAnswer: string max 2000
- DataJson: string (JSON للـ Matching/DragDrop/Ordering)
- Answers: List<StudentAnswer>

### StudentAnswer.cs
- Id: Guid
- QuestionId: Guid
- ChildName: string max 100
- ChosenAnswer: string max 2000
- IsCorrect: bool
- AnsweredAt: DateTime

### StudentProgress.cs
- Id: Guid
- StoryId: Guid?
- LessonId: Guid?
- ChildName: string max 100
- CurrentPage: int
- TotalQuestions: int
- CorrectAnswers: int
- ScorePercentage: double
- ExamCompleted: bool
- LastUpdatedAt: DateTime

### WritingAttempt.cs
- Id: Guid
- LessonPageId: Guid
- ChildName: string
- UploadedImagePath: string max 500
- ExtractedText: string max 1000 (نص OCR)
- ExpectedSentence: string max 500
- SimilarityScore: double (0-100)
- IsAccepted: bool (>= 70%)
- AttemptedAt: DateTime

### StudentGroup.cs
- Id: Guid
- Name: string
- TeacherId: Guid
- CreatedAt: DateTime
- Teacher: Teacher
- Members: List<StudentGroupMember>

### StudentGroupMember.cs
- GroupId: Guid — PK composite
- StudentId: Guid — PK composite
- AddedAt: DateTime

### LessonAssignment.cs
- Id: Guid
- LessonId: Guid
- TeacherId: Guid
- TargetType: string (Student | Group)
- TargetStudentId: Guid?
- TargetGroupId: Guid?
- AssignedAt: DateTime

### Classroom.cs (جديد)
- Id: Guid
- Name: string
- Level: int (1-3)
- SchoolCode: string
- TeacherId: Guid
- CreatedAt: DateTime
- Students: List<ClassroomStudent>

### ClassroomStudent.cs (جديد — جدول وسيط)
- ClassroomId: Guid — PK composite
- StudentId: Guid — PK composite

### PlacementQuestion.cs
- Id: Guid
- Part: int (1, 2, 3)
- Order: int (1-5)
- QuestionText: string
- ImageContent: string (emoji أو حرف)
- OptionsJson: string (JSON array)
- CorrectAnswer: string
- AudioText: string

### LevelWordConfig.cs
- Level: int — PK (1-4)
- WordCount: int
- ExampleSentence: string
- UpdatedAt: DateTime

### KnowledgeDocument.cs
- Id: Guid
- FileName: string max 500
- FilePath: string max 1000
- DocumentType: string max 20 (pdf/pptx/image)
- Letter: string?
- Level: int?
- Tags: string?
- ChunkCount: int
- IngestedAt: DateTime

### RagPageChunk.cs
- Id: Guid
- SourceFile: string
- PageNumber: int
- Sentence: string
- WordCount: int
- ImagePath: string
- Level: int
- Letter: string
- LetterName: string
- ChromaChunkId: string
- CreatedAt: DateTime

---

## 3. Backend — Controllers and Routes

المسار: Backend/trystorybuild/storybuild.API/Controllers/

### AuthController /api/auth

| Method | Path | Guard | الوظيفة |
|--------|------|-------|---------|
| POST | /register | none | تسجيل بالغ |
| POST | /login | none | دخول بالغ |
| POST | /students | Parent,Teacher | إنشاء حساب طالب |
| POST | /students/login | none | دخول الطالب بـ PIN |
| GET | /students | Parent,Teacher | قائمة الطلاب |
| GET | /me | Auth | المستخدم الحالي |
| PATCH | /students/{id}/level | Parent,Teacher | تعديل مستوى |
| PATCH | /students/me/level | Student | الطالب يغير مستواه |
| GET | /school/teachers | SchoolAdmin | معلمو المدرسة |

Functions في AuthController:
- Register(RegisterRequest req)
- Login(LoginRequest req)
- CreateStudent(CreateStudentRequest req)
- StudentLogin(StudentLoginRequest req)
- GetStudents()
- GetMe()
- UpdateStudentLevel(Guid studentId, int level)
- UpdateMyLevel(int level)
- GetSchoolTeachers()
- CurrentUserId() — Guid من JWT
- SchoolCode() — CurrentUserId()[..8].ToUpper()

### StoryController /api/story

| Method | Path | الوظيفة |
|--------|------|---------|
| POST | /generate | توليد قصة AI |
| GET | /{id} | جلب قصة |
| GET | / | كل القصص |
| GET | /mine/{childName} | قصص طفل |
| DELETE | /{id} | حذف قصة |

### LessonsController /api/lessons

| Method | Path | الوظيفة |
|--------|------|---------|
| GET | ?level= | دروس حسب المستوى |
| GET | /{id} | درس كامل |
| POST | /generate | توليد درس AI |
| POST | /manual | درس يدوي |
| DELETE | /{id} | حذف درس |
| GET | /my/{creatorId} | دروس منشئ محدد |

### ExamController /api/exam

| Method | Path | الوظيفة |
|--------|------|---------|
| POST | /generate/{storyId} | توليد اختبار قصة |
| POST | /generate/lesson/{lessonId} | توليد اختبار درس |
| GET | /story/{storyId} | جلب اختبار موجود |
| POST | /submit | تقديم الإجابات |

### WritingController /api/writing

| Method | Path | الوظيفة |
|--------|------|---------|
| POST | /evaluate | تصحيح كتابة يدوية من صورة |
| POST | /canvas | تصحيح رسم Canvas |

قاعدة القبول: similarityScore >= 70 => isAccepted = true => فك قفل الصفحة التالية

### PlacementController /api/placement

| Method | Path | الوظيفة |
|--------|------|---------|
| GET | /questions | 15 سؤال (3 أجزاء × 5) |
| POST | /submit | تقديم الإجابات |

قاعدة التصنيف:
- الجزء 1 أو 2 راسب => المستوى 1
- الجزء 3 راسب => المستوى 2
- كل الأجزاء ناجح => المستوى 3

### ProgressController /api/progress

| Method | Path | الوظيفة |
|--------|------|---------|
| GET | /{storyId}/{childName} | جلب التقدم |
| PUT | / | حفظ تقدم القصة |
| PUT | /lesson | حفظ تقدم الدرس |

### ClassroomsController /api/classrooms (جديد)

| Method | Path | Guard | الوظيفة |
|--------|------|-------|---------|
| GET | / | SchoolAdmin | كل فصول المدرسة |
| GET | /{id} | SchoolAdmin | فصل + طلابه |
| POST | / | SchoolAdmin | إنشاء فصل |
| PUT | /{id} | SchoolAdmin | تعديل فصل |
| DELETE | /{id} | SchoolAdmin | حذف فصل |
| POST | /{id}/students | SchoolAdmin | إضافة طالب للفصل |
| DELETE | /{id}/students/{studentId} | SchoolAdmin | إزالة طالب |
| GET | /school-students?q= | SchoolAdmin | بحث في طلاب المدرسة |
| GET | /report | SchoolAdmin | تقرير الفصول مع النتائج |
| GET | /my | Teacher | فصولي كمعلم |

Business Logic:
- عند إضافة طالب: student.TeacherId = classroom.TeacherId
- SchoolCode = CurrentUserId().ToString("N")[..8].ToUpper()

### GroupsController /api/groups

| Method | Path | الوظيفة |
|--------|------|---------|
| GET | /teacher/{teacherId} | مجموعات معلم |
| POST | /teacher/{teacherId} | إنشاء مجموعة |
| POST | /{groupId}/members | إضافة طالب |
| DELETE | /{groupId}/members/{studentId} | إزالة طالب |
| DELETE | /{groupId} | حذف مجموعة |
| POST | /assign | تعيين درس لطالب/مجموعة |
| GET | /assignments/teacher/{teacherId} | تعيينات المعلم |
| GET | /assigned/student/{studentId} | دروس الطالب المعينة |

### DashboardController /api/dashboard

| Method | Path | Guard | الوظيفة |
|--------|------|-------|---------|
| GET | /student/{childName} | Auth | لوحة الطالب |
| GET | /parent/{childName} | Parent | لوحة ولي الأمر |
| GET | /teacher | Teacher | لوحة المعلم |
| GET | /school | SchoolAdmin | لوحة المدرسة |

### RagController /api/rag

| Method | Path | الوظيفة |
|--------|------|---------|
| POST | /ingest | استيعاب وثيقة |
| POST | /ingest-educational | PDF تعليمي بـ Gemini Vision |
| GET | /page-chunks | كل الـ Chunks |
| GET | /documents | كل المستندات |
| DELETE | /documents/{id} | حذف مستند |
| POST | /search | بحث دلالي |
| POST | /generate-lesson | توليد درس من RAG |

### AdminController /api/admin

| Method | Path | الوظيفة |
|--------|------|---------|
| GET | /users | كل المستخدمين |
| POST | /users/{id}/block | حجب |
| POST | /users/{id}/unblock | رفع الحجب |
| GET | /schools | كل المدارس |
| POST | /schools | إنشاء مدرسة |
| GET | /subscriptions | إحصائيات |
| GET+PUT | /ai-settings | إعدادات AI |
| GET | /stories | قصص للمراجعة |

---

## 4. Backend — Agents and Business Logic

المسار: Backend/trystorybuild/Application/Agent/

### StoryAgent.cs
RunAsync(childName, character, theme) => GenerateStoryResponse

الخطوات:
1. GeminiStoryGeneratorService.GenerateAsync() => Title + 3x{Sentence, ImagePrompt}
2. GeminiJudgeService.ValidateAsync() => هل المحتوى مناسب? (retry x3)
3. CloudflareImageService.GenerateImageAsync() × 3 => imageUrls
4. IStoryRepository.SaveAsync(story + pages)
5. return GenerateStoryResponse

### ExamAgent.cs
- GenerateForStoryAsync(storyId) => ExamResponse
- GenerateForLessonAsync(lessonId) => ExamResponse
- GradeAsync(submitRequest) => ExamResultResponse

التوليد:
1. جلب Story/Lesson من DB
2. GeminiExamGeneratorService => MCQ + Matching + DragDrop + Ordering
3. للدروس: imageUrl من صفحات الدرس
4. حفظ Exam + Questions
5. return ExamResponse

التصحيح:
1. جلب الإجابات الصحيحة
2. مقارنة ChosenAnswer بـ CorrectAnswer
3. scorePercentage = (correct / total) * 100
4. حفظ StudentAnswers
5. return ExamResultResponse مع feedback

### WritingCorrectionAgent.cs
EvaluateAsync(image, expectedSentence, childName, lessonPageId) => WritingCorrectionResponse

الخطوات:
1. رفع الصورة لـ Cloudflare R2
2. GeminiOcrService.ExtractArabicTextAsync() => نص
3. GeminiTextCleanupService.CleanupAsync() => نص منظّف
4. ArabicSimilarityService.Calculate(expected, extracted) => 0-100
5. isAccepted = score >= 70
6. إذا isAccepted => LessonPage.IsUnlocked = true
7. IWritingAttemptRepository.SaveAsync()
8. return WritingCorrectionResponse

### LessonGenerationAgent.cs
GenerateAsync(request) => LessonDetailResponse

الخطوات:
1. LevelWordConfigRepository.GetAsync(level) => wordCount
2. RagQueryService.GetContextAsync(letter, level) => ChromaVectorStore => RAG context
3. GeminiLessonGeneratorService => Title + Pages[]{Sentence, ImagePrompt}
4. CloudflareImageService × N صفحات
5. هيكل الصفحات: الأولى Cover مفتوحة، الثانية مفتوحة، الباقي مقفل
6. LessonRepository.SaveAsync()
7. إذا targetGroupId/studentId => LessonAssignment
8. return LessonDetailResponse

### DashboardService.cs
GetStudentDashboardAsync(childName):
  - Stars, StoriesRead, ExamsCompleted, AvgScore
  - WeeklyActivity, ExamHistory, RecentActivity
  - PerformanceLevel: ممتاز(>=80%) | جيد(>=50%) | يحتاج تحسين

GetTeacherDashboardAsync(teacherId):
  - طلاب مباشرون: s.TeacherId == teacherId
  - + طلاب الفصول: db.Classrooms.Where(c.TeacherId==teacherId).SelectMany()
  - Dedup بالاسم
  - PerformanceBands

GetSchoolDashboardAsync(schoolCode):
  - TotalStudents, TotalTeachers, ActiveThisWeek, AvgSchoolScore
  - PerformanceBands

GetClassroomsAsync(teacherId):
  - يجرب Classrooms table أولاً
  - Fallback: فصول افتراضية حسب المستوى

---

## 5. Backend — Infrastructure Services

### AI Services (Infrastructure/AI/)

| الفئة | الوظيفة |
|-------|---------|
| GeminiStoryGeneratorService | توليد قصة عربية |
| GeminiExamGeneratorService | توليد أسئلة اختبار |
| GeminiJudgeService | فحص سلامة المحتوى |
| GeminiOcrService | استخراج نص عربي من صورة |
| GeminiTextCleanupService | تنظيف نص OCR |
| GeminiEmbeddingService | نص => vector |
| CloudflareImageService | توليد صور FLUX |
| ArabicSimilarityService | قياس تشابه نصين |

### RAG Services (Infrastructure/Rag/)

| الفئة | الوظيفة |
|-------|---------|
| RagIngestionService | تقطيع + embedding + Chroma |
| RagQueryService | بحث دلالي + سياق |
| ChromaVectorStoreService | تفاعل مع Chroma Cloud |
| PdfDocumentProcessor | استخراج من PDF |
| EducationalPdfIngestionService | Gemini Vision لكل صفحة |
| ArabicTextChunker | تقطيع يحترم النحو العربي |

### AuthService (Infrastructure/Auth/AuthService.cs)

RegisterAsync(req):
  - BCrypt.HashPassword(password)
  - Create User + Parent أو Teacher
  - SchoolAdmin: SchoolCode = userId[..8].ToUpper()
  - Teacher: IsPrivate = (SchoolCode == null)

LoginAsync(req):
  - BCrypt.Verify(password, hash)
  - يتحقق IsActive && !IsBlocked

CreateStudentAsync(creatorId, req):
  - التحقق من Username فريد
  - ربط بـ Parent أو Teacher

StudentLoginAsync(req):
  - مطابقة Username + ImagePin1 + (ImagePin2 إن وجد)

GenerateJwt(user):
  - Claims: userId, email, role
  - HS256, 30 days

---

## 6. Backend — DTOs

### Auth DTOs
- RegisterRequest: FullName, Email, Password, Role, SchoolCode?
- LoginRequest: Email, Password
- AuthResponse: Token, UserId, Name, Role, ExpiresAt, SchoolCode?
- CreateStudentRequest: Name, Age, Username, ImagePin1, ImagePin2?, Level
- StudentLoginRequest: Username, ImagePin1, ImagePin2?
- StudentAuthResponse: Token, StudentId, Name, Level, PlacementDone, ExpiresAt

### Story DTOs
- GenerateStoryRequest: ChildName, Character, Theme
- GenerateStoryResponse: Id, Title, IsApproved, Pages[]
- StoryPageDto: PageId, PageNumber, Sentence, ImageUrl, IsUnlocked

### Exam DTOs
- ExamResponse: ExamId, StoryId, Questions[]
- QuestionDto: QuestionId, Type, Text, OptionA-D?, DataJson?, ImageUrl?
- SubmitExamRequest: ExamId, ChildName, Answers[{QuestionId, ChosenAnswer}]
- ExamResultResponse: TotalQuestions, CorrectAnswers, ScorePercentage, Feedback[]

### Placement DTOs
- PlacementQuestionDto: Id, Part, Order, QuestionText, ImageContent, Options[], AudioText
- PlacementSubmitRequest: Answers[{QuestionId, ChosenAnswer}]
- PlacementResultDto: TotalScore, Part1-3Score, AssignedLevel, LevelName, Message

### Dashboard DTOs
- StudentDashboardDto: ChildName, Stars, StoriesRead, LessonsCompleted, ExamsCompleted, AvgScore, WritingAttempts, WritingAccepted, WritingAcceptanceRate, PerformanceLevel, CurrentStreak, WeeklyActivity[], InProgressLessons[], TopStories[], TopLessons[], ExamHistory[], RecentActivity[]
- TeacherDashboardDto: TotalStudents, ActiveThisWeek, AvgClassScore, Students[], PerformanceBands[], TopStories[], TopLessons[]
- SchoolDashboardDto: TotalStudents, TotalTeachers, ActiveThisWeek, AvgSchoolScore, PerformanceBands[]
- ClassroomReportDto: ClassroomId, ClassroomName, Level, TeacherName, StudentCount, AvgScore, Students[]

---

## 7. Frontend — Angular Routes

الملف: Frontend/story-app/src/app/app.routes.ts

### عامة
- / => HomeComponent
- /auth/login => LoginComponent
- /auth/register => RegisterComponent
- /auth/create-student => CreateStudentComponent [authGuard]
- /settings => SettingsComponent
- /upgrade => UpgradeComponent
- ** => NotFoundComponent

### طالب
- /test => PlacementWelcomeComponent
- /test/question => PlacementQuestionComponent
- /test/result => PlacementResultComponent
- /dashboard => StudentDashboardComponent [authGuard]
- /levels => LevelsComponent [authGuard]
- /levels/:id/books => BooksComponent [authGuard]
- /books/:id/read => StoryReaderComponent [authGuard]
- /books/:id/quiz-result => QuizResultComponent [authGuard] [Client SSR]
- /writing-practice => WritingPracticeComponent [authGuard]
- /progress => ProgressComponent [authGuard]
- /my-stories => MyStoriesComponent [authGuard]
- /achievements => AchievementsComponent [authGuard]
- /generate-story => StoryGeneratorComponent [authGuard]
- /generate-lesson => GenerateLessonComponent [authGuard]
- /my-lessons => MyLessonsComponent [authGuard]
- /assigned-lessons => AssignedLessonsComponent [authGuard]
- /ai-story => AiStoryWizardComponent
- /lessons/:id => LessonReaderComponent
- /lessons-list => LessonsListComponent [authGuard]
- /exam => Exam [authGuard]
- /writing-correction => WritingCorrectionComponent [authGuard]

### ولي الأمر
- /parent/dashboard => ParentDashboardComponent [parentGuard]
- /parent/children => ParentChildrenComponent [parentGuard]
- /parent/notifications => ParentNotificationsComponent [parentGuard]
- /parent/child/:name/progress => ChildProgressComponent [parentGuard]

### معلم
- /teacher/students => TeacherDashboardComponent [teacherGuard]
- /teacher/ai-generator => AiGeneratorComponent [teacherGuard]
- /teacher/lessons/create => LessonCreateComponent [teacherGuard]
- /teacher/reports => TeacherReportsComponent [teacherGuard]
- /teacher/lessons => TeacherLessonsComponent [teacherGuard]
- /teacher/students/:name => StudentDetailComponent [teacherGuard]
- /teacher/groups => TeacherGroupsComponent [teacherGuard]
- /teacher/classes => TeacherClassesComponent [teacherGuard]

### مدرسة
- /school/dashboard => SchoolDashboardComponent [schoolGuard]
- /school/teachers => SchoolTeachersComponent [schoolGuard]
- /school/classrooms => SchoolClassroomsComponent [schoolGuard]
- /school/reports => SchoolReportsComponent [schoolGuard]
- /school/subscription => SchoolSubscriptionComponent [schoolGuard]

### مدير النظام
- /admin/rag => AdminRagComponent [adminGuard]
- /admin/pdf-library => AdminPdfLibraryComponent [adminGuard]
- /admin/content => AdminContentComponent [adminGuard]
- /admin/books => AdminImportComponent [adminGuard]
- /admin/ai-settings => AiSettingsComponent [adminGuard]
- /admin/subscriptions => SubscriptionsComponent [adminGuard]
- /admin/users => AdminUsersComponent [adminGuard]
- /admin/schools => AdminSchoolsComponent [adminGuard]
- /admin/stories => AdminStoriesComponent [adminGuard]
- /admin/rag-chunks => AdminRagChunksComponent [adminGuard]

---

## 8. Frontend — Guards

الملف: Frontend/story-app/src/app/core/auth.guard.ts

- authGuard => isLoggedIn()
- studentGuard => isStudent()
- parentGuard => isParent()
- teacherGuard => isTeacher() || isAdmin()
- schoolGuard => isSchoolAdmin() || isTeacher() || isAdmin()
- adminGuard => isSystemAdmin() || isAdmin()

كل Guard: true أو redirect الى /auth/login

---

## 9. Frontend — Services

### AuthService (services/auth.service.ts)

localStorage Keys:
- lughati_token => JWT
- lughati_user => CurrentUser JSON
- lughati_child => اسم الطفل

Signals:
- _token: signal<string|null>
- _user: signal<CurrentUser|null>

Computed:
- isLoggedIn() => !!token()
- userRole() => currentUser()?.role ?? ""
- isStudent() => role === "student"
- isParent() => role === "parent"
- isTeacher() => role === "teacher"
- isSchoolAdmin() => role === "schooladmin"
- isAdmin() => role === "systemadmin" || "admin"

Methods:
- register(body) => POST /api/auth/register
- login(email, password) => POST /api/auth/login
- studentLogin(username, pin1, pin2?) => POST /api/auth/students/login
- createStudent(req) => POST /api/auth/students
- getMyStudents() => GET /api/auth/students
- updateChildLevel(id, level) => PATCH /api/auth/students/{id}/level
- getSchoolTeachers() => GET /api/auth/school/teachers
- logout() => مسح localStorage + Signals
- persistSession(res) => حفظ token + user
- persistStudentSession(res) => حفظ token + student
- loadUserFromStorage() => تحميل عند init

### StoryService (services/story.ts)

Stories:
- generateStory(req) => POST /api/story/generate
- getStory(id) => GET /api/story/{id}
- getAllStories() => GET /api/story
- getMyStories(childName) => GET /api/story/mine/{childName}
- deleteStory(id) => DELETE /api/story/{id}

Exams:
- generateExam(storyId) => POST /api/exam/generate/{storyId}
- generateLessonExam(lessonId) => POST /api/exam/generate/lesson/{lessonId}
- getOrGenerateExam(storyId) => GET /api/exam/story/{storyId}
- submitExam(req) => POST /api/exam/submit

Writing:
- submitLessonWriting(lessonId, pageId, childName, imageBlob) => POST /api/writing/evaluate
- evaluateCanvasWriting(base64, expectedText) => POST /api/writing/canvas

Lessons:
- getLessonsByLevel(level) => GET /api/lessons?level={level}
- getLesson(id) => GET /api/lessons/{id}
- generateLesson(req) => POST /api/lessons/generate
- createManualLesson(req) => POST /api/lessons/manual
- deleteLesson(id) => DELETE /api/lessons/{id}
- getMyLessons(creatorId) => GET /api/lessons/my/{creatorId}

Progress:
- getProgress(storyId, childName) => GET /api/progress/{storyId}/{childName}
- updateProgress(progress) => PUT /api/progress
- updateLessonProgress(req) => PUT /api/progress/lesson

Dashboards:
- getStudentDashboard(childName) => GET /api/dashboard/student/{childName}
- getParentDashboard(childName) => GET /api/dashboard/parent/{childName}
- getTeacherDashboard() => GET /api/dashboard/teacher
- getSchoolDashboard() => GET /api/dashboard/school

Classrooms:
- getSchoolClassrooms() => GET /api/classrooms
- createClassroom(body) => POST /api/classrooms
- getClassroomDetail(id) => GET /api/classrooms/{id}
- editClassroom(id, body) => PUT /api/classrooms/{id}
- deleteClassroom(id) => DELETE /api/classrooms/{id}
- addStudentToClassroom(classId, studentId) => POST /api/classrooms/{id}/students
- removeStudentFromClassroom(cId, sId) => DELETE /api/classrooms/{id}/students/{sId}
- searchSchoolStudents(q) => GET /api/classrooms/school-students?q=
- getClassroomsReport() => GET /api/classrooms/report
- getMyTeacherClassrooms() => GET /api/classrooms/my

Groups:
- getTeacherGroups(teacherId) => GET /api/groups/teacher/{teacherId}
- createGroup(teacherId, name) => POST /api/groups/teacher/{teacherId}
- addGroupMember(groupId, studentId) => POST /api/groups/{groupId}/members
- removeGroupMember(gId, sId) => DELETE /api/groups/{gId}/members/{sId}
- deleteGroup(groupId) => DELETE /api/groups/{groupId}
- assignLesson(req) => POST /api/groups/assign
- getTeacherAssignments(teacherId) => GET /api/groups/assignments/teacher/{teacherId}
- getAssignedLessons(studentId) => GET /api/groups/assigned/student/{studentId}

Admin:
- getAiSettings() => GET /api/admin/ai-settings
- saveAiSettings(settings) => PUT /api/admin/ai-settings
- getSubscriptionStats() => GET /api/admin/subscriptions
- getAllUsers() => GET /api/admin/users
- blockUser(id) / unblockUser(id) => POST /api/admin/users/{id}/block|unblock
- getSchools() => GET /api/admin/schools
- createSchool(body) => POST /api/admin/schools
- getPlacementQuestions() => GET /api/placement/questions
- submitPlacement(req) => POST /api/placement/submit
- updateStudentLevel(level) => PATCH /api/auth/students/me/level
- ingestDocument(file, letter?, level?, tags?) => POST /api/rag/ingest
- getKnowledgeDocuments() => GET /api/rag/documents
- deleteKnowledgeDocument(id) => DELETE /api/rag/documents/{id}
- getRagPageChunks(level?, letter?) => GET /api/rag/page-chunks

### AppStateService (services/app-state-service.ts)

Signals:
- _user: signal<CurrentUser|null>
- _lang: signal<"ar"|"en">
- _childName: signal<string>
- currentStory: signal<StoryResponse|null>
- currentLesson: signal<LessonDetail|null>
- currentExamResult: signal<ExamResult|null>
- currentPage: signal<number>
- lessonStarted: signal<boolean>

Computed:
- isLoggedIn() => !!_user()
- userRole() => _user()?.role ?? ""
- currentUserName() => _user()?.name ?? ""
- isRtl() => _lang() === "ar"
- activePage() => الصفحة الحالية من story أو lesson
- totalPages() => عدد الصفحات
- isLastPage() => currentPage() >= totalPages()
- progressPercent() => (currentPage / totalPages) * 100

Methods:
- setUser(user) => حفظ في localStorage + Signal
- logout() => مسح كل شيء
- updateStudentLevel(level, newToken?) => تحديث level + token
- setLang(lang) => تغيير dir على document.documentElement
- setChildName(name) => localStorage["lughati_child"]
- setStory(story) => currentStory + currentPage=1
- setLesson(lesson) => currentLesson + currentPage=1
- updateLessonPages(pages) => تحديث صفحات الدرس
- goToPage(page) => currentPage = page
- nextPage() => currentPage++
- startLesson() => lessonStarted = true
- setExamResult(result) => currentExamResult = result
- saveLessonProgress(id, page, total, done) => localStorage["lughati_lesson_progress"]
- lessonProgress(lessonId) => جلب تقدم درس من localStorage
- reset() => مسح currentStory/Lesson/Page

---

## 10. Frontend — Models (story.models.ts)

- QuizType: MCQ=0, Matching=1, DragDrop=2, Ordering=3
- CurrentUser: { id, name, email, role, level?, placementDone?, schoolCode? }
- StoryPage: { pageId, pageNumber, sentence, imageUrl, isUnlocked }
- LessonPage: { pageId, pageNumber, sentence, imageUrl, isUnlocked, isCoverPage }
- StoryResponse: { id, title, isApproved, pages: StoryPage[] }
- LessonSummary: { id, level, letter, letterName, title, coverImageUrl, pageCount }
- LessonDetail: { id, level, letter, letterName, title, coverImageUrl, pages: LessonPage[] }
- QuestionDto: { questionId, questionNumber, type, text, optionA-D?, dataJson?, imageUrl? }
- ExamResponse: { examId, storyId, questions: QuestionDto[] }
- SubmitExamRequest: { examId, childName, answers: { questionId, chosenAnswer }[] }
- ExamResult: { totalQuestions, correctAnswers, scorePercentage, feedback: AnswerFeedback[] }
- AnswerFeedback: { questionId, isCorrect, yourAnswer, correctAnswer }
- WritingCorrectionResponse: { extractedText, expectedSentence, similarityScore, isAccepted, message }
- ProgressResponse: { storyId, childName, currentPage, totalQuestions, correctAnswers, scorePercentage, examCompleted }

---

## 11. Frontend — Components (الرئيسية)

### LoginComponent (features/auth/login/)
Signals: flow, isLoading, error, selectedPins
emojiOptions = 20 رمز: rabbit, duck, fish, panda, lion, dog, cat, frog, koala, fox, flower, star, rainbow, apple, balloon, rocket, music, trophy, moon, heart
توجيه حسب الدور: parent=>/parent/dashboard | teacher=>/teacher/students | schooladmin=>/school/dashboard | systemadmin=>/admin/users | student=>/dashboard أو /test إذا !placementDone

### StoryReaderComponent (features/story-reader/)
Signals: story, pageNum, totalPages, isPlaying, imageLoaded
Methods: nextPage | prevPage | playAudio (SpeechSynthesis Arabic) | stopAudio | goToExam
Keyboard: ArrowLeft=next | ArrowRight=prev | Space=toggle audio

### ExamComponent (features/exam/exam.ts)
Signals: exam, answers (Map), result, isLoading, submitted
Types: MCQ => 4 أزرار | Matching => زوج | DragDrop => سحب | Ordering => ترتيب
submitExam: story=>{sessionStorage[quiz_result] => /quiz-result} | lesson=>{نتيجة in-page}

### QuizResultComponent (features/quiz-result/)
Data: sessionStorage["quiz_result"] = {correct, total}
Computed: score = correct/total * 100
Methods: tryAgain() | goHome()

### WritingPracticeComponent (features/writing-practice/)
Canvas 600x300 | startDraw | draw | endDraw | clearCanvas | captureAsBlob => PNG
submitWriting => evaluateCanvasWriting(base64, expected) => {score, isAccepted}

### StudentDashboardComponent (features/dashboards/student-dashboard/)
Signals: dashboard, isLoading, childName
Computeds: stars | storiesRead | avgScore | performanceLevel | weeklyActivity

### SchoolClassroomsComponent (features/school/classrooms/)
Methods: loadClassrooms | createClassroom | startEdit/saveEdit/cancelEdit | deleteClassroom
toggleExpand(cls) | loadStudents(id) | removeStudent | openAddStudent | onSearchInput | addStudentToClass
progressColor(p): >=80=green | >=50=yellow | red
levelLabel(l): المستوى الأول | الثاني | الثالث

### SchoolReportsComponent (features/school/reports/)
Signals: dashboardData, classroomReport, isLoading
Methods: toggleExpand | bandColor | barWidth | levelColor | scoreColor

### SchoolSubscriptionComponent (features/school/subscription/)
Signals: isLoading, dashboard, classrooms
Computeds: totalStudents, totalTeachers, totalClassrooms
Constants: planName="الخطة المجانية" | planLimit={students:50, teachers:5, classrooms:10} | contactEmail
Methods: usagePct(used,limit) | usageColor(pct): >=90=red | >=70=yellow | green

### TeacherClassesComponent (features/teacher/teacher-classes/)
Signals: classrooms, isLoading
Computeds: allStudents() => flatten+dedup | classGroups() => تجميع حسب اسم
Methods: loadClassrooms() => GET /api/classrooms/my | goToAssignLesson(level?)

---

## 12. Data Flow — تدفق البيانات

### توليد القصة
StoryGeneratorComponent => StoryService.generateStory => POST /api/story/generate
=> StoryAgent: Gemini(title+sentences) => Judge(valid?) => Cloudflare(images) => DB.Save
<= GenerateStoryResponse => AppStateService.setStory => router /books/{id}/read
=> StoryReaderComponent: pages + TTS + progress

### اختبار التحديد
PlacementQuestionComponent => GET /api/placement/questions (15 سؤال)
=> POST /api/placement/submit => {Part1 fail=Level1 | Part2 fail=Level1 | Part3 fail=Level2 | all pass=Level3}
=> PATCH /api/auth/students/me/level => AppStateService.updateStudentLevel => /dashboard

### إدارة الفصول
SchoolClassroomsComponent => POST /api/classrooms {name,level,teacherId}
  SchoolCode = CurrentUserId()[..8].ToUpper() => DB.Save => Classroom
إضافة طالب => GET /api/classrooms/school-students?q= => search results
  => POST /api/classrooms/{id}/students => student.TeacherId = classroom.TeacherId => DB.Save
TeacherClassesComponent => GET /api/classrooms/my <= فصولي + طلابي

### الكتابة اليدوية
WritingPracticeComponent: Canvas => captureAsBlob => POST /api/writing/canvas
=> WritingCorrectionAgent: GeminiOCR => TextCleanup => ArabicSimilarity(score)
  isAccepted = score>=70 => LessonPage.IsUnlocked=true => DB.Save
<= {score, isAccepted} => عرض النتيجة => إذا مقبول: الصفحة التالية

### تعيين الدروس
LessonCreateComponent => POST /api/lessons/generate {letter, level, targetGroupId}
=> LessonGenerationAgent: LevelWordConfig => RagQuery(Chroma) => Gemini(title+pages) => Cloudflare(images)
  Page1=Cover مفتوحة | Page2 مفتوحة | باقي مقفل => LessonAssignment
AssignedLessonsComponent (طالب) => GET /api/groups/assigned/student/{id} => /lessons/{id}
LessonReaderComponent: صفحة مقفلة => WritingCorrectionAgent => فك القفل >= 70%
  => آخر صفحة => ExamAgent => النتيجة

---

## 13. قاعدة البيانات

Server: db55967.public.databaseasp.net | Database: db55967

الجداول الرئيسية:
- Users, Parents, Teachers, Students
- Stories, StoryPages
- Lessons, LessonPages
- Exams, Questions, StudentAnswers, StudentProgress
- WritingAttempts, LessonAssignments
- StudentGroups, StudentGroupMembers (PK: GroupId+StudentId)
- Classrooms, ClassroomStudents (PK: ClassroomId+StudentId) — آخر migration
- PlacementQuestions
- LevelWordConfigs (PK: Level)
- KnowledgeDocuments, RagPageChunks
- PdfDocuments, PdfPages

Migrations:
- (initial) — كل الجداول الأصلية
- 20260616082112_AddClassrooms — Classrooms + ClassroomStudents

---

## 14. مصفوفة ما لدينا وما ينقص

### مكتمل

| الميزة | Backend | Frontend |
|-------|---------|---------|
| تسجيل البالغين | نعم | نعم |
| دخول الطلاب بـ Image PINs | نعم | نعم |
| اختبار التحديد 15 سؤال 3 مستويات | نعم | نعم |
| توليد القصص AI | نعم | نعم |
| قراءة القصص مع TTS | — | نعم |
| اختبار القصص 4 أنواع | نعم | نعم |
| نتيجة الاختبار quiz-result | نعم | نعم |
| الكتابة اليدوية Canvas + OCR | نعم | نعم |
| توليد الدروس AI RAG + Gemini | نعم | نعم |
| قراءة الدروس مع نظام القفل | نعم | نعم |
| مكتبة PDFs التعليمية | نعم | نعم |
| لوحة الطالب (إحصائيات كاملة) | نعم | نعم |
| لوحة ولي الأمر | نعم | نعم |
| لوحة المعلم | نعم | نعم |
| لوحة المدرسة | نعم | نعم |
| إدارة المجموعات للمعلم الخاص | نعم | نعم |
| إدارة الفصول الدراسية | نعم | نعم |
| تعيين دروس للطلاب والمجموعات | نعم | نعم |
| تقارير المدرسة per-classroom | نعم | نعم |
| صفحة الاشتراك (عرض) | — | نعم |
| RAG + Chroma Vector DB | نعم | نعم |
| مسارات المدير | نعم | نعم |
| JWT Auth + Guards | نعم | نعم |

### يحتاج تحسين

| الميزة | المشكلة |
|-------|---------|
| صفحة الاشتراك | عرض ثابت — لا backend فعلي لنظام الدفع |
| نظام الإنجازات | صفحة موجودة بلا منطق حقيقي |
| صفحة الإعدادات | محتوى غير محدد |
| صفحة الترقية | موجودة بلا وظيفة |
| تقارير المعلم | dashboard data فقط لا تقارير مفصّلة |
| إشعارات ولي الأمر | صفحة موجودة بلا نظام إشعارات |

### ناقص — لم يُبنَ بعد

| الميزة | الأهمية |
|-------|--------|
| نظام اشتراكات حقيقي (دفع، خطط، تجديد) | عالية — يحتاج Stripe أو Moyasar |
| إشعارات push/email/SMS | عالية |
| تتبع نشاط الطالب realtime | متوسطة — SignalR |
| نظام الشارات والإنجازات الحقيقي | متوسطة |
| تصدير تقارير PDF | متوسطة |
| بحث عام في القصص والدروس | متوسطة |
| تخصيص Avatar للطالب | منخفضة |
| ألعاب تعليمية gamification | منخفضة |
| تطبيق جوال PWA أو Native | متوسطة |
| لوحة تحليلات متقدمة للمدير | متوسطة |
| نظام مراسلة معلم ولي أمر | متوسطة |
| مكتبة قصص مشتركة | متوسطة |

---

## أسئلة استراتيجية للتفكير مع Gemini

1. نموذج العمل: B2C (أولياء أمور) أم B2B (مدارس) أم كليهما Freemium؟
2. نظام الاشتراك: Stripe أم Moyasar للسوق السعودي؟ ما الخطط المناسبة؟
3. دقة OCR: هل 70% threshold مناسب للكتابة العربية للأطفال؟ كيف نحسّنه؟
4. توصيات AI: كيف نبني توصيات ذكية للقصص والدروس بناءً على أداء الطالب؟
5. الإشعارات: Email vs Push vs SMS — أيها أنسب للسوق العربي؟
6. التطبيق الجوال: PWA أم React Native أم Flutter لأطفال 5-10 سنوات؟
7. تحسين OCR العربي: هل نبني نموذج مخصص للخط العربي للأطفال؟
8. المحتوى: كم عدد القصص والدروس الكافي للإطلاق؟ كيف نبني مكتبة أولية؟
