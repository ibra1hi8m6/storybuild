import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface AnnotationDto {
  id: string;
  studentId: string;
  pageId: string;
  pageType: string;
  type: 'Highlight' | 'Stamp';
  colorHex: string;
  selectedText: string;
  startOffset: number;
  endOffset: number;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AnnotationService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/annotations`;

  async save(req: Omit<AnnotationDto, 'id' | 'createdAt'>): Promise<AnnotationDto> {
    return firstValueFrom(this.http.post<AnnotationDto>(this.base, req));
  }

  async getForPage(studentId: string, pageId: string): Promise<AnnotationDto[]> {
    return firstValueFrom(this.http.get<AnnotationDto[]>(`${this.base}/${studentId}/${pageId}`));
  }

  async delete(id: string, studentId: string): Promise<void> {
    await firstValueFrom(this.http.delete(`${this.base}/${id}/${studentId}`));
  }
}
