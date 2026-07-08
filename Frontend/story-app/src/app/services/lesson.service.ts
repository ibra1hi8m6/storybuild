import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { LessonSummary, LessonDetail } from '../models/lesson.models';
import { GenerateLessonV2Request } from '../models/rag.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class LessonService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  getLessonsByLevel(level: number): Observable<LessonSummary[]> {
    return this.http.get<LessonSummary[]>(`${this.api}/api/lessons?level=${level}`);
  }

  getLessonsCatalog(level: number): Observable<LessonSummary[]> {
    return this.http.get<LessonSummary[]>(`${this.api}/api/lessons/catalog?level=${level}`);
  }

  getLesson(id: string): Observable<LessonDetail> {
    return this.http.get<any>(`${this.api}/api/lessons/${id}`).pipe(
      map(l => ({
        ...l,
        pages: (l.pages ?? []).map((p: any) => ({
          ...p,
          pageId:   p.pageId   ?? p.id,
          imageUrl: p.imageUrl ?? p.imagePath ?? ''
        }))
      }))
    );
  }

  deleteLesson(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/lessons/${id}`);
  }

  createManualLesson(req: {
    title: string; level: number; letter: string;
    creatorId?: string; pages: { content: string; type: string }[];
  }): Observable<any> {
    return this.http.post<any>(`${this.api}/api/lessons/manual`, req);
  }

  generateLesson(req: GenerateLessonV2Request): Observable<LessonDetail> {
    return this.http.post<LessonDetail>(`${this.api}/api/lessons/generate`, req);
  }

  getMyLessons(creatorId: string): Observable<LessonSummary[]> {
    return this.http.get<LessonSummary[]>(`${this.api}/api/lessons/my/${creatorId}`);
  }
}
