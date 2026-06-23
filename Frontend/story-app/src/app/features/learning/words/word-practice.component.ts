import {
  Component, signal, inject, OnInit, OnDestroy,
  ViewChild, ElementRef, AfterViewInit
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { LearningService } from '../../../services/learning.service';
import { AppStateService } from '../../../services/app-state-service';
import { WordContentDto } from '../../../models/learning.models';

type Tool = 'pen' | 'eraser';
type Stage = 'read' | 'write' | 'done';

@Component({
  selector: 'app-word-practice',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  templateUrl: './word-practice.component.html',
  styleUrl: './word-practice.component.css'
})
export class WordPracticeComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('canvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly svc    = inject(LearningService);
  private readonly state  = inject(AppStateService);

  readonly word       = signal<WordContentDto | null>(null);
  readonly isLoading  = signal(true);
  readonly stage      = signal<Stage>('read');
  readonly isPlaying  = signal(false);
  readonly isRecording = signal(false);
  readonly result     = signal<{ isCorrect: boolean; feedback: string } | null>(null);
  readonly activeTool = signal<Tool>('pen');
  isChecking = false;

  private ctx!: CanvasRenderingContext2D;
  private drawing = false;
  private lastX = 0;
  private lastY = 0;

  private recognition: any = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.svc.getWord(id).subscribe({
      next: d  => {
        this.word.set(d);
        this.isLoading.set(false);
        this.speak();
        setTimeout(() => this.initCanvas(), 0);
      },
      error: () => this.isLoading.set(false)
    });
  }

  ngAfterViewInit(): void { this.initCanvas(); }
  ngOnDestroy(): void {
    window.speechSynthesis.cancel();
    this.stopRecording();
  }

  private initCanvas(): void {
    const el = this.canvasRef?.nativeElement;
    if (!el) return;
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
    const w = this.word(); if (!w || !this.ctx) return;
    const c = this.canvasRef.nativeElement;
    this.ctx.clearRect(0, 0, c.width, c.height);
    this.ctx.font = `bold ${Math.min(c.width * .35, 80)}px Cairo, serif`;
    this.ctx.textAlign = 'center'; this.ctx.textBaseline = 'middle';
    this.ctx.fillStyle = 'rgba(14,165,233,0.18)';
    this.ctx.fillText(w.displayWord, c.width / 2, c.height / 2);
  }

  private startDraw(e: MouseEvent): void {
    this.drawing = true;
    const r = this.canvasRef.nativeElement.getBoundingClientRect();
    this.lastX = e.clientX - r.left; this.lastY = e.clientY - r.top;
  }
  private startDrawT(e: TouchEvent): void {
    this.drawing = true;
    const r = this.canvasRef.nativeElement.getBoundingClientRect();
    this.lastX = e.touches[0].clientX - r.left; this.lastY = e.touches[0].clientY - r.top;
  }
  private doDraw(e: MouseEvent): void {
    if (!this.drawing) return;
    const r = this.canvasRef.nativeElement.getBoundingClientRect();
    this.stroke(e.clientX - r.left, e.clientY - r.top);
  }
  private doDrawT(e: TouchEvent): void {
    if (!this.drawing) return;
    const r = this.canvasRef.nativeElement.getBoundingClientRect();
    this.stroke(e.touches[0].clientX - r.left, e.touches[0].clientY - r.top);
  }
  private stroke(x: number, y: number): void {
    const ctx = this.ctx;
    ctx.beginPath(); ctx.moveTo(this.lastX, this.lastY); ctx.lineTo(x, y);
    ctx.strokeStyle = this.activeTool() === 'eraser' ? '#fff' : '#0EA5E9';
    ctx.lineWidth   = this.activeTool() === 'eraser' ? 28 : 5;
    ctx.lineCap = 'round'; ctx.lineJoin = 'round'; ctx.stroke();
    this.lastX = x; this.lastY = y; this.result.set(null);
  }

  clearCanvas(): void {
    const c = this.canvasRef.nativeElement;
    this.ctx.clearRect(0, 0, c.width, c.height);
    this.drawGuide(); this.result.set(null);
  }

  speak(text?: string): void {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) return;
    window.speechSynthesis.cancel();
    const w = this.word();
    const t = text ?? w?.audioText ?? w?.displayWord ?? '';
    if (!t) return;
    const u = new SpeechSynthesisUtterance(t);
    u.lang = 'ar-SA'; u.rate = 0.85;
    u.onstart = () => this.isPlaying.set(true);
    u.onend   = () => this.isPlaying.set(false);
    window.speechSynthesis.speak(u);
  }

  startRecording(): void {
    const SR = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
    if (!SR) { alert('متصفحك لا يدعم التعرف على الصوت'); return; }
    this.recognition = new SR();
    this.recognition.lang = 'ar-SA';
    this.recognition.interimResults = false;
    this.recognition.onstart = () => this.isRecording.set(true);
    this.recognition.onend   = () => this.isRecording.set(false);
    this.recognition.onresult = (e: any) => {
      const said = e.results[0][0].transcript.trim();
      const w = this.word();
      const expected = (w?.displayWord ?? '').trim();
      const saidN = this.normalizeAr(said);
      const expN  = this.normalizeAr(expected);
      // Split said into individual words and require an exact word-level match.
      // Substring .includes() is too loose — "ابدأ" contains "بدأ" as a substring.
      const saidWords = saidN.split(/\s+/).filter(Boolean);
      const isCorrect = saidWords.some(w => w === expN) || saidN === expN;
      const feedback = isCorrect ? `أحسنت! قرأت: ${said} 🌟` : `قرأت: ${said}. حاول مرة أخرى ✏️`;
      this.result.set({ isCorrect, feedback });
      this.svc.saveAttempt({
        childName: this.state.childName() ?? '',
        studentId: undefined,
        contentType: 3, // WordPractice
        contentId: w!.id,
        attemptType: 2, // Reading
        expectedText: expected,
        detectedText: said,
        score: isCorrect ? 100 : 30,
        isCorrect,
        feedbackText: feedback
      }).subscribe();
      this.speak(feedback);
    };
    this.recognition.onerror = () => { this.isRecording.set(false); };
    this.recognition.start();
  }

  stopRecording(): void { try { this.recognition?.stop(); } catch {} }

  private normalizeAr(text: string): string {
    return text
      .replace(/[ً-ٰٟ]/g, '') // strip diacritics / tashkeel
      .replace(/[أإآ]/g, 'ا')
      .replace(/ة/g, 'ه')
      .replace(/ى/g, 'ي')
      .replace(/\s+/g, ' ').trim();
  }

  submitWriting(): void {
    const w = this.word(); if (!w) return;
    const canvas = this.canvasRef.nativeElement;
    const data = this.ctx.getImageData(0, 0, canvas.width, canvas.height).data;
    let hasStrokes = false;
    for (let i = 3; i < data.length; i += 4) { if (data[i] > 50) { hasStrokes = true; break; } }
    if (!hasStrokes) return;

    const base64       = canvas.toDataURL('image/png').split(',')[1];
    const expectedText = w.displayWord;

    this.isChecking = true;
    this.svc.evaluateCanvas(base64, expectedText).subscribe({
      next: res => {
        const isCorrect    = res.isAccepted;
        const feedbackText = res.displayMessage || (isCorrect ? 'أحسنت! 🌟' : 'حاول مرة أخرى ✏️');
        this.result.set({ isCorrect, feedback: feedbackText });
        this.isChecking = false;
        this.svc.saveAttempt({
          childName:    this.state.childName() ?? '',
          studentId:    undefined,
          contentType:  3,
          contentId:    w.id,
          attemptType:  1,
          expectedText,
          detectedText: res.extractedText,
          score:        Math.round(res.similarityScore * 100),
          isCorrect,
          feedbackText
        }).subscribe();
        this.speak(res.spokenFeedback || feedbackText);
        if (isCorrect) setTimeout(() => this.stage.set('done'), 1800);
      },
      error: () => {
        this.isChecking = false;
        this.result.set({ isCorrect: false, feedback: 'حدث خطأ أثناء التقييم، حاول مرة أخرى.' });
      }
    });
  }

  goBack(): void { this.router.navigate(['/learning/words']); }
}
