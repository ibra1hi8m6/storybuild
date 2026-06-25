import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { GenerateStoryRequest, StoryResponse, UploadedStoryDto } from '../models/story-content.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class StoryContentService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  generateStory(req: GenerateStoryRequest): Observable<StoryResponse> {
    return this.http.post<StoryResponse>(`${this.api}/api/story/generate`, req);
  }

  getStory(id: string): Observable<StoryResponse> {
    return this.http.get<any>(`${this.api}/api/story/${id}`).pipe(
      map(s => ({
        ...s,
        pages: (s.pages ?? []).map((p: any) => ({
          ...p,
          pageId:   p.pageId   ?? p.id,
          imageUrl: p.imageUrl ?? p.imagePath ?? ''
        }))
      }))
    );
  }

  getAllStories(): Observable<StoryResponse[]> {
    return this.http.get<StoryResponse[]>(`${this.api}/api/story`);
  }

  getMyStories(studentId: string): Observable<StoryResponse[]> {
    return this.http.get<StoryResponse[]>(`${this.api}/api/story/mine/${studentId}`);
  }

  deleteStory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/story/${id}`);
  }

  uploadStoryPdf(title: string, file: File): Observable<UploadedStoryDto> {
    const fd = new FormData();
    fd.append('title', title);
    fd.append('pdfFile', file);
    return this.http.post<UploadedStoryDto>(`${this.api}/api/admin/uploaded-stories`, fd);
  }

  getUploadedStories(): Observable<UploadedStoryDto[]> {
    return this.http.get<UploadedStoryDto[]>(`${this.api}/api/story/uploaded`);
  }

  getUploadedStory(id: string): Observable<UploadedStoryDto> {
    return this.http.get<UploadedStoryDto>(`${this.api}/api/story/uploaded/${id}`);
  }

  deleteUploadedStory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/admin/uploaded-stories/${id}`);
  }
}
