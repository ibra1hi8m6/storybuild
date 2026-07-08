// Facade — delegates to focused feature services.
// All existing components that inject StoryService continue to work unchanged.

import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { StoryContentService }  from './story-content.service';
import { ExamService }          from './exam.service';
import { WritingService }       from './writing.service';
import { LessonService }        from './lesson.service';
import { AdminService }         from './admin.service';
import { ProgressService }      from './progress.service';
import { RagService }           from './rag.service';
import { DashboardService }     from './dashboard.service';
import { PdfLibraryService }    from './pdf-library.service';
import { GroupsService }        from './groups.service';
import { AssignmentsService }   from './assignments.service';
import { AnalyticsService }     from './analytics.service';

import {
  GenerateStoryRequest, StoryResponse, UploadedStoryDto,
  ExamResponse, SubmitExamRequest, ExamResult,
  WritingCorrectionResponse, WritingAttemptHistory, ReadingAttemptHistory,
  LessonSummary, LessonDetail, ImportBookResponse, AdminBooksPageDto, CreateManualBookRequest,
  ProgressResponse, WeaknessMap,
  KnowledgeDocumentDto, RagSearchResult, GenerateLessonRequest,
  IngestDocumentResponse, RagPageChunkDto, GenerateLessonV2Request,
  StudentDashboardDto, ParentDashboardDto, TeacherDashboardDto, SchoolDashboardDto, LevelProgressDto,
  PdfDocumentDto, PdfDocumentDetailDto, EmbedResultDto, PdfLibraryStatsDto,
  StudentGroupDto, AssignLessonRequest, LessonAssignmentDto,
  AssignmentDto, AssignmentSubmissionDto, TeacherAssignmentOverview,
  WeakLetterDto, AnalyticsSummaryDto
} from '../models/story.models';

@Injectable({ providedIn: 'root' })
export class StoryService {
  private readonly stories     = inject(StoryContentService);
  private readonly exams       = inject(ExamService);
  private readonly writing     = inject(WritingService);
  private readonly lessons     = inject(LessonService);
  private readonly admin       = inject(AdminService);
  private readonly progress    = inject(ProgressService);
  private readonly rag         = inject(RagService);
  private readonly dashboard   = inject(DashboardService);
  private readonly pdfLibrary  = inject(PdfLibraryService);
  private readonly groups      = inject(GroupsService);
  private readonly assignments = inject(AssignmentsService);
  private readonly analytics   = inject(AnalyticsService);

  // ── Story ──────────────────────────────────────────────────────────────────
  generateStory(req: GenerateStoryRequest): Observable<StoryResponse> { return this.stories.generateStory(req); }
  getStory(id: string): Observable<StoryResponse>                     { return this.stories.getStory(id); }
  getAllStories(): Observable<StoryResponse[]>                        { return this.stories.getAllStories(); }
  getMyStories(studentId: string): Observable<StoryResponse[]>       { return this.stories.getMyStories(studentId); }
  deleteStory(id: string): Observable<void>                          { return this.stories.deleteStory(id); }
  uploadStoryPdf(title: string, file: File): Observable<UploadedStoryDto>   { return this.stories.uploadStoryPdf(title, file); }
  getUploadedStories(): Observable<UploadedStoryDto[]>               { return this.stories.getUploadedStories(); }
  getUploadedStoriesCatalog(): Observable<UploadedStoryDto[]>        { return this.stories.getUploadedStoriesCatalog(); }
  getUploadedStory(id: string): Observable<UploadedStoryDto>         { return this.stories.getUploadedStory(id); }
  deleteUploadedStory(id: string): Observable<void>                  { return this.stories.deleteUploadedStory(id); }

  // ── Exam ───────────────────────────────────────────────────────────────────
  generateExam(storyId: string): Observable<ExamResponse>            { return this.exams.generateExam(storyId); }
  generateLessonExam(lessonId: string): Observable<ExamResponse>     { return this.exams.generateLessonExam(lessonId); }
  getOrGenerateExam(storyId: string): Observable<ExamResponse>       { return this.exams.getOrGenerateExam(storyId); }
  submitExam(req: SubmitExamRequest): Observable<ExamResult>         { return this.exams.submitExam(req); }

