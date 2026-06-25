import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProgressResponse } from '../models/progress.models';
import { WeaknessMap } from '../models/analytics.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ProgressService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  getProgress(storyId: string, studentId: string): Observable<ProgressResponse> {
    return this.http.get<ProgressResponse>(`${this.api}/api/progress/${storyId}/${studentId}`);
  }

  updateProgress(progress: ProgressResponse): Observable<ProgressResponse> {
    return this.http.put<ProgressResponse>(`${this.api}/api/progress`, progress);
  }

  updateLessonProgress(req: {
    lessonId: string; studentId: string;
    totalQuestions: number; correctAnswers: number;
    scorePercentage: number; examCompleted: boolean;
  }): Observable<any> {
    return this.http.put<any>(`${this.api}/api/progress/lesson`, req);
  }

  markPageDone(studentId: string, lessonId: string, lessonPageId: string, writingSubmitted: boolean): Observable<void> {
    return this.http.post<void>(`${this.api}/api/progress/page`, {
      studentId, lessonId, lessonPageId, writingSubmitted
    });
  }

  getLessonPageProgress(lessonId: string, studentId: string): Observable<{ completedPageIds: string[]; completedCount: number; totalPages: number }> {
    return this.http.get<any>(`${this.api}/api/progress/lesson/${lessonId}/${studentId}`);
  }

  getCurrentLesson(studentId: string): Observable<{ lessonId: string | null; lessonTitle: string | null; currentPage: number; totalPages: number; level: number }> {
    return this.http.get<any>(`${this.api}/api/progress/current/${studentId}`);
  }

  getWeaknessMap(studentId: string): Observable<WeaknessMap> {
    return this.http.get<WeaknessMap>(`${this.api}/api/progress/weakness/${studentId}`);
  }
}
