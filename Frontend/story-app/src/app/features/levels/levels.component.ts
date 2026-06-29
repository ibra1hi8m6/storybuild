import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AppStateService } from '../../services/app-state-service';
import { ProgressService, ProgressSummary } from '../../services/progress.service';

@Component({
  selector: 'app-levels',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './levels.component.html',
  styleUrl: './levels.component.css'
})
export class LevelsComponent implements OnInit {
  private readonly router   = inject(Router);
  private readonly progress = inject(ProgressService);
  readonly state            = inject(AppStateService);

  readonly navItems = [
    { icon: '📊', label: 'لوحتي',          route: '/dashboard' },
    { icon: '✏️', label: 'محتوى التعلم',   route: '/learning' },
    // { icon: '📋', label: 'تقدّمي',          route: '/progress' },
    { icon: '🏆', label: 'إنجازاتي',       route: '/achievements' },
    { icon: '📖', label: 'قصصي',           route: '/my-stories' },
    { icon: '✨', label: 'قصص ذكية',       route: '/ai-story' },
  ];

  readonly isLoading = signal(false);
  readonly summary   = signal<ProgressSummary | null>(null);

  readonly studentLevel = computed(() => this.state.currentUser()?.level ?? 1);

  ngOnInit(): void {
    const studentId = this.state.currentUser()?.id;
    if (!studentId) return;
    this.isLoading.set(true);
    this.progress.getSummary(studentId).subscribe({
      next:  s => { this.summary.set(s); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  pct(done: number, total: number): number {
    if (!total) return 0;
    return Math.round((done / total) * 100);
  }

  go(route: string): void { this.router.navigate([route]); }

  logout(): void { this.state.logout(); this.router.navigate(['/auth/login']); }
}
