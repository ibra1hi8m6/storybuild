import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AppStateService } from '../../../services/app-state-service';
import { GateQuizService, GateQuiz1Question, GateQuiz1Answer } from '../../../services/gate-quiz.service';
import { TtsService } from '../../../services/tts.service';

type QuizPhase = 'loading' | 'quiz' | 'result';

@Component({
  selector: 'app-gate-quiz1',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './gate-quiz1.component.html',
  styleUrl: './gate-quiz1.component.css'
})
export class GateQuiz1Component implements OnInit {
  private readonly state   = inject(AppStateService);
  private readonly quiz    = inject(GateQuizService);
  private readonly router  = inject(Router);
  private readonly tts     = inject(TtsService);

  readonly phase         = signal<QuizPhase>('loading');
  readonly questions     = signal<GateQuiz1Question[]>([]);
  readonly currentIndex  = signal(0);
  readonly answers       = signal<GateQuiz1Answer[]>([]);
  readonly selectedLetter = signal<string | null>(null);
  readonly isSubmitting  = signal(false);
  readonly passed        = signal(false);
  readonly score         = signal(0);
  readonly error         = signal('');

  readonly currentQuestion = computed(() => this.questions()[this.currentIndex()]);
  readonly isLast          = computed(() => this.currentIndex() === this.questions().length - 1);
  readonly dots            = computed(() => this.questions().map((_, i) => i <= this.currentIndex()));

  ngOnInit(): void {
    const studentId = this.state.currentUser()?.id;
    if (!studentId) { this.router.navigate(['/dashboard']); return; }

    this.quiz.getGateQuiz1(studentId).subscribe({
      next:  res => { this.questions.set(res.questions); this.phase.set('quiz'); },
      error: err => {
        const msg = err?.error?.error ?? 'تعذّر تحميل الاختبار';
        this.error.set(msg);
        this.phase.set('result');
      }
    });
  }

  choose(letter: string): void {
    if (this.selectedLetter()) return; // already chose for this question

    this.selectedLetter.set(letter);

    // Brief pause so the child sees the highlight, then advance
    setTimeout(() => {
      const q = this.currentQuestion();
      this.answers.update(prev => [...prev, { letterId: q.letterId, chosenLetter: letter }]);

      if (this.isLast()) {
        this.submit();
      } else {
        this.currentIndex.update(i => i + 1);
        this.selectedLetter.set(null);
      }
    }, 600);
  }

  private submit(): void {
    this.isSubmitting.set(true);
    const studentId = this.state.currentUser()?.id!;

    this.quiz.submitGateQuiz1(studentId, this.answers()).subscribe({
      next: res => {
        this.passed.set(res.passed);
        this.score.set(res.score);
        this.isSubmitting.set(false);
        this.phase.set('result');

        if (res.passed) {
          this.state.updateStudentLevel(2);
          void this.tts.play('أحسنت! لقد انتقلت إلى المستوى الثاني', 'Kore');
        } else {
          void this.tts.play('حاول مرة أخرى، ستنجح قريباً', 'Kore');
        }
      },
      error: () => { this.isSubmitting.set(false); this.phase.set('result'); }
    });
  }

  goToDashboard(): void { this.router.navigate(['/dashboard']); }
  goToLetters():   void { this.router.navigate(['/learning/letters']); }
}
