import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  KnowledgeDocumentDto, RagSearchResult, GenerateLessonRequest,
  IngestDocumentResponse, RagPageChunkDto, IngestEducationalPdfRequest, GenerateLessonV2Request
} from '../models/rag.models';
import { LessonDetail } from '../models/lesson.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class RagService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  ingestDocument(file: File, letter?: string, level?: number, tags?: string): Observable<IngestDocumentResponse> {
    const form = new FormData();
    form.append('file', file);
    if (letter)       form.append('letter', letter);
    if (level != null) form.append('level', String(level));
    if (tags)         form.append('tags', tags);
    return this.http.post<IngestDocumentResponse>(`${this.api}/api/rag/ingest`, form);
  }

  getKnowledgeDocuments(): Observable<KnowledgeDocumentDto[]> {
    return this.http.get<KnowledgeDocumentDto[]>(`${this.api}/api/rag/documents`);
  }

  deleteKnowledgeDocument(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/rag/documents/${id}`);
  }

  ragSearch(query: string): Observable<RagSearchResult[]> {
    return this.http.post<RagSearchResult[]>(`${this.api}/api/rag/search`, JSON.stringify(query), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  generateRagLesson(req: GenerateLessonRequest): Observable<LessonDetail> {
    return this.http.post<LessonDetail>(`${this.api}/api/rag/generate-lesson`, req);
  }

  ingestEducationalPdf(file: File, level: number, letter: string, letterName: string): Observable<IngestDocumentResponse> {
    const form = new FormData();
    form.append('file',       file);
    form.append('level',      String(level));
    form.append('letter',     letter);
    form.append('letterName', letterName);
    return this.http.post<IngestDocumentResponse>(`${this.api}/api/rag/ingest-educational`, form);
  }

  getRagPageChunks(level?: number, letter?: string): Observable<RagPageChunkDto[]> {
    let url = `${this.api}/api/rag/page-chunks`;
    const params: string[] = [];
    if (level  != null) params.push(`level=${level}`);
    if (letter)         params.push(`letter=${encodeURIComponent(letter)}`);
    if (params.length)  url += '?' + params.join('&');
    return this.http.get<RagPageChunkDto[]>(url);
  }

  uploadKnowledgeDocument(file: File, name: string, description: string): Observable<any> {
    const form = new FormData();
    form.append('file',        file);
    form.append('name',        name);
    form.append('description', description);
    return this.http.post<any>(`${this.api}/api/rag/documents`, form);
  }
}
