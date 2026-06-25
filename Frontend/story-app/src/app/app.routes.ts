import { Routes } from '@angular/router';
import { authGuard, studentGuard, parentGuard, teacherGuard, schoolGuard, adminGuard } from './core/auth.guard';

export const routes: Routes = [
  // ── Public ──────────────────────────────────────────────────────────────────
  {
    path: '',
    loadComponent: () =>
      import('./features/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'auth/login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'auth/register',
    loadComponent: () =>
      import('./features/auth/register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'auth/create-student',
    loadComponent: () =>
      import('./features/auth/create-student/create-student.component').then(m => m.CreateStudentComponent),
    canActivate: [authGuard]
  },

  // ── Placement Test ───────────────────────────────────────────────────────────
  {
    path: 'test',
    loadComponent: () =>
      import('./features/placement/welcome/placement-welcome.component')
        .then(m => m.PlacementWelcomeComponent)
  },
  {
    path: 'test/question',
    loadComponent: () =>
      import('./features/placement/question/placement-question.component')
        .then(m => m.PlacementQuestionComponent)
  },
  {
    path: 'test/result',
    loadComponent: () =>
      import('./features/placement/result/placement-result.component')
        .then(m => m.PlacementResultComponent)
  },

  // ── Student ──────────────────────────────────────────────────────────────────
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboards/student-dashboard/student-dashboard.component')
        .then(m => m.StudentDashboardComponent),
    canActivate: [authGuard]
  },
  // ── محتوى التعلم hub ──────────────────────────────────────────────────────
  {
    path: 'learning',
    loadComponent: () =>
      import('./features/levels/levels.component').then(m => m.LevelsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'learning/letters',
    loadComponent: () =>
      import('./features/learning/letters/letters.component').then(m => m.LettersComponent),
    canActivate: [authGuard]
  },
  {
    path: 'learning/letters/recognition',
    loadComponent: () =>
      import('./features/learning/letters/letter-recognition.component').then(m => m.LetterRecognitionComponent),
    canActivate: [authGuard]
  },
  {
    path: 'learning/letters/:id',
    loadComponent: () =>
      import('./features/learning/letters/letter-lesson.component').then(m => m.LetterLessonComponent),
    canActivate: [authGuard]
  },
  {
    path: 'learning/words-sentences',
    loadComponent: () =>
      import('./features/learning/words/words-sentences-hub.component').then(m => m.WordsSentencesHubComponent),
    canActivate: [authGuard]
  },
  {
    path: 'learning/words',
    loadComponent: () =>
      import('./features/learning/words/words.component').then(m => m.WordsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'learning/words/:id',
    loadComponent: () =>
      import('./features/learning/words/word-practice.component').then(m => m.WordPracticeComponent),
    canActivate: [authGuard]
  },
  {
    path: 'learning/sentences',
    loadComponent: () =>
      import('./features/learning/sentences/sentences.component').then(m => m.SentencesComponent),
    canActivate: [authGuard]
  },
  {
    path: 'learning/sentences/:id',
    loadComponent: () =>
      import('./features/learning/sentences/sentence-practice.component').then(m => m.SentencePracticeComponent),
    canActivate: [authGuard]
  },
  {
    path: 'learning/booklets-stories',
    loadComponent: () =>
      import('./features/learning/booklets-stories/booklets-stories-hub.component').then(m => m.BookletsStoriesHubComponent),
    canActivate: [authGuard]
  },
  // keep /levels so existing bookmarks still work
  {
    path: 'levels',
    loadComponent: () =>
      import('./features/levels/levels.component').then(m => m.LevelsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'levels/:id/books',
    loadComponent: () =>
      import('./features/books/books.component').then(m => m.BooksComponent),
    canActivate: [authGuard]
  },
  {
    path: 'books/:id/read',
    loadComponent: () =>
      import('./features/story-reader/story-reader').then(m => m.StoryReaderComponent),
    canActivate: [authGuard]
  },
  {
    path: 'writing-practice',
    loadComponent: () =>
      import('./features/writing-practice/writing-practice').then(m => m.WritingPracticeComponent),
    canActivate: [authGuard]
  },

  // ── Reading Journey (Module A) ───────────────────────────────────────────────
  {
    path: 'books/:id/journey',
    loadComponent: () =>
      import('./features/fluency/reading-journey/reading-journey-host.component')
        .then(m => m.ReadingJourneyHostComponent),
    canActivate: [authGuard],
    data: { pageType: 'Story' }
  },
  {
    path: 'lessons/:id/journey',
    loadComponent: () =>
      import('./features/fluency/reading-journey/reading-journey-host.component')
        .then(m => m.ReadingJourneyHostComponent),
    canActivate: [authGuard],
    data: { pageType: 'Lesson' }
  },

  // ── Student sub-pages ────────────────────────────────────────────────────────
  {
    path: 'progress',
    loadComponent: () =>
      import('./features/student/progress/progress.component').then(m => m.ProgressComponent),
    canActivate: [authGuard]
  },
  {
    path: 'my-stories',
    loadComponent: () =>
      import('./features/student/my-stories/my-stories.component').then(m => m.MyStoriesComponent),
    canActivate: [authGuard]
  },
  {
    path: 'achievements',
    loadComponent: () =>
      import('./features/student/achievements/achievements.component').then(m => m.AchievementsComponent),
    canActivate: [authGuard]
  },

  // ── Student lesson views ─────────────────────────────────────────────────────
  {
    path: 'generate-lesson',
    loadComponent: () =>
      import('./features/student/generate-lesson/generate-lesson')
        .then(m => m.GenerateLessonComponent),
    canActivate: [authGuard]
  },
  {
    path: 'my-lessons',
    loadComponent: () =>
      import('./features/student/my-lessons/my-lessons')
        .then(m => m.MyLessonsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'assigned-lessons',
    loadComponent: () =>
      import('./features/student/assigned-lessons/assigned-lessons')
        .then(m => m.AssignedLessonsComponent),
    canActivate: [authGuard]
  },

  // ── Quiz Result ──────────────────────────────────────────────────────────────
  {
    path: 'books/:id/quiz-result',
    loadComponent: () =>
      import('./features/quiz-result/quiz-result.component').then(m => m.QuizResultComponent),
    canActivate: [authGuard]
  },

  // ── Story Generator ──────────────────────────────────────────────────────────
  {
    path: 'generate-story',
    loadComponent: () =>
      import('./features/story-generator/story-generator').then(m => m.StoryGeneratorComponent),
    canActivate: [authGuard]
  },

  // ── AI Story Wizard ──────────────────────────────────────────────────────────
  {
    path: 'ai-story',
    loadComponent: () =>
      import('./features/ai-story-wizard/ai-story-wizard.component')
        .then(m => m.AiStoryWizardComponent)
  },

  // ── Lessons ──────────────────────────────────────────────────────────────────
  {
    path: 'lessons/:id',
    loadComponent: () =>
      import('./features/lesson-reader/lesson-reader').then(m => m.LessonReaderComponent)
  },
  {
    path: 'lessons/:id/complete',
    loadComponent: () =>
      import('./features/lesson-complete/lesson-complete.component').then(m => m.LessonCompleteComponent)
  },

  // ── Parent ───────────────────────────────────────────────────────────────────
  {
    path: 'parent/dashboard',
    loadComponent: () =>
      import('./features/dashboards/parent-dashboard/parent-dashboard.component')
        .then(m => m.ParentDashboardComponent),
    canActivate: [parentGuard]
  },
  {
    path: 'parent/children',
    loadComponent: () =>
      import('./features/parent/children/parent-children.component').then(m => m.ParentChildrenComponent),
    canActivate: [parentGuard]
  },
  {
    path: 'parent/notifications',
    loadComponent: () =>
      import('./features/parent/notifications/parent-notifications.component').then(m => m.ParentNotificationsComponent),
    canActivate: [parentGuard]
  },
  {
    path: 'parent/child/:studentId/progress',
    loadComponent: () =>
      import('./features/parent/child-progress/child-progress.component').then(m => m.ChildProgressComponent),
    canActivate: [parentGuard]
  },

  // ── Teacher ──────────────────────────────────────────────────────────────────
  {
    path: 'teacher/students',
    loadComponent: () =>
      import('./features/dashboards/teacher-dashboard/teacher-dashboard.component')
        .then(m => m.TeacherDashboardComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/ai-generator',
    loadComponent: () =>
      import('./features/teacher/ai-generator/ai-generator.component')
        .then(m => m.AiGeneratorComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/lessons/create',
    loadComponent: () =>
      import('./features/teacher/lesson-create/lesson-create.component')
        .then(m => m.LessonCreateComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/reports',
    loadComponent: () =>
      import('./features/teacher/reports/teacher-reports.component')
        .then(m => m.TeacherReportsComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/lessons',
    loadComponent: () =>
      import('./features/teacher/lessons/teacher-lessons.component').then(m => m.TeacherLessonsComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/students/:studentId',
    loadComponent: () =>
      import('./features/teacher/student-detail/student-detail.component').then(m => m.StudentDetailComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/groups',
    loadComponent: () =>
      import('./features/teacher/groups/teacher-groups')
        .then(m => m.TeacherGroupsComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/classes',
    loadComponent: () =>
      import('./features/teacher/teacher-classes/teacher-classes.component')
        .then(m => m.TeacherClassesComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'teacher/analytics',
    loadComponent: () =>
      import('./features/teacher/analytics/teacher-analytics.component')
        .then(m => m.TeacherAnalyticsComponent),
    canActivate: [teacherGuard]
  },

  // ── School ───────────────────────────────────────────────────────────────────
  {
    path: 'school/dashboard',
    loadComponent: () =>
      import('./features/dashboards/school-dashboard/school-dashboard.component')
        .then(m => m.SchoolDashboardComponent),
    canActivate: [schoolGuard]
  },
  {
    path: 'school/teachers',
    loadComponent: () =>
      import('./features/school/teachers/school-teachers.component').then(m => m.SchoolTeachersComponent),
    canActivate: [schoolGuard]
  },
  {
    path: 'school/classrooms',
    loadComponent: () =>
      import('./features/school/classrooms/school-classrooms.component').then(m => m.SchoolClassroomsComponent),
    canActivate: [schoolGuard]
  },
  {
    path: 'school/reports',
    loadComponent: () =>
      import('./features/school/reports/school-reports.component').then(m => m.SchoolReportsComponent),
    canActivate: [schoolGuard]
  },
  {
    path: 'school/subscription',
    loadComponent: () =>
      import('./features/school/subscription/school-subscription.component').then(m => m.SchoolSubscriptionComponent),
    canActivate: [schoolGuard]
  },

  // ── Exam ──────────────────────────────────────────────────────────────────────
  {
    path: 'exam',
    loadComponent: () =>
      import('./features/exam/exam').then(m => m.Exam),
    canActivate: [authGuard]
  },

  // ── Lessons list (letter-books) ────────────────────────────────────────────
  {
    path: 'lessons-list',
    loadComponent: () =>
      import('./features/lessons-list/lessons-list')
        .then(m => m.LessonsListComponent),
    canActivate: [authGuard]
  },

  // ── Uploaded PDF Stories (student) ───────────────────────────────────────────
  {
    path: 'uploaded-stories',
    loadComponent: () =>
      import('./features/uploaded-stories/uploaded-stories.component').then(m => m.UploadedStoriesComponent),
    canActivate: [authGuard]
  },
  {
    path: 'uploaded-stories/:id/journey',
    loadComponent: () =>
      import('./features/uploaded-stories/uploaded-story-journey.component').then(m => m.UploadedStoryJourneyComponent),
    canActivate: [authGuard]
  },

  // ── Admin ─────────────────────────────────────────────────────────────────────
  {
    path: 'admin/rag',
    loadComponent: () =>
      import('./features/admin-rag/admin-rag.component')
        .then(m => m.AdminRagComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/pdf-library',
    loadComponent: () =>
      import('./features/admin-pdf-library/admin-pdf-library.component')
        .then(m => m.AdminPdfLibraryComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/content',
    loadComponent: () =>
      import('./features/admin/content/admin-content.component')
        .then(m => m.AdminContentComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/books',
    loadComponent: () =>
      import('./features/admin-import/admin-import')
        .then(m => m.AdminImportComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/ai-settings',
    loadComponent: () =>
      import('./features/admin/ai-settings/ai-settings.component')
        .then(m => m.AiSettingsComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/subscriptions',
    loadComponent: () =>
      import('./features/admin/subscriptions/subscriptions.component')
        .then(m => m.SubscriptionsComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/users',
    loadComponent: () =>
      import('./features/admin/users/admin-users.component')
        .then(m => m.AdminUsersComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/schools',
    loadComponent: () =>
      import('./features/admin/schools/admin-schools.component')
        .then(m => m.AdminSchoolsComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/stories',
    loadComponent: () =>
      import('./features/admin/stories/admin-stories.component')
        .then(m => m.AdminStoriesComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/rag-chunks',
    loadComponent: () =>
      import('./features/admin-rag-chunks/admin-rag-chunks')
        .then(m => m.AdminRagChunksComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/uploaded-stories',
    loadComponent: () =>
      import('./features/admin-uploaded-stories/admin-uploaded-stories.component')
        .then(m => m.AdminUploadedStoriesComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/learning',
    loadComponent: () =>
      import('./features/admin/learning/admin-learning.component')
        .then(m => m.AdminLearningComponent),
    canActivate: [adminGuard]
  },

  // ── Module B: eTools ─────────────────────────────────────────────────────────
  {
    path: 'my-journal',
    loadComponent: () =>
      import('./features/etools/word-journal/word-journal.component').then(m => m.WordJournalComponent),
    canActivate: [authGuard]
  },

  // ── Module D: Messaging ───────────────────────────────────────────────────────
  {
    path: 'inbox',
    loadComponent: () =>
      import('./features/messaging/student-inbox/student-inbox.component').then(m => m.StudentInboxComponent),
    canActivate: [authGuard]
  },
  {
    path: 'teacher/feedback',
    loadComponent: () =>
      import('./features/messaging/teacher-feedback/teacher-feedback.component').then(m => m.TeacherFeedbackComponent),
    canActivate: [teacherGuard]
  },
  {
    path: 'parent/child/:id/recordings',
    loadComponent: () =>
      import('./features/messaging/parent-recordings/parent-recordings.component').then(m => m.ParentRecordingsComponent),
    canActivate: [parentGuard]
  },

  // ── Module C: Mini-Games ──────────────────────────────────────────────────────
  {
    path: 'mini-games',
    loadComponent: () =>
      import('./features/mini-games/mini-games-host/mini-games-host.component').then(m => m.MiniGamesHostComponent),
    canActivate: [authGuard]
  },

  // ── Utility ───────────────────────────────────────────────────────────────────
  {
    path: 'settings',
    loadComponent: () =>
      import('./features/settings/settings.component').then(m => m.SettingsComponent)
  },
  {
    path: 'upgrade',
    loadComponent: () =>
      import('./features/upgrade/upgrade.component').then(m => m.UpgradeComponent)
  },

  // ── 404 (must be last) ────────────────────────────────────────────────────────
  {
    path: '**',
    loadComponent: () =>
      import('./features/not-found/not-found.component').then(m => m.NotFoundComponent)
  },
];
