import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  LetterContentDto,
  WordContentDto,
  SentenceContentDto,
  LearningAttemptDto,
  SaveLearningAttemptRequest,
  WritingCorrectionResponse,
  LearningContentType
} from '../models/learning.models';

@Injectable({ providedIn: 'root' })
export class LearningService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  // ── Letters ────────────────────────────────────────────────────────────────

  getLetters(): Observable<LetterContentDto[]> {
    return this.http.get<LetterContentDto[]>(`${this.api}/api/learning/letters`);
  }

  getAllLetters(): Observable<LetterContentDto[]> {
    return this.http.get<LetterContentDto[]>(`${this.api}/api/learning/letters/all`);
  }

  getLetter(id: string): Observable<LetterContentDto> {
    return this.http.get<LetterContentDto>(`${this.api}/api/learning/letters/${id}`);
  }

  createLetter(form: FormData): Observable<LetterContentDto> {
    return this.http.post<LetterContentDto>(`${this.api}/api/learning/letters`, form);
  }

  updateLetter(id: string, form: FormData): Observable<LetterContentDto> {
    return this.http.put<LetterContentDto>(`${this.api}/api/learning/letters/${id}`, form);
  }

  toggleLetterPublish(id: string, published: boolean): Observable<void> {
    return this.http.patch<void>(`${this.api}/api/learning/letters/${id}/publish?published=${published}`, {});
  }

  deleteLetter(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/learning/letters/${id}`);
  }

  // ── Words ─────────────────────────────────────────────────────────────────

  getWords(): Observable<WordContentDto[]> {
    return this.http.get<WordContentDto[]>(`${this.api}/api/learning/words`);
  }

  getAllWords(): Observable<WordContentDto[]> {
    return this.http.get<WordContentDto[]>(`${this.api}/api/learning/words/all`);
  }

  getWordsByLetter(letter: string): Observable<WordContentDto[]> {
    return this.http.get<WordContentDto[]>(`${this.api}/api/learning/words/by-letter/${encodeURIComponent(letter)}`);
  }

  getWordLetters(): Observable<string[]> {
    return this.http.get<string[]>(`${this.api}/api/learning/words/letters`);
  }

  getWord(id: string): Observable<WordContentDto> {
    return this.http.get<WordContentDto>(`${this.api}/api/learning/words/${id}`);
  }

  createWord(form: FormData): Observable<WordContentDto> {
    return this.http.post<WordContentDto>(`${this.api}/api/learning/words`, form);
  }

  updateWord(id: string, form: FormData): Observable<WordContentDto> {
    return this.http.put<WordContentDto>(`${this.api}/api/learning/words/${id}`, form);
  }

  deleteWord(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/learning/words/${id}`);
  }

  // ── Sentences ─────────────────────────────────────────────────────────────

  getSentences(): Observable<SentenceContentDto[]> {
    return this.http.get<SentenceContentDto[]>(`${this.api}/api/learning/sentences`);
  }

  getAllSentences(): Observable<SentenceContentDto[]> {
    return this.http.get<SentenceContentDto[]>(`${this.api}/api/learning/sentences/all`);
  }

  getSentence(id: string): Observable<SentenceContentDto> {
    return this.http.get<SentenceContentDto>(`${this.api}/api/learning/sentences/${id}`);
  }

  createSentence(form: FormData): Observable<SentenceContentDto> {
    return this.http.post<SentenceContentDto>(`${this.api}/api/learning/sentences`, form);
  }

  updateSentence(id: string, form: FormData): Observable<SentenceContentDto> {
    return this.http.put<SentenceContentDto>(`${this.api}/api/learning/sentences/${id}`, form);
  }

  deleteSentence(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/learning/sentences/${id}`);
  }

  // ── Attempts ──────────────────────────────────────────────────────────────

  saveAttempt(req: SaveLearningAttemptRequest): Observable<LearningAttemptDto> {
    return this.http.post<LearningAttemptDto>(`${this.api}/api/learning/attempts`, req);
  }

  getAttempts(childName: string, contentType?: LearningContentType): Observable<LearningAttemptDto[]> {
    let url = `${this.api}/api/learning/attempts/${encodeURIComponent(childName)}`;
    if (contentType) url += `?contentType=${contentType}`;
    return this.http.get<LearningAttemptDto[]>(url);
  }

  // ── Writing evaluation ────────────────────────────────────────────────────

  evaluateCanvas(imageBase64: string, expectedText: string): Observable<WritingCorrectionResponse> {
    return this.http.post<WritingCorrectionResponse>(`${this.api}/api/writing/canvas`, {
      imageBase64,
      expectedText
    });
  }

  // ── Page selection (uploaded stories) ───────────────────────────────────

  toggleStoryPage(pageId: string, isStoryPage: boolean): Observable<void> {
    return this.http.put<void>(`${this.api}/api/learning/story-pages/${pageId}/select?isStoryPage=${isStoryPage}`, {});
  }

  updateStoryPageAudio(pageId: string, audioText: string): Observable<void> {
    return this.http.put<void>(`${this.api}/api/learning/story-pages/${pageId}/audio`, { audioText });
  }

  updateLessonPageAudio(pageId: string, audioText: string): Observable<void> {
    return this.http.put<void>(`${this.api}/api/learning/lesson-pages/${pageId}/audio`, { audioText });
  }
}
