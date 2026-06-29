import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface GateQuiz1Question {
  questionIndex: number;
  letterId:      string;
  imagePath:     string;
  choices:       string[];
}

export interface GateQuiz1Answer {
  letterId:      string;
  chosenLetter:  string;
}

export interface GateQuiz1Result {
  passed: boolean;
  score:  number;
  total:  number;
  reset:  boolean;
}

export interface GateQuiz2Word {
  wordId:      string;
  displayWord: string;
  audioText:   string;
  imagePath:   string | null;
}

export interface GateQuiz2Sentence {
  sentenceId:   string;
  sentenceText: string;
  audioText:    string;
}

export interface GateQuiz2Data {
  words:     GateQuiz2Word[];
  sentences: GateQuiz2Sentence[];
}

@Injectable({ providedIn: 'root' })
export class GateQuizService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  // ── Gate Quiz 1 ────────────────────────────────────────────────────────────
  getGateQuiz1(studentId: string): Observable<{ questions: GateQuiz1Question[] }> {
    return this.http.get<{ questions: GateQuiz1Question[] }>(
      `${this.api}/api/quiz/gate1/${studentId}`
    );
  }

  submitGateQuiz1(studentId: string, answers: GateQuiz1Answer[]): Observable<GateQuiz1Result> {
    return this.http.post<GateQuiz1Result>(`${this.api}/api/quiz/gate1/submit`, {
      studentId,
      answers
    });
  }

  // ── Gate Quiz 2 ────────────────────────────────────────────────────────────
  getGateQuiz2(studentId: string): Observable<GateQuiz2Data> {
    return this.http.get<GateQuiz2Data>(`${this.api}/api/quiz/gate2/${studentId}`);
  }

  completeGateQuiz2(studentId: string, passed: boolean): Observable<{ passed: boolean; reset: boolean }> {
    return this.http.post<{ passed: boolean; reset: boolean }>(
      `${this.api}/api/quiz/gate2/complete`,
      { studentId, passed }
    );
  }
}
