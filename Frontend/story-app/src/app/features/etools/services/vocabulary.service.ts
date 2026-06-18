import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface WordJournalDto {
  id: string;
  studentId: string;
  word: string;
  definition: string;
  imageUrl?: string;
  createdAt: string;
}

export interface LessonVocabularyDto {
  id: string;
  lessonId: string;
  word: string;
  definition: string;
  imageUrl?: string;
  order: number;
}

@Injectable({ providedIn: 'root' })
export class VocabularyService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/vocabulary`;

  async getLessonVocabulary(lessonId: string): Promise<LessonVocabularyDto[]> {
    return firstValueFrom(this.http.get<LessonVocabularyDto[]>(`${this.base}/lesson/${lessonId}`));
  }

  async getJournal(studentId: string): Promise<WordJournalDto[]> {
    return firstValueFrom(this.http.get<WordJournalDto[]>(`${this.base}/journal/${studentId}`));
  }

  async addToJournal(studentId: string, word: string, definition: string): Promise<WordJournalDto> {
    return firstValueFrom(
      this.http.post<WordJournalDto>(`${this.base}/journal`, { studentId, word, definition })
    );
  }

  async removeFromJournal(id: string, studentId: string): Promise<void> {
    await firstValueFrom(this.http.delete(`${this.base}/journal/${id}/${studentId}`));
  }
}
