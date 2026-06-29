import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { TeacherSidebarComponent } from '../teacher-shell/teacher-sidebar.component';
import {
  AnalyticsSummaryDto, StudentAnalyticsDto, WeakLetterDto, TeacherAssignmentOverview
} from '../../../models/story.models';

@Component({
  selector: 'app-teacher-analytics',
  standalone: true,
  imports: [CommonModule, RouterLink, TeacherSidebarComponent],
  templateUrl: './teacher-analytics.component.html',
  styleUrl: './teacher-analytics.component.css'
})
export class TeacherAnalyticsComponent implements OnInit {
  private readonly svc   = inject(StoryService);
  private readonly state = inject(AppStateService);

  readonly isLoading   = signal(false);
  readonly summary     = signal<AnalyticsSummaryDto | null>(null);
  readonly overview    = signal<TeacherAssignmentOverview[]>([]);
  readonly activeTab   = signal<'analytics' | 'assignments'>('analytics');
  readonly error       = signal<string | null>(null);

  readonly weakLettersSorted = computed(() =>
    (this.summary()?.mostCommonWeakLetters ?? []).slice(0, 10)
  );

  readonly maxAttempts = computed(() =>
    Math.max(...this.weakLettersSorted().map(w => w.attempts), 1)
  );

  barW(attempts: number): number {
    return Math.round(attempts / this.maxAttempts() * 100);
  }

  accuracyColor(acc: number): string {
    return acc >= 80 ? '#22c55e' : acc >= 50 ? '#f59e0b' : '#ef4444';
  }

  ngOnInit(): void {
    const user = this.state.currentUser();
    if (!user?.id) return;
    this.load(user.id);
  }

  private load(teacherId: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.svc.getClassAnalytics(teacherId).subscribe({
      next:  s => { this.summary.set(s); this.isLoading.set(false); },
      error: () => { this.error.set('تعذّر تحميل التحليلات.'); this.isLoading.set(false); }
    });

    this.svc.getTeacherAssignmentOverview(teacherId).subscribe({
      next:  o => this.overview.set(o),
      error: () => {}
    });
  }
}
