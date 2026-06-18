import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface FluencyReportDto {
  reportId: string;
  recordingId: string;
  audioFileUrl: string;
  wcpm: number;
  accuracyScore: number;
  expectedText: string;
  extractedText: string;
  mispronouncedWords: string[];
  passed: boolean;
  createdAt: string;
}

export interface FluencyHistoryDto {
  recordingId: string;
  pageId: string;
  pageType: string;
  audioFileUrl: string;
  durationSeconds: number;
  wcpm: number;
  accuracyScore: number;
  passed: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class FluencyApiService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/api/fluency`;

  async evaluate(params: {
    pageId: string;
    pageType: string;
    expectedText: string;
    audioBlob: Blob;
  }): Promise<FluencyReportDto> {
    const fd = new FormData();
    fd.append('pageId', params.pageId);
    fd.append('pageType', params.pageType);
    fd.append('expectedText', params.expectedText);
    fd.append('audio', params.audioBlob, 'recording.webm');
    return firstValueFrom(this.http.post<FluencyReportDto>(`${this.base}/evaluate`, fd));
  }

  getStudentHistory(studentId: string): Promise<FluencyHistoryDto[]> {
    return firstValueFrom(this.http.get<FluencyHistoryDto[]>(`${this.base}/student/${studentId}`));
  }

  getPageHistory(pageId: string, studentId: string): Promise<FluencyHistoryDto[]> {
    return firstValueFrom(
      this.http.get<FluencyHistoryDto[]>(`${this.base}/page/${pageId}/student/${studentId}`)
    );
  }
}
