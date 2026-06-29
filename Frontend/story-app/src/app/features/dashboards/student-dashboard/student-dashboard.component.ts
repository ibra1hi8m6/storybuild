import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { ProgressService, ProgressSummary } from '../../../services/progress.service';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, DecimalPipe, RouterLink, RouterLinkActive],
  templateUrl: './student-dashboard.component.html',
  styleUrl: './student-dashboard.component.css'
})
export class StudentDashboardComponent implements OnInit {
  private readonly service  = inject(StoryService);
  readonly state            = inject(AppStateService);
  private readonly router   = inject(Router);
  private readonly progress = inject(ProgressService);

  readonly isLoading       = signal(false);
  readonly data            = signal<any>(null);
  readonly progressSummary = signal<ProgressSummary | null>(null);
  nameInput                = this.state.childName() || this.state.currentUser()?.name || '';

  readonly Math     = Math;
  readonly weekDays = ['الإثنين','الثلاثاء','الأربعاء','الخميس','الجمعة','السبت','الأحد'];

  readonly weekActivity   = computed(() => this.data()?.weeklyActivity as number[] ?? [0,0,0,0,0,0,0]);
  readonly maxWeekActivity = computed(() => Math.max(...this.weekActivity(), 1));
  barHeight(v: number): number { return Math.round(v / this.maxWeekActivity() * 100); }

  readonly achievements = computed(() => {
    const d = this.data();
    if (!d) return [];
    return [
      { icon:'🔥', label:`${d.currentStreak ?? 0} أيام متتالية`, earned: (d.currentStreak ?? 0) >= 3 },
      { icon:'📚', label:'دودة كتب',     earned: (d.storiesRead ?? 0) >= 3 },
      { icon:'⭐', label:'أول نجمة',      earned: (d.stars ?? 0) >= 1 },
      { icon:'🚀', label:'القارئ السريع', earned: (d.examsCompleted ?? 0) >= 5 },
      { icon:'🎯', label:'علامة كاملة',   earned: (d.avgScore ?? 0) >= 90 },
      { icon:'🏆', label:'سيد المستوى',   earned: (d.lessonsCompleted ?? 0) >= 10 },
    ];
  });

  readonly navItems = [
    { icon:'📊', label:'لوحتي',          route:'/dashboard' },
    { icon:'✏️', label:'محتوى التعلم',  route:'/learning' },
    // { icon:'📋', label:'تقدّمي',         route:'/progress' },
    { icon:'🏆', label:'إنجازاتي',       route:'/achievements' },
    { icon:'📖', label:'قصصي',           route:'/my-stories' },
    { icon:'✨', label:'قصص ذكية',       route:'/ai-story' },
    // { icon:'📚', label:'دروسي',          route:'/my-lessons' },
    // { icon:'🎯', label:'دروس مُعيَّنة',  route:'/assigned-lessons' },
    // { icon:'📒', label:'مفرداتي',        route:'/my-journal' },
    // { icon:'🎮', label:'ألعاب اللغة',    route:'/mini-games' },
    // { icon:'💌', label:'رسائلي',         route:'/inbox' },
  ];

  readonly mobileNavItems = [
     { icon:'📊', label:'لوحتي',          route:'/dashboard' },
    { icon:'✏️', label:'محتوى التعلم',  route:'/learning' },
   
    { icon:'📖', label:'قصصي',           route:'/my-stories' },
    { icon:'✨', label:'قصص ذكية',       route:'/ai-story' },
    // { icon:'🎮', label:'ألعاب',          route:'/mini-games' },
    // { icon:'💌', label:'رسائلي',        route:'/inbox' },
    // { icon:'📒', label:'مفرداتي',       route:'/my-journal' },
  ];

  ngOnInit(): void {
    const studentId = this.state.currentUser()?.id;
    if (studentId) this.load(studentId);
  }

  load(studentId: string): void {
    if (!studentId) return;
    this.isLoading.set(true);
    this.service.getStudentDashboard(studentId).subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
    this.progress.getSummary(studentId).subscribe({
      next: s => {
        this.progressSummary.set(s);
        this.state.setCompletedIds([
          ...(s.completedLetterIds   ?? []),
          ...(s.completedWordIds     ?? []),
          ...(s.completedSentenceIds ?? []),
          ...(s.completedLessonIds   ?? []),
          ...(s.completedStoryIds    ?? []),
        ]);
      },
      error: () => {}
    });
  }

  goToQuiz1(): void { this.router.navigate(['/quiz/gate1']); }
  goToQuiz2(): void { this.router.navigate(['/quiz/gate2']); }

  pct(done: number, total: number): number {
    if (!total) return 0;
    return Math.round((done / total) * 100);
  }

  openBook(id: string):    void { this.router.navigate(['/books', id, 'read']); }
  openLesson(id: string): void { this.router.navigate(['/lessons', id]); }

  logout(): void {
    this.state.logout();
    this.router.navigate(['/auth/login']);
  }
}
