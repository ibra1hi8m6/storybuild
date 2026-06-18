import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { ListenModeComponent } from './listen-mode.component';
import { RecordModeComponent } from './record-mode.component';
import { environment } from '../../../../environments/environment';

type JourneyMode = 'listen' | 'read' | 'record';

// Strip non-Arabic characters — keeps Arabic letters, diacritics, punctuation, spaces
function arabicOnly(text: string): string {
  return text
    .replace(/[a-zA-Z]+/g, '')          // remove English words
    .replace(/\s{2,}/g, ' ')            // collapse multiple spaces
    .trim();
}

@Component({
  selector: 'app-reading-journey-host',
  standalone: true,
  imports: [CommonModule, ListenModeComponent, RecordModeComponent],
  templateUrl: './reading-journey-host.component.html',
  styleUrls: ['./reading-journey-host.component.css']
})
export class ReadingJourneyHostComponent implements OnInit {
  private route  = inject(ActivatedRoute);
  private router = inject(Router);
  private story  = inject(StoryService);
  private state  = inject(AppStateService);

  readonly id       = signal('');
  readonly pageType = signal<'Story' | 'Lesson'>('Story');
  readonly pages    = signal<{ pageId: string; sentence: string; imageUrl: string; pageNumber: number }[]>([]);
  readonly pageIdx  = signal(0);
  readonly mode     = signal<JourneyMode>('listen');
  readonly isLoading = signal(true);

  readonly currentPage = computed(() => this.pages()[this.pageIdx()]);
  readonly totalPages  = computed(() => this.pages().length);
  readonly progress    = computed(() =>
    this.totalPages() > 0 ? ((this.pageIdx() + 1) / this.totalPages()) * 100 : 0
  );

  async ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    const type = (this.route.snapshot.data['pageType'] ?? 'Story') as 'Story' | 'Lesson';
    this.id.set(id);
    this.pageType.set(type);

    try {
      if (type === 'Story') {
        const s = await firstValueFrom(this.story.getStory(id));
        this.pages.set(s.pages.map(p => ({
          pageId:     p.pageId,
          sentence:   arabicOnly(p.sentence),
          imageUrl:   p.imageUrl,
          pageNumber: p.pageNumber
        })));
      } else {
        const l = await firstValueFrom(this.story.getLesson(id));
        this.pages.set(l.pages
          .filter(p => !p.isCoverPage)
          .filter(p => arabicOnly(p.sentence).trim().length > 0)  // skip pages with no Arabic
          .map(p => ({
            pageId:     p.pageId,
            sentence:   arabicOnly(p.sentence),
            imageUrl:   p.imageUrl,
            pageNumber: p.pageNumber
          })));
      }
    } finally {
      this.isLoading.set(false);
    }
  }

  resolveUrl(url: string): string {
    if (!url) return '';
    return url.startsWith('http') ? url : `${environment.apiUrl}${url}`;
  }

  setMode(m: JourneyMode) { this.mode.set(m); }

  onRecordPassed() {
    const next = this.pageIdx() + 1;
    if (next < this.totalPages()) {
      this.pageIdx.set(next);
      this.mode.set('listen');
    } else {
      // All pages done — navigate to mini-games
      this.router.navigate(['/mini-games'], {
        queryParams: { from: this.pageType(), id: this.id() }
      });
    }
  }

  goBack() {
    const type = this.pageType();
    if (type === 'Story') this.router.navigate(['/books', this.id(), 'read']);
    else this.router.navigate(['/lessons', this.id()]);
  }
}
