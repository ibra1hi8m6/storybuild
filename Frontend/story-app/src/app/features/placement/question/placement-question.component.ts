import { Component, signal, computed, OnInit, OnDestroy, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { TtsService } from '../../../services/tts.service';

@Component({
  selector: 'app-placement-question',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './placement-question.component.html',
  styleUrl: './placement-question.component.css'
})
export class PlacementQuestionComponent implements OnInit, OnDestroy {
  private readonly router  = inject(Router);
  private readonly service = inject(StoryService);
  private readonly state   = inject(AppStateService);
  private readonly tts     = inject(TtsService);

  readonly questions    = signal<any[]>([]);
  readonly currentIdx   = signal(0);
  readonly answers      = signal<Record<string, string>>({});
  readonly selected     = signal<string | null>(null);
  readonly showFeedback = signal(false);
  readonly isPlaying    = signal(false);
  readonly isLoading    = signal(false);

  readonly currentQ  = computed(() => this.questions()[this.currentIdx()]);
  readonly total     = computed(() => this.questions().length);
  readonly progress  = computed(() =>
    this.total() > 0 ? Math.round(this.currentIdx() / this.total() * 100) : 0
  );
  readonly partLabel = computed(() => {
    const q = this.currentQ();
    return q ? `الجزء ${q.part ?? q.Part} من 3` : '';
  });

  // True when the question is audio-only (student hears the letter, must not see it)
  readonly isAudioOnly = computed(() => {
    const q = this.currentQ();
    if (!q) return false;
    if (q.isAudioOnly === true) return true;
    const text: string = (q.questionText ?? '').trim();
    return text.includes('تسمع') || text.includes('صوت') || text.includes('تسمعه');
  });

  // ============================================================
  //  اختبار تحديد المستوى - 3 أجزاء × 5 أسئلة = 15 سؤال
  // ============================================================
  // الجزء 1: التمييز بين (حرف – كلمة – جملة)
  // الجزء 2: التعرف على الحرف وصوته (صوتي فقط)
  // الجزء 3: تكوين جملة بسيطة
  // ============================================================
  private readonly mockQuestions: any[] = [
    // ═══════════════════════════════════════════════════════
    //  الجزء الأول: التمييز بين (حرف – كلمة – جملة) - 5 أسئلة
    // ═══════════════════════════════════════════════════════
    {
      id: 'p1-q1',
      part: 1,
      imageContent: 'أ',
      questionText: 'ما هذا؟',
      audioText: 'ما هذا؟',
      correctAnswer: 'A',
      options: [
        { key: 'A', emoji: '', label: 'حرف' },
        { key: 'B', emoji: '', label: 'كلمة' },
        { key: 'C', emoji: '', label: 'جملة' }
      ]
    },
    {
      id: 'p1-q2',
      part: 1,
      imageContent: 'قطة',
      questionText: 'ما هذا؟',
      audioText: 'ما هذا؟',
      correctAnswer: 'B',
      options: [
        { key: 'A', emoji: '', label: 'حرف' },
        { key: 'B', emoji: '', label: 'كلمة' },
        { key: 'C', emoji: '', label: 'جملة' }
      ]
    },
    {
      id: 'p1-q3',
      part: 1,
      imageContent: 'ذهب أخي إلى المدرسة.',
      questionText: 'ما هذا؟',
      audioText: 'ما هذا؟',
      correctAnswer: 'C',
      options: [
        { key: 'A', emoji: '', label: 'حرف' },
        { key: 'B', emoji: '', label: 'كلمة' },
        { key: 'C', emoji: '', label: 'جملة' }
      ]
    },
    {
      id: 'p1-q4',
      part: 1,
      imageContent: 'ق',
      questionText: 'ما هذا؟',
      audioText: 'ما هذا؟',
      correctAnswer: 'A',
      options: [
        { key: 'A', emoji: '', label: 'حرف' },
        { key: 'B', emoji: '', label: 'كلمة' },
        { key: 'C', emoji: '', label: 'جملة' }
      ]
    },
    {
      id: 'p1-q5',
      part: 1,
      imageContent: 'الولد يلعب بالكرة.',
      questionText: 'ما هذا؟',
      audioText: 'ما هذا؟',
      correctAnswer: 'C',
      options: [
        { key: 'A', emoji: '', label: 'حرف' },
        { key: 'B', emoji: '', label: 'كلمة' },
        { key: 'C', emoji: '', label: 'جملة' }
      ]
    },

    // ═══════════════════════════════════════════════════════
    //  الجزء الثاني: التعرف على الحرف وصوته - 5 أسئلة (صوتي فقط)
    // ═══════════════════════════════════════════════════════
    {
      id: 'p2-q1',
      part: 2,
      imageContent: '',
      questionText: 'أي حرف تسمعه؟',
      audioText: 'ألف',
      isAudioOnly: true,
      correctAnswer: 'B',
      options: [
        { key: 'A', emoji: '', label: 'ب' },
        { key: 'B', emoji: '', label: 'أ' },
        { key: 'C', emoji: '', label: 'ت' },
        { key: 'D', emoji: '', label: 'ث' }
      ]
    },
    {
      id: 'p2-q2',
      part: 2,
      imageContent: '',
      questionText: 'أي حرف تسمعه؟',
      audioText: 'باء',
      isAudioOnly: true,
      correctAnswer: 'A',
      options: [
        { key: 'A', emoji: '', label: 'ب' },
        { key: 'B', emoji: '', label: 'ت' },
        { key: 'C', emoji: '', label: 'ث' },
        { key: 'D', emoji: '', label: 'ن' }
      ]
    },
    {
      id: 'p2-q3',
      part: 2,
      imageContent: '',
      questionText: 'أي حرف تسمعه؟',
      audioText: 'تاء',
      isAudioOnly: true,
      correctAnswer: 'B',
      options: [
        { key: 'A', emoji: '', label: 'ب' },
        { key: 'B', emoji: '', label: 'ت' },
        { key: 'C', emoji: '', label: 'ث' },
        { key: 'D', emoji: '', label: 'ن' }
      ]
    },
    {
      id: 'p2-q4',
      part: 2,
      imageContent: '',
      questionText: 'أي حرف تسمعه؟',
      audioText: 'ثاء',
      isAudioOnly: true,
      correctAnswer: 'C',
      options: [
        { key: 'A', emoji: '', label: 'ب' },
        { key: 'B', emoji: '', label: 'ت' },
        { key: 'C', emoji: '', label: 'ث' },
        { key: 'D', emoji: '', label: 'ن' }
      ]
    },
    {
      id: 'p2-q5',
      part: 2,
      imageContent: '',
      questionText: 'أي حرف تسمعه؟',
      audioText: 'جيم',
      isAudioOnly: true,
      correctAnswer: 'D',
      options: [
        { key: 'A', emoji: '', label: 'ح' },
        { key: 'B', emoji: '', label: 'خ' },
        { key: 'C', emoji: '', label: 'ع' },
        { key: 'D', emoji: '', label: 'ج' }
      ]
    },

    // ═══════════════════════════════════════════════════════
    //  الجزء الثالث: تكوين جملة بسيطة - 5 أسئلة
    // ═══════════════════════════════════════════════════════
    {
      id: 'p3-q1',
      part: 3,
      imageContent: '🏃‍♂️⚽',
      questionText: 'رتب الكلمات: يلعب – الولد – الكرة',
      audioText: 'رتب الكلمات: يلعب، الولد، الكرة',
      correctAnswer: 'A',
      options: [
        { key: 'A', emoji: '', label: 'الولد يلعب الكرة' },
        { key: 'B', emoji: '', label: 'يلعب الولد الكرة' },
        { key: 'C', emoji: '', label: 'الكرة يلعب الولد' },
        { key: 'D', emoji: '', label: 'يلعب الكرة الولد' }
      ]
    },
    {
      id: 'p3-q2',
      part: 3,
      imageContent: '🐱🥛',
      questionText: 'رتب الكلمات: القطة – تشرب – الحليب',
      audioText: 'رتب الكلمات: القطة، تشرب، الحليب',
      correctAnswer: 'B',
      options: [
        { key: 'A', emoji: '', label: 'تشرب القطة الحليب' },
        { key: 'B', emoji: '', label: 'القطة تشرب الحليب' },
        { key: 'C', emoji: '', label: 'الحليب تشرب القطة' },
        { key: 'D', emoji: '', label: 'القطة الحليب تشرب' }
      ]
    },
    {
      id: 'p3-q3',
      part: 3,
      imageContent: '☀️🌳',
      questionText: 'أكمل الجملة: الشمس ___ في السماء.',
      audioText: 'أكمل الجملة: الشمس في السماء',
      correctAnswer: 'B',
      options: [
        { key: 'A', emoji: '', label: 'تنام' },
        { key: 'B', emoji: '', label: 'تشرق' },
        { key: 'C', emoji: '', label: 'تسبح' },
        { key: 'D', emoji: '', label: 'تطير' }
      ]
    },
    {
      id: 'p3-q4',
      part: 3,
      imageContent: '🦋🌸',
      questionText: 'أكمل الجملة: الفراشة ___ بين الزهور.',
      audioText: 'أكمل الجملة: الفراشة بين الزهور',
      correctAnswer: 'C',
      options: [
        { key: 'A', emoji: '', label: 'تسبح' },
        { key: 'B', emoji: '', label: 'تنام' },
        { key: 'C', emoji: '', label: 'تطير' },
        { key: 'D', emoji: '', label: 'تجري' }
      ]
    },
    {
      id: 'p3-q5',
      part: 3,
      imageContent: '📖👦',
      questionText: 'أكمل الجملة: الولد ___ القصة.',
      audioText: 'أكمل الجملة: الولد القصة',
      correctAnswer: 'A',
      options: [
        { key: 'A', emoji: '', label: 'يقرأ' },
        { key: 'B', emoji: '', label: 'يأكل' },
        { key: 'C', emoji: '', label: 'يلعب' },
        { key: 'D', emoji: '', label: 'ينام' }
      ]
    }
  ];

  ngOnInit(): void {
    this.questions.set(this.mockQuestions);
    this.speakQuestion();
  }

  ngOnDestroy(): void { this.tts.stop(); }

  private normalizeQuestions(qs: any[]): any[] {
    return qs.map(q => ({
      id:            q.id,
      part:          q.part ?? q.Part,
      imageContent:  q.imageContent ?? q.ImageContent ?? '',
      questionText:  q.questionText ?? q.QuestionText ?? '',
      audioText:     q.audioText    ?? q.AudioText    ?? '',
      isAudioOnly:   q.isAudioOnly  ?? q.IsAudioOnly  ?? false,
      correctAnswer: q.correctAnswer ?? q.CorrectAnswer ?? '',
      options:       (q.options ?? q.Options ?? []).map((o: any) => ({
        key:   o.key   ?? o.Key,
        emoji: o.emoji ?? o.Emoji ?? '',
        label: o.label ?? o.Label
      }))
    }));
  }

  selectOption(key: string, event?: MouseEvent): void {
    if (this.showFeedback()) return;
    (event?.target as HTMLElement)?.blur(); // remove focus ring so it doesn't carry to next question
    this.speakGen++;       // cancel any running speak sequence
    this.tts.stop();       // stop audio immediately
    this.selected.set(key);
    this.showFeedback.set(true);
    const q = this.currentQ();
    this.answers.update(a => ({ ...a, [q.id]: key }));
    setTimeout(() => this.advance(), 1200);
  }

  skip(): void {
    if (this.showFeedback()) return;
    const q = this.currentQ();
    if (!q) return;
    this.speakGen++;
    this.tts.stop();
    this.answers.update(a => ({ ...a, [q.id]: '' }));
    this.advance();
  }

  exit(): void {
    this.speakGen++;
    this.tts.stop();
    this.router.navigate(['/dashboard']);
  }

  private advance(): void {
    const idx = this.currentIdx();
    this.showFeedback.set(false);
    this.selected.set(null);
    if (idx < this.total() - 1) {
      this.currentIdx.set(idx + 1);
      this.speakQuestion();
    } else {
      this.submitAll();
    }
  }

  private submitAll(): void {
    const qs  = this.questions();
    const ans = this.answers();
    const request = {
      answers: qs.map(q => ({ questionId: q.id, answer: ans[q.id] ?? '' }))
    };
    this.service.submitPlacement(request).subscribe({
      next: result => {
        const assignedLevel = result.assignedLevel ?? result.level ?? 1;
        sessionStorage.setItem('placement_result', JSON.stringify({
          score: result.totalScore,
          total: this.total(),
          level: assignedLevel,
          p1: result.part1Score,
          p2: result.part2Score,
          p3: result.part3Score
        }));
        // Persist level to DB and update local session
        if (this.state.isLoggedIn()) {
          this.service.updateStudentLevel(assignedLevel).subscribe({
            next: updated => this.state.updateStudentLevel(assignedLevel, updated.token),
            error: () => {}
          });
        }
        this.router.navigate(['/test/result']);
      },
      error: () => {
        const score = qs.filter(q => ans[q.id] === q.correctAnswer).length;
        const p1 = qs.filter(q => q.part === 1 && ans[q.id] === q.correctAnswer).length;
        const p2 = qs.filter(q => q.part === 2 && ans[q.id] === q.correctAnswer).length;
        const p3 = qs.filter(q => q.part === 3 && ans[q.id] === q.correctAnswer).length;
        const level = (p1 < 5 || p2 < 5) ? 1 : (p3 < 5 ? 2 : 3);
        sessionStorage.setItem('placement_result', JSON.stringify({ score, total: this.total(), level, p1, p2, p3 }));
        this.router.navigate(['/test/result']);
      }
    });
  }

  isCorrect(key: string): boolean {
    const correct = (this.currentQ()?.correctAnswer ?? '').trim().toUpperCase();
    return key.trim().toUpperCase() === correct;
  }

  optionClass(key: string): string {
    if (!this.showFeedback()) return 'opt-btn';
    if (this.isCorrect(key)) return 'opt-btn correct';
    if ((this.selected() ?? '').trim().toUpperCase() === key.trim().toUpperCase()) return 'opt-btn wrong';
    return 'opt-btn';
  }

  feedbackText(): string {
    return this.isCorrect(this.selected() ?? '')
      ? '🌟 ممتاز! 💪'
      : 'إجابة خاطئة، لكنك تتعلم! 💙';
  }

  private speakGen = 0;

  speakQuestion(): void {
    const q = this.currentQ();
    if (!q) return;
    const gen = ++this.speakGen;
    this.isPlaying.set(true);
    void this.runSpeak(q, gen).finally(() => this.isPlaying.set(false));
  }

  private async runSpeak(q: any, gen: number): Promise<void> {
    const part = q.part ?? q.Part;

    if (part === 1) {
      await this.tts.playFromAsset('/audio/placement/ma-hatha.wav', q.questionText);
    } else if (part === 2) {
      await this.tts.playFromAsset('/audio/placement/ayi-harf.wav', q.questionText);
      if (gen !== this.speakGen) return; // student answered — skip the letter
      await this.tts.playFromAsset(`/audio/placement/${q.id}.wav`, q.audioText);
    } else {
      await this.tts.playFromAsset(`/audio/placement/${q.id}.wav`, q.questionText);
    }
  }
}