import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WeakLetterDto, AnalyticsSummaryDto } from '../models/analytics.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  getStudentWeakLetters(studentId: string): Observable<WeakLetterDto[]> {
    return this.http.get<WeakLetterDto[]>(
      `${this.api}/api/analytics/student/${studentId}/weak-letters`);
  }

  getClassAnalytics(teacherId: string): Observable<AnalyticsSummaryDto> {
    return this.http.get<AnalyticsSummaryDto>(
      `${this.api}/api/analytics/teacher/${teacherId}/class`);
  }

  recordActivity(body: {
    studentId: string; childName: string;
    letter: string; correct: boolean; activityType: string;
  }): Observable<void> {
    return this.http.post<void>(`${this.api}/api/analytics/record`, body);
  }
}
