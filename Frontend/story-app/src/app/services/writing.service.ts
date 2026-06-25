import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WritingCorrectionResponse, WritingAttemptHistory, ReadingAttemptHistory } from '../models/writing.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class WritingService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  submitLessonWriting(
    lessonId:     string,
    lessonPageId: string,
    studentId:    string,
    childName:    string,
    imageBlob:    Blob,
    fileName:     string = 'drawing.png'
  ): Observable<WritingCorrectionResponse> {
    const form = new FormData();
    form.append('lessonId',     lessonId);
    form.append('lessonPageId', lessonPageId);
    form.append('studentId',    studentId);
    form.append('childName',    childName);
    form.append('image',        imageBlob, fileName);
    return this.http.post<WritingCorrectionResponse>(`${this.api}/api/writing/evaluate`, form);
  }

  evaluateCanvasWriting(imageBase64: string, expectedText: string): Observable<WritingCorrectionResponse> {
    return this.http.post<WritingCorrectionResponse>(`${this.api}/api/writing/canvas`, {
      imageBase64,
      expectedText
    });
  }

  getWritingHistory(studentId: string, take = 30): Observable<WritingAttemptHistory[]> {
    return this.http.get<WritingAttemptHistory[]>(`${this.api}/api/writing/history/${studentId}?take=${take}`);
  }

  getReadingHistory(studentId: string, take = 30): Observable<ReadingAttemptHistory[]> {
    return this.http.get<ReadingAttemptHistory[]>(`${this.api}/api/fluency/history/${studentId}?take=${take}`);
  }
}
