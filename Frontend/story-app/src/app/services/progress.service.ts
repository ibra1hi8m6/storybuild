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

  getProgress(storyId: string, childName: string): Observable<ProgressResponse> {
    return this.http.get<ProgressResponse>(`${this.api}/api/progress/${storyId}/${childName}`);
  }

  updateProgress(progress: ProgressResponse): Observable<ProgressResponse> {
    return this.http.put<ProgressResponse>(`${this.api}/api/progress`, progress);
  }

  updateLessonProgress(req: {
    lessonId: string; childName: string;
    totalQuestions: number; correctAnswers: number;
    scorePercentage: number; examCompleted: boolean;
  }): Observable<any> {
    return this.http.put<any>(`${this.api}/api/progress/lesson`, req);
  }

  markPageDone(childName: string, lessonId: string, lessonPageId: string, writingSubmitted: boolean): Observable<void> {
    return this.http.post<void>(`${this.api}/api/progress/page`, {
      childName, lessonId, lessonPageId, writingSubmitted
    });
  }

  getLessonPageProgress(lessonId: string, childName: string): Observable<{ completedPageIds: string[]; completedCount: number; totalPages: number }> {
    return this.http.get<any>(`${this.api}/api/progress/lesson/${lessonId}/${childName}`);
  }

  getCurrentLesson(childName: string): Observable<{ lessonId: string | null; lessonTitle: string | null; currentPage: number; totalPages: number; level: number }> {
    return this.http.get<any>(`${this.api}/api/progress/current/${childName}`);
  }

  getWeaknessMap(childName: string): Observable<WeaknessMap> {
    return this.http.get<WeaknessMap>(`${this.api}/api/progress/weakness/${childName}`);
  }
}
