import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { StoryService } from '../../services/story';
import { AppStateService } from '../../services/app-state-service';
import { ProgressService } from '../../services/progress.service';
import { UploadedStoryDto } from '../../models/story-content.models';
import { ListenModeComponent } from '../fluency/reading-journey/listen-mode.component';
import { RecordModeComponent } from '../fluency/reading-journey/record-mode.component';
import { environment } from '../../../environments/environment';

type JourneyMode = 'listen' | 'record';

@Component({
  selector: 'app-uploaded-story-journey',
  standalone: true,
  imports: [CommonModule, ListenModeComponent, RecordModeComponent],
  templateUrl: './uploaded-story-journey.component.html',
  styleUrls: ['./uploaded-story-journey.component.css']
})
export class UploadedStoryJourneyComponent implements OnInit {
  private route    = inject(ActivatedRoute);
  private router   = inject(Router);
  private location = inject(Location);
  private svc      = inject(StoryService);
  private state       = inject(AppStateService);
  private progressSvc = inject(ProgressService);

  readonly id         = signal('');
  readonly pages      = signal<{ pageId: string; sentence: string; imageUrl: string; pageNumber: number }[]>([]);
  readonly pageIdx    = signal(0);
  readonly mode       = signal<JourneyMode>('listen');
  readonly isLoading  = signal(true);
  readonly allStories = signal<UploadedStoryDto[]>([]);

  readonly currentPage = computed(() => this.pages()[this.pageIdx()]);
  readonly totalPages  = computed(() => this.pages().length);
  readonly isLastPage  = computed(() => this.totalPages() > 0 && this.pageIdx() + 1 === this.totalPages());
  readonly nextLabel   = computed(() => this.isLastPage() ? 'انتقل إلى القصة التالية' : 'انتقل إلى الصفحة التالية');
  readonly progress    = computed(() =>
    this.totalPages() > 0 ? ((this.pageIdx() + 1) / this.totalPages()) * 100 : 0
  );

  async ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    // Load the stories list once — reused across in-place story transitions
    const allStories = await firstValueFrom(this.svc.getUploadedStories());
    this.allStories.set(allStories);
    await this.loadStory(id);
  }

  private async loadStory(id: string) {
    this.id.set(id);
    this.pageIdx.set(0);
    this.mode.set('listen');
    this.pages.set([]);
    this.isLoading.set(true);
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

  async goNextPage() {
    const next = this.pageIdx() + 1;
    if (next < this.totalPages()) {
      this.pageIdx.set(next);
      this.mode.set('listen');
      return;
    }
    // Last page — record this uploaded story as completed
    const studentId = this.state.currentUser()?.id;
    const storyId   = this.id();
    if (studentId && storyId) {
      this.progressSvc.completeStory(studentId, storyId).subscribe();
    }
    // Load next story in place (no component recreation)
    const stories = this.allStories();
    const currentIdx = stories.findIndex(s => s.id === this.id());
    const nextStory = currentIdx >= 0 ? stories[currentIdx + 1] : undefined;
    if (nextStory) {
      this.location.go(`/uploaded-stories/${nextStory.id}/journey`);
      await this.loadStory(nextStory.id);
    } else {
      this.router.navigate(['/uploaded-stories']);
    }
  }

  goBack() { this.router.navigate(['/uploaded-stories']); }
}
