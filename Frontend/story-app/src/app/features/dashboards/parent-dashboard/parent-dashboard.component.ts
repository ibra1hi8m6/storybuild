import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { AuthService } from '../../../services/auth.service';
import { SubscriptionService, MySubscription, PLAN_LABELS } from '../../../services/subscription.service';
import { WeaknessMap, WritingAttemptHistory, ReadingAttemptHistory } from '../../../models/story.models';


@Component({
  selector: 'app-parent-dashboard',
  standalone: true,
  imports: [CommonModule, DecimalPipe, RouterLink, NavbarComponent, FormsModule],
  templateUrl: './parent-dashboard.component.html',
  styleUrl: './parent-dashboard.component.css'
})
export class ParentDashboardComponent implements OnInit {
  private readonly service      = inject(StoryService);
  private readonly authService  = inject(AuthService);
  private readonly subService   = inject(SubscriptionService);
  private readonly router       = inject(Router);

  readonly isLoading         = signal(false);
  readonly students          = signal<{ id: string; name: string }[]>([]);
  readonly activeChild       = signal<string>('');
  readonly activeChildId     = signal<string>('');
  readonly data              = signal<any>(null);
  readonly error             = signal<string | null>(null);
  readonly weaknessMap       = signal<WeaknessMap | null>(null);
  readonly writingHistory    = signal<WritingAttemptHistory[]>([]);
  readonly readingHistory    = signal<ReadingAttemptHistory[]>([]);
  readonly showDeleteConfirm = signal(false);
  readonly deleteLoading     = signal(false);

  readonly subscription      = signal<MySubscription | null>(null);
  readonly activationCode    = signal('');
  readonly isActivating      = signal(false);
  readonly activationError   = signal<string | null>(null);
  readonly activationSuccess = signal<string | null>(null);

  readonly parentName = computed(() => this.state.currentUser()?.name ?? 'ولي الأمر');
  readonly atChildLimit = computed(() => {
    const s = this.subscription();
    const max = s?.maxChildren ?? 1;
    const count = s?.childrenCount ?? this.students().length;
    return count >= max;
  });

  readonly planLabel = computed(() => {
    const s = this.subscription();
    return s ? (PLAN_LABELS[s.activePlan] ?? s.activePlan) : '—';
  });

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
    this.subService.getMySubscription().subscribe({
      next: s => this.subscription.set(s),
      error: () => {}
    });
    this.authService.getMyStudents().subscribe({
      next: studentList => {
        const unique = studentList
          .filter((s, i, arr) => arr.findIndex(x => x.id === s.id) === i)
          .map(s => ({ id: s.id, name: s.name }));
        this.students.set(unique);
        if (unique.length > 0) this.selectChild(unique[0].id, unique[0].name);
      }
    });
  }

  activateCode(): void {
    const code = this.activationCode().trim();
    if (!code) return;
    this.isActivating.set(true);
    this.activationError.set(null);
    this.activationSuccess.set(null);
    this.subService.activate(code).subscribe({
      next: res => {
        this.isActivating.set(false);
        this.activationSuccess.set(res.message);
        this.activationCode.set('');
        this.subService.getMySubscription().subscribe({ next: s => this.subscription.set(s), error: () => {} });
      },
      error: (err: Error) => {
        this.isActivating.set(false);
        this.activationError.set(err.message);
      }
    });
  }

  selectChild(studentId: string, name: string): void {
    this.activeChild.set(name);
    this.activeChildId.set(studentId);
    this.isLoading.set(true);
    this.error.set(null);
    this.weaknessMap.set(null);
    this.writingHistory.set([]);
    this.readingHistory.set([]);
    this.service.getParentDashboard(studentId).subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.error.set('لم يتم العثور على بيانات.'); }
    });
    this.service.getWeaknessMap(studentId).subscribe({
      next:  wm => this.weaknessMap.set(wm),
      error: () => {}
    });
    this.service.getWritingHistory(studentId, 10).subscribe({
      next:  h => this.writingHistory.set(h),
      error: () => {}
    });
    this.service.getReadingHistory(studentId, 10).subscribe({
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

  confirmDelete(): void  { this.showDeleteConfirm.set(true); }
  cancelDelete(): void   { this.showDeleteConfirm.set(false); }
  deleteStudent(): void {
    const studentId = this.activeChildId();
    if (!studentId) return;
    this.deleteLoading.set(true);
    this.authService.deleteStudent(studentId).subscribe({
      next: () => {
        const remaining = this.students().filter(s => s.id !== studentId);
        this.students.set(remaining);
        this.showDeleteConfirm.set(false);
        this.deleteLoading.set(false);
        this.data.set(null);
        this.activeChild.set('');
        this.activeChildId.set('');
        if (remaining.length > 0) this.selectChild(remaining[0].id, remaining[0].name);
      },
      error: () => { this.deleteLoading.set(false); this.showDeleteConfirm.set(false); }
    });
  }

  private readonly state = inject(AppStateService);
  logout(): void {
    this.state.logout();
    this.router.navigate(['/auth/login']);
  }
}
