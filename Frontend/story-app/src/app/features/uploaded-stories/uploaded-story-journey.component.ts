import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { StoryService } from '../../services/story';
import { ListenModeComponent } from '../fluency/reading-journey/listen-mode.component';
import { RecordModeComponent } from '../fluency/reading-journey/record-mode.component';
import { environment } from '../../../environments/environment';

type JourneyMode = 'listen' | 'read' | 'record';

@Component({
  selector: 'app-uploaded-story-journey',
  standalone: true,
  imports: [CommonModule, ListenModeComponent, RecordModeComponent],
  templateUrl: './uploaded-story-journey.component.html',
  styleUrls: ['./uploaded-story-journey.component.css']
})
export class UploadedStoryJourneyComponent implements OnInit {
  private route  = inject(ActivatedRoute);
  private router = inject(Router);
  private svc    = inject(StoryService);

  readonly id       = signal('');
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
    this.id.set(id);
    try {
      const story = await firstValueFrom(this.svc.getUploadedStory(id));
      this.pages.set(story.pages.map(p => ({
        pageId:     p.pageId,
        sentence:   p.sentence?.trim() ?? '',
        imageUrl:   p.imageUrl,
        pageNumber: p.pageNumber
      })));
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
      this.router.navigate(['/exam'], { queryParams: { storyId: this.id() } });
    }
  }

  goBack() { this.router.navigate(['/uploaded-stories']); }
}
