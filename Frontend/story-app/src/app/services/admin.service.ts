import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ImportBookResponse, AdminBooksPageDto, LessonDetail, CreateManualBookRequest } from '../models/lesson.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  importBook(level: number, letter: string, letterName: string, pdfFile: File): Observable<ImportBookResponse> {
    const form = new FormData();
    form.append('level',      String(level));
    form.append('letter',     letter);
    form.append('letterName', letterName);
    form.append('pdfFile',    pdfFile);
    return this.http.post<ImportBookResponse>(`${this.api}/api/admin/import-book`, form);
  }

  importBookV2(level: number, letter: string, letterName: string, title: string, pdfFile: File): Observable<ImportBookResponse> {
    const form = new FormData();
    form.append('level',      String(level));
    form.append('letter',     letter);
    form.append('letterName', letterName);
    form.append('title',      title);
    form.append('pdfFile',    pdfFile);
    return this.http.post<ImportBookResponse>(`${this.api}/api/admin/import-book`, form);
  }

  getAllBooksAdmin(level?: number, page = 1, pageSize = 9): Observable<AdminBooksPageDto> {
    let url = `${this.api}/api/admin/books?page=${page}&pageSize=${pageSize}`;
    if (level != null) url += `&level=${level}`;
    return this.http.get<AdminBooksPageDto>(url);
  }

  getBookDetailAdmin(id: string): Observable<LessonDetail> {
    return this.http.get<any>(`${this.api}/api/admin/books/${id}`).pipe(
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

  deleteBook(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/admin/books/${id}`);
  }

  publishLesson(id: string): Observable<any> {
    return this.http.post(`${this.api}/api/admin/books/${id}/publish`, {});
  }

  unpublishLesson(id: string): Observable<any> {
    return this.http.post(`${this.api}/api/admin/books/${id}/unpublish`, {});
  }

  publishStory(id: string): Observable<any> {
    return this.http.post(`${this.api}/api/admin/stories/${id}/publish`, {});
  }

  unpublishStory(id: string): Observable<any> {
    return this.http.post(`${this.api}/api/admin/stories/${id}/unpublish`, {});
  }

  updateBookPageSentence(bookId: string, pageId: string, sentence: string): Observable<void> {
    return this.http.patch<void>(
      `${this.api}/api/admin/books/${bookId}/pages/${pageId}/sentence`,
      { sentence }
    );
  }

  createManualBook(req: CreateManualBookRequest): Observable<ImportBookResponse> {
    return this.http.post<ImportBookResponse>(`${this.api}/api/admin/books/manual`, req);
  }

  getAiSettings(): Observable<any> {
    return this.http.get<any>(`${this.api}/api/admin/ai-settings`);
  }

  saveAiSettings(settings: any): Observable<any> {
    return this.http.put<any>(`${this.api}/api/admin/ai-settings`, settings);
  }

  getSubscriptionStats(): Observable<any> {
    return this.http.get<any>(`${this.api}/api/admin/subscriptions/stats`);
  }

  getAllUsers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.api}/api/admin/users`);
  }

  blockUser(id: string): Observable<void> {
    return this.http.post<void>(`${this.api}/api/admin/users/${id}/block`, {});
  }

  unblockUser(id: string): Observable<void> {
    return this.http.post<void>(`${this.api}/api/admin/users/${id}/unblock`, {});
  }

  getSchools(): Observable<any[]> {
    return this.http.get<any[]>(`${this.api}/api/admin/schools`);
  }

  createSchool(body: { schoolName: string; adminEmail: string; adminPassword: string }): Observable<any> {
    return this.http.post<any>(`${this.api}/api/admin/schools`, body);
  }
}
