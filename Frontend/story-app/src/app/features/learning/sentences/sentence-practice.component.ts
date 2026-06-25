import { Component, signal, inject, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewInit, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { LearningService } from '../../../services/learning.service';
import { AppStateService } from '../../../services/app-state-service';
import { TtsService } from '../../../services/tts.service';
import { SentenceContentDto } from '../../../models/learning.models';

type Stage = 'choose' | 'reading' | 'writing';
type Tool  = 'pen' | 'eraser';

@Component({
  selector: 'app-sentence-practice',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  templateUrl: './sentence-practice.component.html',
  styleUrl: './sentence-practice.component.css'
})
export class SentencePracticeComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('canvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly svc    = inject(LearningService);
  private readonly state  = inject(AppStateService);
  private readonly tts    = inject(TtsService);

  readonly sentence       = signal<SentenceContentDto | null>(null);
  readonly isLoading      = signal(true);
  readonly stage          = signal<Stage>('choose');
  readonly selected       = signal<number | null>(null);
  readonly showResult     = signal(false);
  readonly isPlaying      = signal(false);
  readonly isRecording    = signal(false);
  readonly activeTool     = signal<Tool>('pen');
  readonly writingResult      = signal<{ isCorrect: boolean; feedback: string } | null>(null);
  readonly isWritingChecking  = signal(false);
  readonly readingResult      = signal<{ isCorrect: boolean; feedback: string } | null>(null);
  readonly nextSentenceId     = signal<string | null>(null);

  private recognition: any = null;
  private ctx!: CanvasRenderingContext2D;
  private drawing = false;
  private lastX = 0;
  private lastY = 0;

  readonly options = computed(() => {
    const s = this.sentence();
    if (!s) return [];
    return [
      { index: 1, text: s.option1, audio: s.option1Audio },
      { index: 2, text: s.option2, audio: s.option2Audio },
      { index: 3, text: s.option3, audio: s.option3Audio }
    ];
  });

  readonly correctText = computed(() => {
    const s = this.sentence();
    if (!s) return '';
    return [s.option1, s.option2, s.option3][s.correctOptionIndex - 1];
  });

  readonly correctAudio = computed(() => {
    const s = this.sentence();
    if (!s) return '';
    return [s.option1Audio, s.option2Audio, s.option3Audio][s.correctOptionIndex - 1];
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.svc.getSentence(id).subscribe({
      next: d  => {
        this.sentence.set(d);
        this.isLoading.set(false);
        this.svc.getSentences().subscribe(all => {
          const sorted = [...all].sort((a, b) => a.sortOrder - b.sortOrder);
          const idx = sorted.findIndex(s => s.id === id);
          if (idx !== -1 && idx < sorted.length - 1)
            this.nextSentenceId.set(sorted[idx + 1].id);
        });
      },
      error: () => this.isLoading.set(false)
    });
  }

  ngAfterViewInit(): void { this.initCanvas(); }
  ngOnDestroy(): void { this.tts.stop(); this.stopRecording(); }

  enterWritingStage(): void {
    this.stage.set('writing');
    // Canvas is inside @if(stage()==='writing') so it renders after this signal
    // update. A microtask delay lets Angular finish rendering before we bind.
    setTimeout(() => this.initCanvas(), 0);
  }

  listenOption(audio: string): void {
    if (!audio) return;
    this.isPlaying.set(true);
    void this.tts.play(audio).finally(() => this.isPlaying.set(false));
  }

  choose(idx: number): void {
    // lock after correct answer; allow re-tap on wrong
    if (this.showResult()) {
      if (this.selected() === this.sentence()?.correctOptionIndex) return;
      this.showResult.set(false);
      this.selected.set(null);
    }
    this.selected.set(idx);
    this.showResult.set(true);
    const s = this.sentence()!;
    const isCorrect = idx === s.correctOptionIndex;
    this.svc.saveAttempt({
      childName: this.state.childName() ?? '',
      studentId: this.state.currentUser()?.id,
      contentType: 4, // SentencePractice
      contentId: s.id,
      attemptType: 2, // Reading
      expectedText: String(s.correctOptionIndex),
      detectedText: String(idx),
      score: isCorrect ? 100 : 0,
      isCorrect,
      feedbackText: isCorrect ? 'اخترت الجملة الصحيحة' : 'الإجابة خاطئة'
    }).subscribe();
    if (isCorrect) {
      this.listenOption(this.correctAudio());
    } else {
      this.listenOption('حاول مرة أخرى');
    }
  }

  optClass(idx: number): string {
    if (!this.showResult() || this.selected() !== idx) return 'opt-card';
    const s = this.sentence();
    return idx === s?.correctOptionIndex ? 'opt-card correct' : 'opt-card wrong';
  }

  startRecording(): void {
    const SR = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
    if (!SR) { alert('متصفحك لا يدعم التعرف على الصوت'); return; }
    this.recognition = new SR();
    this.recognition.lang = 'ar-SA'; this.recognition.interimResults = false;
    this.recognition.onstart = () => this.isRecording.set(true);
    this.recognition.onend   = () => this.isRecording.set(false);
    this.recognition.onresult = (e: any) => {
      const said = e.results[0][0].transcript.trim();
      const expected = this.correctText().trim();
      const saidN = this.normalizeAr(said);
      const saidWords = new Set(saidN.split(/\s+/).filter(Boolean));
      const expWords = this.normalizeAr(expected).split(/\s+/).filter(Boolean);
      const matched  = expWords.filter(w => saidWords.has(w)).length;
      const isCorrect = matched >= Math.ceil(expWords.length * 0.6);
      const feedback = isCorrect ? `أحسنت! قرأت الجملة بشكل رائع 🌟` : `حاول مرة أخرى ✏️`;
      this.svc.saveAttempt({
        childName: this.state.childName() ?? '',
        studentId: undefined,
        contentType: 4,
        contentId: this.sentence()!.id,
        attemptType: 2,
        expectedText: expected,
        detectedText: said,
        score: isCorrect ? 100 : 30,
        isCorrect, feedbackText: feedback
      }).subscribe();
      this.readingResult.set({ isCorrect, feedback });
      this.listenOption(feedback);
    };
    this.recognition.onerror = () => this.isRecording.set(false);
    this.recognition.start();
  }

  stopRecording(): void { try { this.recognition?.stop(); } catch {} }

  private normalizeAr(text: string): string {
    return text
      .replace(/[ً-ٰٟ]/g, '') // strip diacritics / tashkeel
      .replace(/[أإآ]/g, 'ا')
      .replace(/ة/g, 'ه')
      .replace(/ى/g, 'ي')
      .replace(/\s+/g, ' ').trim();
  }

  private initCanvas(): void {
    const el = this.canvasRef?.nativeElement; if (!el) return;
    this.ctx = el.getContext('2d')!;
    el.width = el.offsetWidth; el.height = el.offsetHeight;
    this.drawGuide();
    el.addEventListener('mousedown',  e => this.startDraw(e));
    el.addEventListener('mousemove',  e => this.doDraw(e));
    el.addEventListener('mouseup',    () => { this.drawing = false; });
    el.addEventListener('mouseleave', () => { this.drawing = false; });
    el.addEventListener('touchstart', e => { e.preventDefault(); this.startDrawT(e); }, { passive: false });
    el.addEventListener('touchmove',  e => { e.preventDefault(); this.doDrawT(e); }, { passive: false });
    el.addEventListener('touchend',   () => { this.drawing = false; });
  }

  private drawGuide(): void {
    if (!this.ctx) return;
    const c = this.canvasRef.nativeElement;
    this.ctx.clearRect(0, 0, c.width, c.height);
    const text = this.correctText();
    if (!text) return;
    this.ctx.font = `bold ${Math.min(c.width / text.length * 1.4, 32)}px Cairo, serif`;
    this.ctx.textAlign = 'center'; this.ctx.textBaseline = 'middle';
    this.ctx.fillStyle = 'rgba(16,185,129,0.15)';
    this.ctx.fillText(text, c.width / 2, c.height / 2);
  }

  private startDraw(e: MouseEvent): void { this.drawing = true; const r = this.canvasRef.nativeElement.getBoundingClientRect(); this.lastX = e.clientX - r.left; this.lastY = e.clientY - r.top; }
  private startDrawT(e: TouchEvent): void { this.drawing = true; const r = this.canvasRef.nativeElement.getBoundingClientRect(); this.lastX = e.touches[0].clientX - r.left; this.lastY = e.touches[0].clientY - r.top; }
  private doDraw(e: MouseEvent): void { if (!this.drawing) return; const r = this.canvasRef.nativeElement.getBoundingClientRect(); this.stroke(e.clientX - r.left, e.clientY - r.top); }
  private doDrawT(e: TouchEvent): void { if (!this.drawing) return; const r = this.canvasRef.nativeElement.getBoundingClientRect(); this.stroke(e.touches[0].clientX - r.left, e.touches[0].clientY - r.top); }
  private stroke(x: number, y: number): void {
    const ctx = this.ctx;
    ctx.beginPath(); ctx.moveTo(this.lastX, this.lastY); ctx.lineTo(x, y);
    ctx.strokeStyle = this.activeTool() === 'eraser' ? '#fff' : '#10B981';
    ctx.lineWidth = this.activeTool() === 'eraser' ? 28 : 4;
    ctx.lineCap = 'round'; ctx.lineJoin = 'round'; ctx.stroke();
    this.lastX = x; this.lastY = y; this.writingResult.set(null);
  }

  clearCanvas(): void { const c = this.canvasRef.nativeElement; this.ctx.clearRect(0, 0, c.width, c.height); this.drawGuide(); this.writingResult.set(null); }

  submitWriting(): void {
    const s = this.sentence(); if (!s) return;
    const canvas = this.canvasRef.nativeElement;
    const data = this.ctx.getImageData(0, 0, canvas.width, canvas.height).data;
    let hasStrokes = false;
    for (let i = 3; i < data.length; i += 4) { if (data[i] > 50) { hasStrokes = true; break; } }
    if (!hasStrokes) return;

    const base64       = canvas.toDataURL('image/png').split(',')[1];
    const expectedText = this.correctText();

    this.isWritingChecking.set(true);
    this.svc.evaluateCanvas(base64, expectedText).subscribe({
      next: res => {
        const isCorrect    = res.isAccepted;
        const feedbackText = res.displayMessage || (isCorrect ? 'أحسنت! 🌟' : 'حاول مرة أخرى ✏️');
        this.writingResult.set({ isCorrect, feedback: feedbackText });
        this.isWritingChecking.set(false);
        this.svc.saveAttempt({
          childName:    this.state.childName() ?? '',
          studentId:    undefined,
          contentType:  4,
          contentId:    s.id,
          attemptType:  1,
          expectedText,
          detectedText: res.extractedText,
          score:        Math.round(res.similarityScore * 100),
          isCorrect,
          feedbackText
        }).subscribe();
        this.listenOption(res.spokenFeedback || feedbackText);
      },
      error: () => {
        this.isWritingChecking.set(false);
        const msg = 'حدث خطأ أثناء التقييم، حاول مرة أخرى.';
        this.writingResult.set({ isCorrect: false, feedback: msg });
        this.listenOption(msg);
      }
    });
  }

  tryAgainChoose(): void { this.showResult.set(false); this.selected.set(null); }
  goToReading(): void { this.stage.set('reading'); }
  tryAgainReading(): void { this.readingResult.set(null); }
  goToWriting(): void { this.readingResult.set(null); this.enterWritingStage(); }
  tryAgainWriting(): void { this.writingResult.set(null); this.clearCanvas(); }

  goNext(): void {
    const next = this.nextSentenceId();
    this.router.navigate(next ? ['/learning/sentences', next] : ['/learning/sentences']);
  }

  goBack(): void { this.router.navigate(['/learning/sentences']); }
}
