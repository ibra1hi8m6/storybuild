import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { StoryService } from '../../services/story';
import { AppStateService } from '../../services/app-state-service';

@Component({
  selector: 'app-lesson-complete',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './lesson-complete.component.html'
})
export class LessonCompleteComponent implements OnInit {
  private readonly route   = inject(ActivatedRoute);
  private readonly router  = inject(Router);
  private readonly service = inject(StoryService);
  private readonly state   = inject(AppStateService);

  readonly lessonId    = signal('');
  readonly lessonTitle = signal('');
  readonly level       = signal(1);
  readonly stars       = signal(0);
  readonly isLoading   = signal(true);

  readonly starsArr = computed(() => Array.from({ length: 3 }, (_, i) => i < this.stars()));

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.lessonId.set(id);

    const lessonInState = this.state.currentLesson();
    if (lessonInState) {
      this.lessonTitle.set(lessonInState.title ?? '');
      this.level.set(lessonInState.level ?? 1);
    }

    const childName = this.state.childName();
    if (childName && id) {
      this.service.getStudentDashboard(childName).subscribe({
        next: d => {
          this.stars.set(d?.stars ?? 0);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      });
    } else {
      this.isLoading.set(false);
    }
  }

  goToExam(): void {
    this.router.navigate(['/exam'], { queryParams: { lessonId: this.lessonId() } });
  }

  goToBooks(): void {
    this.router.navigate(['/levels', this.level(), 'books']);
  }
}