  // ── Writing ────────────────────────────────────────────────────────────────
  submitLessonWriting(lessonId: string, lessonPageId: string, studentId: string, childName: string, imageBlob: Blob, fileName = 'drawing.png'): Observable<WritingCorrectionResponse> {
    return this.writing.submitLessonWriting(lessonId, lessonPageId, studentId, childName, imageBlob, fileName);
  }
  evaluateCanvasWriting(imageBase64: string, expectedText: string): Observable<WritingCorrectionResponse> {
    return this.writing.evaluateCanvasWriting(imageBase64, expectedText);
  }
  getWritingHistory(studentId: string, take = 30): Observable<WritingAttemptHistory[]>  { return this.writing.getWritingHistory(studentId, take); }
  getReadingHistory(studentId: string, take = 30): Observable<ReadingAttemptHistory[]>  { return this.writing.getReadingHistory(studentId, take); }

  // ── Lessons ────────────────────────────────────────────────────────────────
  getLessonsByLevel(level: number): Observable<LessonSummary[]>      { return this.lessons.getLessonsByLevel(level); }
  getLessonsCatalog(level: number): Observable<LessonSummary[]>      { return this.lessons.getLessonsCatalog(level); }
  getLesson(id: string): Observable<LessonDetail>                    { return this.lessons.getLesson(id); }
  deleteLesson(id: string): Observable<void>                         { return this.lessons.deleteLesson(id); }
  createManualLesson(req: any): Observable<any>                      { return this.lessons.createManualLesson(req); }
  generateLesson(req: GenerateLessonV2Request): Observable<LessonDetail> { return this.lessons.generateLesson(req); }
  getMyLessons(creatorId: string): Observable<LessonSummary[]>       { return this.lessons.getMyLessons(creatorId); }

  // ── Admin ──────────────────────────────────────────────────────────────────
  importBook(level: number, letter: string, letterName: string, pdfFile: File): Observable<ImportBookResponse>              { return this.admin.importBook(level, letter, letterName, pdfFile); }
  importBookV2(level: number, letter: string, letterName: string, title: string, pdfFile: File): Observable<ImportBookResponse> { return this.admin.importBookV2(level, letter, letterName, title, pdfFile); }
  getAllBooksAdmin(level?: number, page = 1, pageSize = 9): Observable<AdminBooksPageDto>                                    { return this.admin.getAllBooksAdmin(level, page, pageSize); }
  getBookDetailAdmin(id: string): Observable<LessonDetail>           { return this.admin.getBookDetailAdmin(id); }
  deleteBook(id: string): Observable<void>                           { return this.admin.deleteBook(id); }
  publishLesson(id: string): Observable<any>                         { return this.admin.publishLesson(id); }
  unpublishLesson(id: string): Observable<any>                       { return this.admin.unpublishLesson(id); }
  publishStory(id: string): Observable<any>                          { return this.admin.publishStory(id); }
  unpublishStory(id: string): Observable<any>                        { return this.admin.unpublishStory(id); }
  updateBookPageSentence(bookId: string, pageId: string, sentence: string): Observable<void> { return this.admin.updateBookPageSentence(bookId, pageId, sentence); }
  createManualBook(req: CreateManualBookRequest): Observable<ImportBookResponse>             { return this.admin.createManualBook(req); }
  getAiSettings(): Observable<any>                                   { return this.admin.getAiSettings(); }
  saveAiSettings(settings: any): Observable<any>                     { return this.admin.saveAiSettings(settings); }
  getSubscriptionStats(): Observable<any>                            { return this.admin.getSubscriptionStats(); }
  getAllUsers(): Observable<any[]>                                    { return this.admin.getAllUsers(); }
  blockUser(id: string): Observable<void>                            { return this.admin.blockUser(id); }
  unblockUser(id: string): Observable<void>                          { return this.admin.unblockUser(id); }
  getSchools(): Observable<any[]>                                    { return this.admin.getSchools(); }
  createSchool(body: { schoolName: string; adminEmail: string; adminPassword: string }): Observable<any> { return this.admin.createSchool(body); }

