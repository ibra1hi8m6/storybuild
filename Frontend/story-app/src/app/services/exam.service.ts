import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError } from 'rxjs';
import { ExamResponse, SubmitExamRequest, ExamResult } from '../models/exam.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ExamService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  generateExam(storyId: string): Observable<ExamResponse> {
    return this.http.post<ExamResponse>(`${this.api}/api/exam/generate/${storyId}`, {});
  }

  generateLessonExam(lessonId: string): Observable<ExamResponse> {
    return this.http.post<ExamResponse>(`${this.api}/api/exam/generate/lesson/${lessonId}`, {});
  }

  getOrGenerateExam(storyId: string): Observable<ExamResponse> {
    return this.http.get<ExamResponse>(`${this.api}/api/exam/story/${storyId}`).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 404)
          return this.http.post<ExamResponse>(`${this.api}/api/exam/generate/${storyId}`, {});
        throw err;
      })
    );
  }

  submitExam(req: SubmitExamRequest): Observable<ExamResult> {
    return this.http.post<ExamResult>(`${this.api}/api/exam/submit`, req);
  }
}
