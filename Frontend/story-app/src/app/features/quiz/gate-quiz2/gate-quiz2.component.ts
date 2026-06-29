import {
  Component, signal, computed, inject, OnInit, ViewChild, ElementRef, AfterViewInit
} from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AppStateService } from '../../../services/app-state-service';
import { GateQuizService, GateQuiz2Word, GateQuiz2Sentence } from '../../../services/gate-quiz.service';
import { LearningService } from '../../../services/learning.service';
import { FluencyApiService } from '../../fluency/services/fluency-api.service';
import { TtsService } from '../../../services/tts.service';

type Phase = 'loading' | 'words' | 'sentences' | 'submitting' | 'result';
type RecordState = 'idle' | 'recording' | 'evaluating';

@Component({
  selector: 'app-gate-quiz2',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './gate-quiz2.component.html',
  styleUrl: './gate-quiz2.component.css'
})
export class GateQuiz2Component implements OnInit, AfterViewInit {
  @ViewChild('canvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly state     = inject(AppStateService);
  private readonly quiz      = inject(GateQuizService);
  private readonly learning  = inject(LearningService);
  private readonly fluency   = inject(FluencyApiService);
  private readonly router    = inject(Router);
  private readonly tts       = inject(TtsService);

  // ── Data ────────────────────────────────────────────────────────────────────

  readonly phase         = signal<Phase>('loading');
  readonly words         = signal<GateQuiz2Word[]>([]);
  readonly sentences     = signal<GateQuiz2Sentence[]>([]);

  // Word part
  readonly wordIndex     = signal(0);
  readonly wordResults   = signal<boolean[]>([]);
  readonly isEvaluating  = signal(false);
  readonly wordFeedback  = signal<string | null>(null);
  private drawing        = false;
  private ctx!: CanvasRenderingContext2D;

  // Sentence part
  readonly sentenceIndex  = signal(0);
  readonly sentenceResults = signal<boolean[]>([]);
  readonly recordState    = signal<RecordState>('idle');
  private mediaRecorder!: MediaRecorder;
  private audioChunks:    Blob[] = [];
  private stream!: MediaStream;

  // Result
  readonly passed         = signal(false);
  readonly wordScore      = signal(0);
  readonly sentenceScore  = signal(0);
  readonly error          = signal('');

  readonly currentWord     = computed(() => this.words()[this.wordIndex()]);
  readonly currentSentence = computed(() => this.sentences()[this.sentenceIndex()]);
  readonly wordDots        = computed(() => this.words().map((_, i) => i < this.wordResults().length));
  readonly sentenceDots    = computed(() => this.sentences().map((_, i) => i < this.sentenceResults().length));

  // ── Lifecycle ────────────────────────────────────────────────────────────────

  ngOnInit(): void {
    const studentId = this.state.currentUser()?.id;
    if (!studentId) { this.router.navigate(['/dashboard']); return; }

    this.quiz.getGateQuiz2(studentId).subscribe({
      next: data => {
        this.words.set(data.words);
        this.sentences.set(data.sentences);
        this.phase.set('words');
      },
      error: err => {
        this.error.set(err?.error?.error ?? 'تعذّر تحميل الاختبار');
        this.phase.set('result');
      }
    });
  }

  ngAfterViewInit(): void {
    this.initCanvas();
  }

  private initCanvas(): void {
    if (!this.canvasRef) return;
    const canvas = this.canvasRef.nativeElement;
    this.ctx = canvas.getContext('2d')!;
    this.ctx.strokeStyle = '#1F2937';
    this.ctx.lineWidth   = 4;
    this.ctx.lineCap     = 'round';
    this.ctx.lineJoin    = 'round';

    canvas.addEventListener('mousedown',  e => this.startDraw(e));
    canvas.addEventListener('mousemove',  e => this.draw(e));
    canvas.addEventListener('mouseup',    () => this.endDraw());
    canvas.addEventListener('mouseleave', () => this.endDraw());
    canvas.addEventListener('touchstart', e => { e.preventDefault(); this.startDraw(e.touches[0]); }, { passive: false });
    canvas.addEventListener('touchmove',  e => { e.preventDefault(); this.draw(e.touches[0]); }, { passive: false });
    canvas.addEventListener('touchend',   () => this.endDraw());
  }

  private pos(e: MouseEvent | Touch) {
    const rect = this.canvasRef.nativeElement.getBoundingClientRect();
    return { x: e.clientX - rect.left, y: e.clientY - rect.top };
  }

  private startDraw(e: MouseEvent | Touch): void {
    this.drawing = true;
    const { x, y } = this.pos(e);
    this.ctx.beginPath();
    this.ctx.moveTo(x, y);
  }

  private draw(e: MouseEvent | Touch): void {
    if (!this.drawing) return;
    const { x, y } = this.pos(e);
    this.ctx.lineTo(x, y);
    this.ctx.stroke();
  }

  private endDraw(): void { this.drawing = false; }

  clearCanvas(): void {
    const c = this.canvasRef.nativeElement;
    this.ctx.clearRect(0, 0, c.width, c.height);
    this.wordFeedback.set(null);
  }

  // ── Word evaluation ──────────────────────────────────────────────────────────

  async submitWord(): Promise<void> {
    const canvas = this.canvasRef.nativeElement;
    const imageBase64 = canvas.toDataURL('image/png').split(',')[1];
    const expected    = this.currentWord().displayWord;

    this.isEvaluating.set(true);
    this.wordFeedback.set(null);
    try {
      const res = await this.learning.evaluateCanvas(imageBase64, expected).toPromise();
      const correct = res?.isAccepted ?? false;
      this.wordFeedback.set(correct ? 'أحسنت! ✓' : `الإجابة الصحيحة: ${expected}`);
      this.wordResults.update(prev => [...prev, correct]);

      setTimeout(() => {
        if (this.wordIndex() < this.words().length - 1) {
          this.wordIndex.update(i => i + 1);
          this.clearCanvas();
          this.initCanvas();
        } else {
          this.phase.set('sentences');
          setTimeout(() => this.initSentenceMic(), 100);
        }
        this.wordFeedback.set(null);
      }, 1200);
    } finally {
      this.isEvaluating.set(false);
    }
  }

  // ── Sentence recording ───────────────────────────────────────────────────────

  private async initSentenceMic(): Promise<void> {
    try {
      this.stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    } catch {
      this.error.set('يرجى السماح بالوصول للميكروفون');
    }
  }

  async startRecording(): Promise<void> {
    if (!this.stream) {
      try { this.stream = await navigator.mediaDevices.getUserMedia({ audio: true }); } catch { return; }
    }
    this.audioChunks = [];
    this.mediaRecorder = new MediaRecorder(this.stream);
    this.mediaRecorder.ondataavailable = e => { if (e.data.size > 0) this.audioChunks.push(e.data); };
    this.mediaRecorder.start();
    this.recordState.set('recording');
  }

  async stopRecording(): Promise<void> {
    this.recordState.set('evaluating');
    this.mediaRecorder.stop();

    await new Promise<void>(res => { this.mediaRecorder.onstop = () => res(); });

    const blob = new Blob(this.audioChunks, { type: 'audio/webm' });
    const sent = this.currentSentence();

    try {
      const report = await this.fluency.evaluate({
        pageId:       sent.sentenceId,
        pageType:     'Sentence',
        expectedText: sent.audioText,
        audioBlob:    blob
      });

      const pass = report.passed;
      this.sentenceResults.update(prev => [...prev, pass]);

      setTimeout(() => {
        if (this.sentenceIndex() < this.sentences().length - 1) {
          this.sentenceIndex.update(i => i + 1);
        } else {
          this.finalSubmit();
        }
        this.recordState.set('idle');
      }, 800);
    } catch {
      this.sentenceResults.update(prev => [...prev, false]);
      this.recordState.set('idle');
    }
  }

  // ── Final submit ─────────────────────────────────────────────────────────────

  private finalSubmit(): void {
    this.phase.set('submitting');
    const wScore = this.wordResults().filter(Boolean).length;
    const sScore = this.sentenceResults().filter(Boolean).length;
    const pass   = wScore === this.words().length && sScore === this.sentences().length;

    const studentId = this.state.currentUser()?.id!;
    this.quiz.completeGateQuiz2(studentId, pass).subscribe({
      next: () => {
        this.wordScore.set(wScore);
        this.sentenceScore.set(sScore);
        this.passed.set(pass);
        this.phase.set('result');
        if (pass) {
          this.state.updateStudentLevel(3);
          void this.tts.play('ممتاز! لقد انتقلت إلى المستوى الثالث', 'Kore');
        } else {
          void this.tts.play('حاول مرة أخرى، ستنجح قريباً', 'Kore');
        }
      },
      error: () => { this.passed.set(false); this.phase.set('result'); }
    });
  }

  playAudio(text: string): void {
    void this.tts.play(text, 'Kore');
  }

  goToDashboard():   void { this.router.navigate(['/dashboard']); }
  goToWordsSentences(): void { this.router.navigate(['/learning/words-sentences']); }
}
