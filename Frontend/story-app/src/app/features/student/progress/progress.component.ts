import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AppStateService } from '../../../services/app-state-service';
import { ProgressService, ProgressSummary } from '../../../services/progress.service';

@Component({
  selector: 'app-progress',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './progress.component.html',
})
export class ProgressComponent implements OnInit {
  private readonly progress = inject(ProgressService);
  readonly state            = inject(AppStateService);
  private readonly router   = inject(Router);

  readonly isLoading = signal(false);
  readonly data      = signal<ProgressSummary | null>(null);
  readonly error     = signal<string | null>(null);

  readonly navItems = [
    { icon: '📊', label: 'لوحتي',         route: '/dashboard' },
    { icon: '✏️', label: 'محتوى التعلم',  route: '/learning' },
    // { icon: '📋', label: 'تقدّمي',         route: '/progress' },
    { icon: '🏆', label: 'إنجازاتي',      route: '/achievements' },
    { icon: '📖', label: 'قصصي',          route: '/my-stories' },
    { icon: '✨', label: 'قصص ذكية',      route: '/ai-story' },
  ];

  readonly sections = [
    { icon: '✏️', label: 'الحروف',   completedKey: 'lettersCompleted'   as const, totalKey: 'lettersTotal'   as const, color: '#F4788A' },
    { icon: '📝', label: 'الكلمات',  completedKey: 'wordsCompleted'     as const, totalKey: 'wordsTotal'     as const, color: '#8B5CF6' },
    { icon: '💬', label: 'الجمل',    completedKey: 'sentencesCompleted' as const, totalKey: 'sentencesTotal' as const, color: '#0EA5E9' },
    { icon: '📚', label: 'الدروس',   completedKey: 'lessonsCompleted'   as const, totalKey: 'lessonsTotal'   as const, color: '#10B981' },
    { icon: '📖', label: 'القصص',    completedKey: 'storiesCompleted'   as const, totalKey: 'storiesTotal'   as const, color: '#F59E0B' },
  ];

  ngOnInit(): void {
    const studentId = this.state.currentUser()?.id;
    if (!studentId) { this.router.navigate(['/dashboard']); return; }
    this.isLoading.set(true);
    this.progress.getSummary(studentId).subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.error.set('لم يتم تحميل البيانات.'); }
    });
  }

  pct(completed: number, total: number): number {
    return total > 0 ? Math.round(completed / total * 100) : 0;
  }

  logout(): void { this.state.logout(); this.router.navigate(['/auth/login']); }
}
