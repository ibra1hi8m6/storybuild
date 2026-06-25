import { Component, signal, inject, OnInit, OnDestroy, computed } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { LearningService } from '../../../services/learning.service';
import { AppStateService } from '../../../services/app-state-service';
import { TtsService } from '../../../services/tts.service';
import { LetterContentDto } from '../../../models/learning.models';

interface RecognitionQuestion {
  letter: LetterContentDto;
  options: string[];
}

@Component({
  selector: 'app-letter-recognition',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  templateUrl: './letter-recognition.component.html',
  styleUrl: './letter-recognition.component.css'
})
export class LetterRecognitionComponent implements OnInit, OnDestroy {
  private readonly svc    = inject(LearningService);
  private readonly router = inject(Router);
  private readonly state  = inject(AppStateService);
  private readonly tts    = inject(TtsService);

  readonly isLoading    = signal(true);
  readonly questions    = signal<RecognitionQuestion[]>([]);
  readonly currentIdx   = signal(0);
  readonly selected     = signal<string | null>(null);
  readonly showFeedback = signal(false);
  readonly score        = signal(0);
  readonly done         = signal(false);
  readonly isPlaying    = signal(false);

  readonly current = computed(() => this.questions()[this.currentIdx()]);
  readonly total   = computed(() => this.questions().length);
  readonly progress = computed(() =>
    this.total() > 0 ? Math.round(this.currentIdx() / this.total() * 100) : 0
  );

  private allLetters: LetterContentDto[] = [];

  ngOnInit(): void {
    this.svc.getLetters().subscribe({
      next: letters => {
        this.allLetters = letters;
        this.buildQuestions(letters);
        this.isLoading.set(false);
        if (this.questions().length) this.speak();
      },
      error: () => this.isLoading.set(false)
    });
  }

  ngOnDestroy(): void { this.tts.stop(); }

  private buildQuestions(letters: LetterContentDto[]): void {
    if (letters.length < 4) return;
    const shuffled = [...letters].sort(() => Math.random() - 0.5).slice(0, Math.min(10, letters.length));
    const qs: RecognitionQuestion[] = shuffled.map(l => {
      const wrong = letters
        .filter(x => x.letter !== l.letter)
        .sort(() => Math.random() - 0.5)
        .slice(0, 3)
        .map(x => x.letter);
      const options = [...wrong, l.letter].sort(() => Math.random() - 0.5);
      return { letter: l, options };
    });
    this.questions.set(qs);
  }

  select(opt: string): void {
    if (this.showFeedback()) return;
    this.selected.set(opt);
    this.showFeedback.set(true);
    const isCorrect = opt === this.current().letter.letter;
    if (isCorrect) {
      this.score.update(s => s + 1);
      this.speak('أحسنت!');
    } else {
      this.speak('حاول مرة أخرى');
    }

    const childName = this.state.childName() ?? '';
    this.svc.saveAttempt({
      childName,
      studentId: this.state.currentUser()?.id,
      contentType: 2, // LetterRecognition
      contentId: this.current().letter.id,
      attemptType: 2, // Reading
      expectedText: this.current().letter.letter,
      detectedText: opt,
      score: isCorrect ? 100 : 0,
      isCorrect,
      feedbackText: isCorrect ? 'إجابة صحيحة' : 'إجابة خاطئة'
    }).subscribe();

    setTimeout(() => this.advance(), 1400);
  }

  private advance(): void {
    this.showFeedback.set(false);
    this.selected.set(null);
    if (this.currentIdx() < this.total() - 1) {
      this.currentIdx.update(i => i + 1);
      this.speak();
    } else {
      this.done.set(true);
    }
  }

  speak(text?: string): void {
    const t = text ?? `ما الحرف الذي تبدأ به كلمة ${this.current()?.letter.exampleWord ?? ''}؟`;
    if (!t) return;
    this.isPlaying.set(true);
    void this.tts.play(t).finally(() => this.isPlaying.set(false));
  }

  isCorrectOpt(opt: string): boolean { return opt === this.current()?.letter.letter; }

  optClass(opt: string): string {
    if (!this.showFeedback()) return 'opt-btn';
    if (this.isCorrectOpt(opt)) return 'opt-btn correct';
    if (this.selected() === opt) return 'opt-btn wrong';
    return 'opt-btn';
  }

  restart(): void {
    this.buildQuestions(this.allLetters);
    this.currentIdx.set(0);
    this.score.set(0);
    this.done.set(false);
    this.selected.set(null);
    this.showFeedback.set(false);
    this.speak();
  }

  goBack(): void { this.router.navigate(['/learning/letters']); }
}
