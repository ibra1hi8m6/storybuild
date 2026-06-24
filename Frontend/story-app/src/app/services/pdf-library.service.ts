import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PdfDocumentDto, PdfDocumentDetailDto, EmbedResultDto, PdfLibraryStatsDto } from '../models/pdf-library.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PdfLibraryService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  uploadPdfDocument(file: File, letter: string, level: number): Observable<PdfDocumentDto> {
    const fd = new FormData();
    fd.append('file',   file);
    fd.append('letter', letter);
    fd.append('level',  level.toString());
    return this.http.post<PdfDocumentDto>(`${this.api}/api/pdf-library/upload`, fd);
  }

  generatePdfEmbeddings(id: string): Observable<EmbedResultDto> {
    return this.http.post<EmbedResultDto>(`${this.api}/api/pdf-library/${id}/embed`, {});
  }

  getPdfDocuments(): Observable<PdfDocumentDto[]> {
    return this.http.get<PdfDocumentDto[]>(`${this.api}/api/pdf-library`);
  }

  getPdfDocument(id: string): Observable<PdfDocumentDetailDto> {
    return this.http.get<PdfDocumentDetailDto>(`${this.api}/api/pdf-library/${id}`);
  }

  deletePdfDocument(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/pdf-library/${id}`);
  }

  getPdfLibraryStats(): Observable<PdfLibraryStatsDto> {
    return this.http.get<PdfLibraryStatsDto>(`${this.api}/api/pdf-library/stats`);
  }
}