  // ── Progress ───────────────────────────────────────────────────────────────
  getProgress(storyId: string, studentId: string): Observable<ProgressResponse>            { return this.progress.getProgress(storyId, studentId); }
  updateProgress(p: ProgressResponse): Observable<ProgressResponse>                        { return this.progress.updateProgress(p); }
  updateLessonProgress(req: any): Observable<any>                                           { return this.progress.updateLessonProgress(req); }
  markPageDone(studentId: string, lessonId: string, lessonPageId: string, writingSubmitted: boolean): Observable<void> { return this.progress.markPageDone(studentId, lessonId, lessonPageId, writingSubmitted); }
  getLessonPageProgress(lessonId: string, studentId: string): Observable<any>              { return this.progress.getLessonPageProgress(lessonId, studentId); }
  getCurrentLesson(studentId: string): Observable<any>                                     { return this.progress.getCurrentLesson(studentId); }
  getWeaknessMap(studentId: string): Observable<WeaknessMap>                               { return this.progress.getWeaknessMap(studentId); }

  // ── RAG ────────────────────────────────────────────────────────────────────
  ingestDocument(file: File, letter?: string, level?: number, tags?: string): Observable<IngestDocumentResponse> { return this.rag.ingestDocument(file, letter, level, tags); }
  getKnowledgeDocuments(): Observable<KnowledgeDocumentDto[]>        { return this.rag.getKnowledgeDocuments(); }
  deleteKnowledgeDocument(id: string): Observable<void>              { return this.rag.deleteKnowledgeDocument(id); }
  ragSearch(query: string): Observable<RagSearchResult[]>            { return this.rag.ragSearch(query); }
  generateRagLesson(req: GenerateLessonRequest): Observable<LessonDetail> { return this.rag.generateRagLesson(req); }
  ingestEducationalPdf(file: File, level: number, letter: string, letterName: string): Observable<IngestDocumentResponse> { return this.rag.ingestEducationalPdf(file, level, letter, letterName); }
  getRagPageChunks(level?: number, letter?: string): Observable<RagPageChunkDto[]>         { return this.rag.getRagPageChunks(level, letter); }
  uploadKnowledgeDocument(file: File, name: string, description: string): Observable<any> { return this.rag.uploadKnowledgeDocument(file, name, description); }

  // ── Dashboards ────────────────────────────────────────────────────────────
  getStudentDashboard(studentId: string): Observable<StudentDashboardDto>     { return this.dashboard.getStudentDashboard(studentId); }
  getTeacherStudentView(studentId: string): Observable<StudentDashboardDto>  { return this.dashboard.getTeacherStudentView(studentId); }
  getParentDashboard(studentId: string): Observable<ParentDashboardDto>      { return this.dashboard.getParentDashboard(studentId); }
  getTeacherDashboard(): Observable<TeacherDashboardDto>                   { return this.dashboard.getTeacherDashboard(); }
  getSchoolDashboard(): Observable<SchoolDashboardDto>                     { return this.dashboard.getSchoolDashboard(); }
  getKnownStudentNames(): Observable<string[]>                             { return this.dashboard.getKnownStudentNames(); }
  getLevelProgress(studentId: string): Observable<LevelProgressDto[]>      { return this.dashboard.getLevelProgress(studentId); }
  requestPlacementRetake(): Observable<{ message: string }>                { return this.dashboard.requestPlacementRetake(); }
  updateStudentLevel(level: number): Observable<any>                       { return this.dashboard.updateStudentLevel(level); }
  getPlacementQuestions(): Observable<any[]>                               { return this.dashboard.getPlacementQuestions(); }
  submitPlacement(request: { answers: { questionId: string; answer: string }[] }): Observable<any> { return this.dashboard.submitPlacement(request); }
  getSchoolClassrooms(): Observable<any[]>                                 { return this.dashboard.getSchoolClassrooms(); }
  createClassroom(body: { name: string; level: number; teacherId: string }): Observable<any> { return this.dashboard.createClassroom(body); }
  addStudentToClassroom(classroomId: string, studentId: string): Observable<any>             { return this.dashboard.addStudentToClassroom(classroomId, studentId); }
  getMyTeacherClassrooms(): Observable<any[]>                              { return this.dashboard.getMyTeacherClassrooms(); }
  getClassroomDetail(id: string): Observable<any>                          { return this.dashboard.getClassroomDetail(id); }
  editClassroom(id: string, body: { name?: string; level?: number; teacherId?: string }): Observable<any> { return this.dashboard.editClassroom(id, body); }
  deleteClassroom(id: string): Observable<void>                            { return this.dashboard.deleteClassroom(id); }
  removeStudentFromClassroom(classroomId: string, studentId: string): Observable<void>       { return this.dashboard.removeStudentFromClassroom(classroomId, studentId); }
  searchSchoolStudents(q: string): Observable<any[]>                       { return this.dashboard.searchSchoolStudents(q); }
  getClassroomsReport(): Observable<any[]>                                 { return this.dashboard.getClassroomsReport(); }

