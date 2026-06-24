import {
  Component, signal, computed, inject,
  OnInit, OnDestroy, AfterViewInit,
  ViewChild, ElementRef, HostListener
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { StoryService } from '../../services/story';
import { AppStateService } from '../../services/app-state-service';
import { TtsService } from '../../services/tts.service';
import { WritingCorrectionResponse } from '../../models/story.models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-lesson-reader',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './lesson-reader.html',
  styleUrl: './lesson-reader.css'
})
export class LessonReaderComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('drawingCanvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly router  = inject(Router);
  private readonly route   = inject(ActivatedRoute);
  private readonly service = inject(StoryService);
  private readonly state   = inject(AppStateService);
  private readonly tts     = inject(TtsService);

  readonly isLoading            = signal(false);
  readonly lesson               = signal<any>(null);
  readonly pageNum              = signal(1);
  readonly isPlaying            = signal(false);
  readonly imageLoaded          = signal(false);
  readonly tool                 = signal<'pencil' | 'eraser'>('pencil');
  readonly hasDrawing           = signal(false);
  readonly isChecking           = signal(false);
  readonly checkResult          = signal<WritingCorrectionResponse | null>(null);
  readonly writingCheckedOnPage = signal(false);
  readonly showWritingWarning   = signal(false);

  private ctx!: CanvasRenderingContext2D;
  private isDrawing = false;

  readonly activePage = computed(() => {
    const l = this.lesson();
    if (!l) return null;
    return l.pages?.find((p: any) => p.pageNumber === this.pageNum()) ?? null;
  });

  readonly totalPages  = computed(() => this.lesson()?.pages?.length ?? 0);
  readonly isFirst     = computed(() => this.pageNum() === 1);
  readonly isLast      = computed(() => this.pageNum() === this.totalPages());
  readonly progressPct = computed(() =>
    this.totalPages() > 0 ? Math.round(this.pageNum() / this.totalPages() * 100) : 0
  );

  readonly pageNeedsWriting = computed(() => {
    const p = this.activePage();
    return !!p?.sentence && !p?.isCoverPage;
  });

  readonly canGoNext = computed(() =>
    !this.pageNeedsWriting() || this.writingCheckedOnPage()
  );

  private lessonId = '';

  private canvasReady = false;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.router.navigate(['/levels']); return; }
    this.lessonId = id;
    this.isLoading.set(true);
    this.service.getLesson(id).subscribe({
      next: l => {
        this.lesson.set(l);
        this.state.setLesson(l);

        // Resume from last saved page
        const saved = this.state.lessonProgress(id);
        const resumePage = (saved && saved.currentPage > 1 && saved.currentPage <= (l.pages?.length ?? 1))
          ? saved.currentPage : 1;
        this.pageNum.set(resumePage);

        this.saveProgress(resumePage, l.pages?.length ?? 0, false);
        this.isLoading.set(false);
        setTimeout(() => {
          this.setupCanvas();
          this.autoPlayCurrentPage();
        }, 300);
      },
      error: () => { this.isLoading.set(false); this.router.navigate(['/levels']); }
    });
  }

  ngAfterViewInit(): void { /* canvas set up after lesson loads */ }
  ngOnDestroy(): void { this.tts.stop(); }

  @HostListener('window:keydown', ['$event'])
  onKey(e: KeyboardEvent): void {
    if (e.key === 'ArrowRight') this.prev();
    if (e.key === 'ArrowLeft')  this.next();
  }

  prev(): void {
    if (this.isFirst()) return;
    this.pageNum.update(p => p - 1);
    this.imageLoaded.set(false);
    this.checkResult.set(null);
    this.writingCheckedOnPage.set(false);
    this.showWritingWarning.set(false);
    this.clearCanvas();
    this.tts.stop();
    this.isPlaying.set(false);
    setTimeout(() => this.autoPlayCurrentPage(), 200);
  }

  next(): void {
    if (!this.canGoNext()) {
      this.showWritingWarning.set(true);
      setTimeout(() => this.showWritingWarning.set(false), 3000);
      return;
    }
    this.showWritingWarning.set(false);

    const page = this.activePage();
    if (page?.pageId) {
      this.service.markPageDone(
        this.state.childName() || 'طالب',
        this.lessonId,
        page.pageId,
        this.writingCheckedOnPage()
      ).subscribe();
    }

    if (this.isLast()) {
      this.saveProgress(this.pageNum(), this.totalPages(), true);
      this.router.navigate(['/lessons', this.lessonId, 'complete']);
      return;
    }
    const next = this.pageNum() + 1;
    this.pageNum.set(next);
    this.writingCheckedOnPage.set(false);
    this.saveProgress(next, this.totalPages(), false);
    this.imageLoaded.set(false);
    this.clearCanvas();
    this.tts.stop();
    this.isPlaying.set(false);
    setTimeout(() => this.autoPlayCurrentPage(), 200);
  }

  private saveProgress(page: number, total: number, completed: boolean): void {
    if (this.lessonId)
      this.state.saveLessonProgress(this.lessonId, page, total, completed);
  }

  playAudio(): void {
    const page = this.activePage();
    if (!page) return;
    if (this.isPlaying()) {
      this.tts.stop();
      this.isPlaying.set(false);
      return;
    }
    void this.speakText(page.sentence);
  }

  private autoPlayCurrentPage(): void {
    const page = this.activePage();
    if (!page?.sentence) return;
    void this.speakText(page.sentence);
  }

  private async speakText(text: string): Promise<void> {
    if (!text) return;
    this.isPlaying.set(true);
    try { await this.tts.play(text); }
    finally { this.isPlaying.set(false); }
  }

  speakFeedback(text: string): void {
    void this.speakText(text);
  }

  private setupCanvas(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return;
    if (this.canvasReady) { this.drawBackground(); return; }
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    this.ctx = ctx;
    this.canvasReady = true;
    this.drawBackground();

    canvas.addEventListener('pointerdown', (e: PointerEvent) => {
      canvas.setPointerCapture(e.pointerId);
      const { x, y } = this.pos(e, canvas);
      this.isDrawing = true;
      this.ctx.beginPath(); this.ctx.moveTo(x, y);
    });
    canvas.addEventListener('pointermove', (e: PointerEvent) => {
      if (!this.isDrawing) return;
      const { x, y } = this.pos(e, canvas);
      if (this.tool() === 'eraser') {
        this.ctx.clearRect(x - 15, y - 15, 30, 30);
      } else {
        this.ctx.strokeStyle = '#1A1A2E'; this.ctx.lineWidth = 4;
        this.ctx.lineCap = 'round'; this.ctx.lineJoin = 'round';
        this.ctx.lineTo(x, y); this.ctx.stroke();
        this.hasDrawing.set(true);
      }
    });
    canvas.addEventListener('pointerup',     () => { this.isDrawing = false; this.ctx.beginPath(); });
    canvas.addEventListener('pointercancel', () => { this.isDrawing = false; });
  }

  private drawBackground(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas || !this.ctx) return;

    // Clear to white
    this.ctx.fillStyle = '#fff';
    this.ctx.fillRect(0, 0, canvas.width, canvas.height);

    this.ctx.save();

    // ── Dashed baseline guide ──────────────────────────────────────────────────
    this.ctx.setLineDash([8, 10]);
    this.ctx.strokeStyle = 'rgba(244,120,138,0.25)';
    this.ctx.lineWidth = 1.5;
    this.ctx.beginPath();
    this.ctx.moveTo(30, canvas.height * 0.72);
    this.ctx.lineTo(canvas.width - 30, canvas.height * 0.72);
    this.ctx.stroke();
    this.ctx.setLineDash([]);

    // ── Sentence as traceable background text ─────────────────────────────────
    const sentence = this.activePage()?.sentence ?? '';
    if (sentence) {
      // Fit sentence into canvas width by adjusting font size
      let fontSize = Math.round(canvas.height * 0.52);
      this.ctx.font      = `bold ${fontSize}px Amiri, serif`;
      this.ctx.direction = 'rtl';
      while (fontSize > 18 && this.ctx.measureText(sentence).width > canvas.width - 60) {
        fontSize -= 2;
        this.ctx.font = `bold ${fontSize}px Amiri, serif`;
      }
      this.ctx.textAlign    = 'center';
      this.ctx.textBaseline = 'alphabetic';
      this.ctx.fillStyle    = 'rgba(244,120,138,0.12)';
      this.ctx.fillText(sentence, canvas.width / 2, canvas.height * 0.78);

      // Dashed stroke so child can trace
      this.ctx.setLineDash([5, 6]);
      this.ctx.strokeStyle = 'rgba(244,120,138,0.22)';
      this.ctx.lineWidth   = 1.5;
      this.ctx.strokeText(sentence, canvas.width / 2, canvas.height * 0.78);
      this.ctx.setLineDash([]);
    } else {
      // Fallback: draw the letter watermark when no sentence
      const letter = this.lesson()?.letter ?? '';
      if (letter) {
        const fontSize = Math.round(canvas.height * 0.78);
        this.ctx.font          = `bold ${fontSize}px Amiri, serif`;
        this.ctx.textAlign     = 'center';
        this.ctx.textBaseline  = 'alphabetic';
        this.ctx.direction     = 'rtl';
        this.ctx.fillStyle     = 'rgba(244,120,138,0.13)';
        this.ctx.fillText(letter, canvas.width / 2, canvas.height * 0.82);
        this.ctx.setLineDash([6, 7]);
        this.ctx.strokeStyle = 'rgba(244,120,138,0.28)';
        this.ctx.lineWidth   = 2;
        this.ctx.strokeText(letter, canvas.width / 2, canvas.height * 0.82);
        this.ctx.setLineDash([]);
      }
    }

    this.ctx.restore();
  }

  clearCanvas(): void {
    this.hasDrawing.set(false);
    this.checkResult.set(null);
    this.drawBackground();
  }

  checkWriting(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas || !this.hasDrawing()) return;
    const page = this.activePage();
    if (!page) return;

    this.isChecking.set(true);
    this.checkResult.set(null);

    canvas.toBlob(blob => {
      if (!blob) { this.isChecking.set(false); return; }
      this.service.submitLessonWriting(
        this.lessonId,
        page.pageId,
        this.state.childName() || 'طالب',
        blob
      ).subscribe({
        next:  r => {
          this.checkResult.set(r);
          this.isChecking.set(false);
          this.writingCheckedOnPage.set(true);
          const parts: string[] = [];
          if (r.spokenFeedback || r.displayMessage) parts.push(r.spokenFeedback || r.displayMessage);
          if (r.mistakes?.length) parts.push(...r.mistakes.map((m: any) => m.description).filter(Boolean));
          if (r.tips?.length)     parts.push(...r.tips.filter(Boolean));
          const tts = parts.join('. ');
          if (tts) setTimeout(() => this.speakFeedback(tts), 400);
        },
        error: () => { this.isChecking.set(false); }
      });
    }, 'image/png');
  }

  private pos(e: PointerEvent, canvas: HTMLCanvasElement) {
    const r = canvas.getBoundingClientRect();
    return {
      x: (e.clientX - r.left) * (canvas.width  / r.width),
      y: (e.clientY - r.top)  * (canvas.height / r.height)
    };
  }

  imageUrl(url: string): string {
    if (!url) return '';
    return url.startsWith('http') ? url : `${environment.apiUrl}${url}`;
  }

  onImgLoad(): void { this.imageLoaded.set(true); }
  goBack(): void {
    const level = this.lesson()?.level ?? 1;
    this.router.navigate(['/levels', level, 'books']);
  }

  pageDotsArr(): number[] {
    return Array.from({ length: this.totalPages() }, (_, i) => i + 1);
  }
}
