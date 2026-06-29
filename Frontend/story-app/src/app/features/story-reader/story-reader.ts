import {
  Component, signal, computed, inject, OnInit, OnDestroy, HostListener
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { StoryService } from '../../services/story';
import { AppStateService } from '../../services/app-state-service';
import { TtsService } from '../../services/tts.service';
import { ProgressService } from '../../services/progress.service';
import { environment } from '../../../environments/environment';
import { ListenModeComponent } from '../fluency/reading-journey/listen-mode.component';
import { RecordModeComponent } from '../fluency/reading-journey/record-mode.component';

@Component({
  selector: 'app-story-reader',
  standalone: true,
  imports: [CommonModule, ListenModeComponent, RecordModeComponent],
  templateUrl: './story-reader.html',
  styleUrl: './story-reader.css'
})
export class StoryReaderComponent implements OnInit, OnDestroy {
  private readonly router       = inject(Router);
  private readonly route        = inject(ActivatedRoute);
  private readonly storyService = inject(StoryService);
  private readonly state        = inject(AppStateService);
  private readonly tts          = inject(TtsService);
  private readonly progress     = inject(ProgressService);

  readonly isLoading   = signal(false);
  readonly story       = signal<any>(null);
  readonly pageNum     = signal(1);
  readonly isPlaying   = signal(false);
  readonly imageLoaded = signal(false);
  readonly storyId     = signal('');
  readonly readingTab  = signal<'listen' | 'read' | 'record'>('listen');

  readonly activePage = computed(() => {
    const s = this.story();
    if (!s?.pages) return null;
    return s.pages.find((p: any) => p.pageNumber === this.pageNum()) ?? null;
  });
  readonly totalPages = computed(() => this.story()?.pages?.length ?? 0);
  readonly isFirst    = computed(() => this.pageNum() === 1);
  readonly isLast     = computed(() => this.pageNum() === this.totalPages());
  readonly dots       = computed(() =>
    Array.from({ length: this.totalPages() }, (_, i) => i + 1)
  );

  private returnTo = '/levels';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.router.navigate(['/levels']); return; }
    this.returnTo = this.route.snapshot.queryParamMap.get('returnTo') ?? '/levels';
    this.storyId.set(id);
    this.loadStory(id);
  }

  ngOnDestroy(): void { this.tts.stop(); }

  @HostListener('window:keydown', ['$event'])
  onKey(e: KeyboardEvent): void {
    const tag = (e.target as HTMLElement)?.tagName;
    if (['INPUT', 'TEXTAREA'].includes(tag)) return;
    if (e.key === 'ArrowRight') this.prev();
    if (e.key === 'ArrowLeft')  this.next();
    if (e.key === ' ') { e.preventDefault(); this.playAudio(); }
  }

  private loadStory(id: string): void {
    const cached = this.state.currentStory();
    if (cached && cached.id === id) { this.story.set(cached); return; }
    this.isLoading.set(true);
    this.storyService.getStory(id).subscribe({
      next: s => {
        this.story.set(s);
        this.state.setStory(s);
        this.isLoading.set(false);
        // source 0 = AiGenerated (no cover) → start at page 1
        // source 1 = PdfImport (has cover)  → start at page 2
        const isPdfImport = s.source === 1;
        if (isPdfImport && (s.pages?.length ?? 0) > 1) this.pageNum.set(2);
      },
      error: () => { this.isLoading.set(false); this.router.navigate(['/levels']); }
    });
  }

  prev(): void {
    if (this.isFirst()) return;
    this.tts.stop();
    this.isPlaying.set(false);
    this.imageLoaded.set(false);
    this.readingTab.set('listen');
    this.pageNum.update(p => p - 1);
  }

  next(): void {
    if (this.isLast()) {
      // Only record completion for uploaded PDF stories (source=1).
      // AI-generated stories (source=0) are for fun and must not count as progress.
      const isPreview  = this.returnTo !== '/levels';
      const isPdfStory = this.story()?.source === 1;
      if (!isPreview && isPdfStory) {
        const studentId = this.state.currentUser()?.id;
        const storyId   = this.storyId();
        if (studentId && storyId) {
          this.progress.completeStory(studentId, storyId).subscribe();
        }
      }
      this.router.navigate([this.returnTo]);
      return;
    }
    this.tts.stop();
    this.isPlaying.set(false);
    this.imageLoaded.set(false);
    this.readingTab.set('listen');
    this.pageNum.update(p => p + 1);
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

  private async speakText(text: string): Promise<void> {
    if (!text) return;
    this.isPlaying.set(true);
    try { await this.tts.play(text); }
    finally { this.isPlaying.set(false); }
  }

  imageUrl(url: string): string {
    if (!url) return '';
    return url.startsWith('http') ? url : `${environment.apiUrl}${url}`;
  }

  onImgLoad(): void { this.imageLoaded.set(true); }
  goBack(): void { this.router.navigate([this.returnTo]); }
}