  // ── PDF Library ───────────────────────────────────────────────────────────
  uploadPdfDocument(file: File, letter: string, level: number): Observable<PdfDocumentDto>  { return this.pdfLibrary.uploadPdfDocument(file, letter, level); }
  generatePdfEmbeddings(id: string): Observable<EmbedResultDto>            { return this.pdfLibrary.generatePdfEmbeddings(id); }
  getPdfDocuments(): Observable<PdfDocumentDto[]>                          { return this.pdfLibrary.getPdfDocuments(); }
  getPdfDocument(id: string): Observable<PdfDocumentDetailDto>             { return this.pdfLibrary.getPdfDocument(id); }
  deletePdfDocument(id: string): Observable<void>                          { return this.pdfLibrary.deletePdfDocument(id); }
  getPdfLibraryStats(): Observable<PdfLibraryStatsDto>                     { return this.pdfLibrary.getPdfLibraryStats(); }

  // ── Student Groups ────────────────────────────────────────────────────────
  getTeacherGroups(teacherId: string): Observable<StudentGroupDto[]>       { return this.groups.getTeacherGroups(teacherId); }
  createGroup(teacherId: string, name: string): Observable<StudentGroupDto> { return this.groups.createGroup(teacherId, name); }
  addGroupMember(groupId: string, studentId: string): Observable<void>     { return this.groups.addGroupMember(groupId, studentId); }
  removeGroupMember(groupId: string, studentId: string): Observable<void>  { return this.groups.removeGroupMember(groupId, studentId); }
  deleteGroup(groupId: string): Observable<void>                           { return this.groups.deleteGroup(groupId); }
  assignLesson(req: AssignLessonRequest): Observable<{ id: string; message: string }> { return this.groups.assignLesson(req); }
  getAssignedLessons(studentId: string): Observable<LessonAssignmentDto[]> { return this.groups.getAssignedLessons(studentId); }
  getTeacherAssignments(teacherId: string): Observable<LessonAssignmentDto[]> { return this.groups.getTeacherAssignments(teacherId); }
  getDirectStudents(teacherId: string): Observable<{ id: string; name: string; level: number }[]> { return this.groups.getDirectStudents(teacherId); }
  addDirectStudent(teacherId: string, identifier: string): Observable<{ id: string; name: string; level: number }> { return this.groups.addDirectStudent(teacherId, identifier); }
  removeDirectStudent(teacherId: string, studentId: string): Observable<void> { return this.groups.removeDirectStudent(teacherId, studentId); }

  // ── Assignments ────────────────────────────────────────────────────────────
  getStudentAssignments(studentId: string): Observable<AssignmentDto[]>    { return this.assignments.getStudentAssignments(studentId); }
  submitAssignment(assignmentId: string, body: any): Observable<AssignmentSubmissionDto> { return this.assignments.submitAssignment(assignmentId, body); }
  getAssignmentSubmissions(assignmentId: string): Observable<AssignmentSubmissionDto[]>  { return this.assignments.getAssignmentSubmissions(assignmentId); }
  getTeacherAssignmentOverview(teacherId: string): Observable<TeacherAssignmentOverview[]> { return this.assignments.getTeacherAssignmentOverview(teacherId); }

  // ── Analytics ──────────────────────────────────────────────────────────────
  getStudentWeakLetters(studentId: string): Observable<WeakLetterDto[]>    { return this.analytics.getStudentWeakLetters(studentId); }
  getClassAnalytics(teacherId: string): Observable<AnalyticsSummaryDto>    { return this.analytics.getClassAnalytics(teacherId); }
  recordActivity(body: { studentId: string; childName: string; letter: string; correct: boolean; activityType: string; }): Observable<void> { return this.analytics.recordActivity(body); }
}
