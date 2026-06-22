import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule, DecimalPipe } from '@angular/common';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { AuthService } from '../../../services/auth.service';
import { WeaknessMap, WritingAttemptHistory, ReadingAttemptHistory } from '../../../models/story.models';

@Component({
  selector: 'app-parent-dashboard',
  standalone: true,
  imports: [CommonModule, DecimalPipe, RouterLink, NavbarComponent],
  templateUrl: './parent-dashboard.component.html',
  styleUrl: './parent-dashboard.component.css'
})
export class ParentDashboardComponent implements OnInit {
  private readonly service     = inject(StoryService);
  private readonly authService = inject(AuthService);
  private readonly router      = inject(Router);

  readonly isLoading      = signal(false);
  readonly childNames     = signal<string[]>([]);
  readonly activeChild    = signal<string>('');
  readonly data           = signal<any>(null);
  readonly error          = signal<string | null>(null);
  readonly weaknessMap    = signal<WeaknessMap | null>(null);
  readonly writingHistory = signal<WritingAttemptHistory[]>([]);
  readonly readingHistory = signal<ReadingAttemptHistory[]>([]);

  readonly weekDays       = ['الاثنين','الثلاثاء','الأربعاء','الخميس','الجمعة','السبت','الأحد'];
  readonly weekActivity   = computed(() => this.data()?.weeklyActivity as number[] ?? [0,0,0,0,0,0,0]);
  readonly maxWeekActivity = computed(() => Math.max(...this.weekActivity(), 1));
  barH(v: number): number { return Math.round(v / this.maxWeekActivity() * 100); }

  readonly achievements = computed(() => {
    const d = this.data();
    if (!d) return [];
    return [
      { icon:'🔥', label:`${d.currentStreak ?? 0} أيام متتالية` },
      { icon:'📚', label:'دودة كتب',  show: (d.storiesRead ?? 0) >= 3 },
      { icon:'⭐', label:'أول نجمة',   show: (d.stars ?? 0) >= 1 },
    ].filter(a => (a as any).show !== false);
  });

  ngOnInit(): void {
    this.authService.getMyStudents().subscribe({
      next: students => {
        const names = students.map(s => s.name);
        this.childNames.set(names);
        if (names.length > 0) this.selectChild(names[0]);
      }
    });
  }

  selectChild(name: string): void {
    this.activeChild.set(name);
    this.isLoading.set(true);
    this.error.set(null);
    this.weaknessMap.set(null);
    this.writingHistory.set([]);
    this.readingHistory.set([]);
    this.service.getParentDashboard(name).subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.error.set('لم يتم العثور على بيانات.'); }
    });
    this.service.getWeaknessMap(name).subscribe({
      next:  wm => this.weaknessMap.set(wm),
      error: () => {}
    });
    this.service.getWritingHistory(name, 10).subscribe({
      next:  h => this.writingHistory.set(h),
      error: () => {}
    });
    this.service.getReadingHistory(name, 10).subscribe({
      next:  h => this.readingHistory.set(h),
      error: () => {}
    });
  }

  weakLetters(): Array<{ letter: string; pct: number }> {
    const wm = this.weaknessMap();
    if (!wm) return [];
    return Object.entries(wm.letters)
      .map(([letter, s]) => ({ letter, pct: s.attempts > 0 ? Math.round(s.correct / s.attempts * 100) : 0 }))
      .filter(e => e.pct < 70)
      .sort((a, b) => a.pct - b.pct)
      .slice(0, 6);
  }

  weakLessons(): Array<{ title: string; letter: string; pct: number }> {
    const wm = this.weaknessMap();
    if (!wm) return [];
    return Object.values(wm.lessons)
      .map(l => ({ title: l.title, letter: l.letter, pct: l.attempts > 0 ? Math.round(l.correct / l.attempts * 100) : 0 }))
      .filter(e => e.pct < 70)
      .sort((a, b) => a.pct - b.pct)
      .slice(0, 5);
  }

  avgReading(): number {
    const h = this.readingHistory();
    if (!h.length) return 0;
    return Math.round(h.reduce((s, r) => s + r.accuracyScore, 0) / h.length);
  }
  passedReading(): number { return this.readingHistory().filter(r => r.isAccepted).length; }
  avgWcpm(): number {
    const h = this.readingHistory();
    if (!h.length) return 0;
    return Math.round(h.reduce((s, r) => s + r.wcpm, 0) / h.length);
  }
  avgWriting(): number {
    const h = this.writingHistory();
    if (!h.length) return 0;
    return Math.round(h.reduce((s, w) => s + w.similarityScore, 0) / h.length);
  }
  passedWriting(): number { return this.writingHistory().filter(w => w.isAccepted).length; }

  continueLesson(id: string): void { this.router.navigate(['/books', id, 'read']); }
  skillColor(pct: number): string { return pct >= 80 ? '#22C55E' : pct >= 50 ? '#F59E0B' : '#EF4444'; }
  addChild(): void { this.router.navigate(['/auth/create-student'], { queryParams: { returnTo: 'parent' } }); }

  private readonly state = inject(AppStateService);
  logout(): void {
    this.state.logout();
    this.router.navigate(['/auth/login']);
  }
}
