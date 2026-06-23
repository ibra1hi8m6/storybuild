import {
  Component, signal, inject, OnInit, OnDestroy,
  ViewChild, ElementRef, AfterViewInit
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { LearningService } from '../../../services/learning.service';
import { AppStateService } from '../../../services/app-state-service';
import { LetterContentDto } from '../../../models/learning.models';
import { environment } from '../../../../environments/environment';

type Tool = 'pen' | 'eraser';

@Component({
  selector: 'app-letter-lesson',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  templateUrl: './letter-lesson.component.html',
  styleUrl: './letter-lesson.component.css'
})
export class LetterLessonComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('canvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly svc    = inject(LearningService);
  private readonly state  = inject(AppStateService);
  readonly api = environment.apiUrl;

  readonly letter     = signal<LetterContentDto | null>(null);
  readonly isLoading  = signal(true);
  readonly isPlaying  = signal(false);
  readonly isChecking = signal(false);
  readonly result     = signal<{ isCorrect: boolean; feedback: string } | null>(null);

  readonly activeTool  = signal<Tool>('pen');
  readonly strokeSize  = signal(6);
  readonly strokeColor = signal('#1E1B4B');

  private ctx!: CanvasRenderingContext2D;
  private drawing = false;
  private lastX = 0;
  private lastY = 0;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.svc.getLetter(id).subscribe({
      next: d  => {
        this.letter.set(d);
        this.isLoading.set(false);
        this.speak();
        // Canvas lives inside @if(letter()), so it renders after signal update.
        // A microtask delay lets Angular finish rendering before we bind events.
        setTimeout(() => this.initCanvas(), 0);
      },
      error: () => this.isLoading.set(false)
    });
  }

  ngAfterViewInit(): void {
    // Canvas may not exist yet if data hasn't loaded — initCanvas() is called
    // again after data arrives (see ngOnInit). This handles the rare case where
    // data was already cached when AfterViewInit fires.
    this.initCanvas();
  }

  ngOnDestroy(): void { window.speechSynthesis.cancel(); }

  private initCanvas(): void {
    const el = this.canvasRef?.nativeElement;
    if (!el) return;
    this.ctx = el.getContext('2d')!;
    el.width  = el.offsetWidth;
    el.height = el.offsetHeight;
    this.drawGuide();
    el.addEventListener('mousedown',  e => this.startDraw(e));
    el.addEventListener('mousemove',  e => this.draw(e));
    el.addEventListener('mouseup',    () => { this.drawing = false; });
    el.addEventListener('mouseleave', () => { this.drawing = false; });
    el.addEventListener('touchstart', e => { e.preventDefault(); this.startDrawTouch(e); }, { passive: false });
    el.addEventListener('touchmove',  e => { e.preventDefault(); this.drawTouch(e); }, { passive: false });
    el.addEventListener('touchend',   () => { this.drawing = false; });
  }

  private drawGuide(): void {
    const l = this.letter();
    if (!l || !this.ctx) return;
    const canvas = this.canvasRef.nativeElement;
    this.ctx.clearRect(0, 0, canvas.width, canvas.height);
    this.ctx.font = `bold ${Math.min(canvas.width, canvas.height) * 0.55}px 'Cairo', serif`;
    this.ctx.textAlign = 'center';
    this.ctx.textBaseline = 'middle';
    this.ctx.fillStyle = 'rgba(200,185,255,0.25)';
    this.ctx.fillText(l.writingTarget || l.letter, canvas.width / 2, canvas.height / 2);
  }

  private startDraw(e: MouseEvent): void {
    this.drawing = true;
    const r = this.canvasRef.nativeElement.getBoundingClientRect();
    this.lastX = e.clientX - r.left;
    this.lastY = e.clientY - r.top;
  }

  private startDrawTouch(e: TouchEvent): void {
    this.drawing = true;
    const r = this.canvasRef.nativeElement.getBoundingClientRect();
    this.lastX = e.touches[0].clientX - r.left;
    this.lastY = e.touches[0].clientY - r.top;
  }

  private draw(e: MouseEvent): void {
    if (!this.drawing) return;
    const r = this.canvasRef.nativeElement.getBoundingClientRect();
    this.stroke(e.clientX - r.left, e.clientY - r.top);
  }

  private drawTouch(e: TouchEvent): void {
    if (!this.drawing) return;
    const r = this.canvasRef.nativeElement.getBoundingClientRect();
    this.stroke(e.touches[0].clientX - r.left, e.touches[0].clientY - r.top);
  }

  private stroke(x: number, y: number): void {
    const ctx = this.ctx;
    ctx.beginPath();
    ctx.moveTo(this.lastX, this.lastY);
    ctx.lineTo(x, y);
    ctx.strokeStyle = this.activeTool() === 'eraser' ? '#ffffff' : this.strokeColor();
    ctx.lineWidth   = this.activeTool() === 'eraser' ? 28 : this.strokeSize();
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.stroke();
    this.lastX = x;
    this.lastY = y;
    this.result.set(null);
  }

  clearCanvas(): void {
    const canvas = this.canvasRef.nativeElement;
    this.ctx.clearRect(0, 0, canvas.width, canvas.height);
    this.drawGuide();
    this.result.set(null);
  }

  submitWriting(): void {
    const l = this.letter();
    if (!l) return;
    const canvas = this.canvasRef.nativeElement;
    if (!this.canvasHasContent(canvas)) return;

    const base64       = canvas.toDataURL('image/png').split(',')[1];
    const expectedText = l.writingTarget || l.letter;

    this.isChecking.set(true);
    this.svc.evaluateCanvas(base64, expectedText).subscribe({
      next: res => {
        const isCorrect   = res.isAccepted;
        const feedbackText = res.displayMessage || (isCorrect ? 'أحسنت! 🌟' : 'حاول مرة أخرى ✏️');
        this.result.set({ isCorrect, feedback: feedbackText });
        this.isChecking.set(false);
        this.svc.saveAttempt({
          childName:    this.state.childName() ?? '',
          studentId:    undefined,
          contentType:  1,
          contentId:    l.id,
          attemptType:  1,
          expectedText,
          detectedText: res.extractedText,
          score:        Math.round(res.similarityScore * 100),
          isCorrect,
          feedbackText
        }).subscribe();
        this.speak(res.spokenFeedback || feedbackText);
      },
      error: () => {
        this.isChecking.set(false);
        this.result.set({ isCorrect: false, feedback: 'حدث خطأ أثناء التقييم، حاول مرة أخرى.' });
      }
    });
  }

  private canvasHasContent(canvas: HTMLCanvasElement): boolean {
    const data = this.ctx.getImageData(0, 0, canvas.width, canvas.height).data;
    for (let i = 3; i < data.length; i += 4) {
      if (data[i] > 50) return true;
    }
    return false;
  }

  speak(text?: string): void {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) return;
    window.speechSynthesis.cancel();
    const l = this.letter();
    const t = text ?? l?.audioText ?? l?.displaySentence ?? '';
    if (!t) return;
    const u = new SpeechSynthesisUtterance(t);
    u.lang = 'ar-SA'; u.rate = 0.85;
    u.onstart = () => this.isPlaying.set(true);
    u.onend   = () => this.isPlaying.set(false);
    window.speechSynthesis.speak(u);
  }

  goBack(): void { this.router.navigate(['/learning/letters']); }
}
