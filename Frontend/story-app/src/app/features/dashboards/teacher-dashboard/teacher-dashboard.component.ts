import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { TeacherSidebarComponent } from '../../teacher/teacher-shell/teacher-sidebar.component';

@Component({
  selector: 'app-teacher-dashboard',
  standalone: true,
  imports: [CommonModule, DecimalPipe, FormsModule, RouterLink, TeacherSidebarComponent],
  templateUrl: './teacher-dashboard.component.html',
  styleUrl: './teacher-dashboard.component.css'
})
export class TeacherDashboardComponent implements OnInit {
  private readonly service = inject(StoryService);
  private readonly state   = inject(AppStateService);
  private readonly router  = inject(Router);

  readonly isLoading    = signal(false);
  readonly data         = signal<any>(null);
  readonly searchTerm   = signal('');
  readonly activeLevel  = signal<number | null>(null);
  readonly levelPage    = signal<Record<number, number>>({ 1: 1, 2: 1, 3: 1 });
  readonly PAGE_SIZE    = 6;

  readonly isSchoolTeacher = computed(() => !!this.state.currentUser()?.schoolManagerId);

  readonly filteredStudents = computed(() => {
    const d = this.data();
    const t = this.searchTerm().toLowerCase().trim();
    if (!d?.students) return [];
    const bySearch = t
      ? d.students.filter((s: any) => s.childName.toLowerCase().includes(t))
      : d.students;
    const lv = this.activeLevel();
    return lv === null ? bySearch : bySearch.filter((s: any) => s.level === lv);
  });

  readonly studentsByLevel = computed(() => {
    const students = this.filteredStudents();
    return [1, 2, 3].map(lv => ({
      level: lv,
      label: lv === 1 ? 'المستوى الأول' : lv === 2 ? 'المستوى الثاني' : 'المستوى الثالث',
      color: lv === 1 ? '#F4788A' : lv === 2 ? '#8B5CF6' : '#22C55E',
      students: students.filter((s: any) => s.level === lv),
    }));
  });

  readonly studentsByClassroom = computed(() => {
    const students = this.filteredStudents();
    const map = new Map<string, any[]>();
    for (const s of students) {
      const key = (s as any).classroomName ?? 'بدون فصل';
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(s);
    }
    return Array.from(map.entries()).map(([name, list]) => ({ name, students: list }));
  });

  ngOnInit(): void {
    this.isLoading.set(true);
    this.service.getTeacherDashboard().subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  levelColor(level: string): string {
    return level === 'ممتاز' ? '#16A34A' : level === 'جيد' ? '#D97706' : '#DC2626';
  }

  progressColor(pct: number): string {
    return pct >= 80 ? '#22C55E' : pct >= 50 ? '#F59E0B' : '#EF4444';
  }

  lastActiveLabel(dateStr: string | null): string {
    if (!dateStr) return '—';
    const diff = Math.floor((Date.now() - new Date(dateStr).getTime()) / 86400000);
    if (diff === 0) return 'اليوم';
    if (diff === 1) return 'أمس';
    return `منذ ${diff} أيام`;
  }

  setLevel(lv: number | null): void {
    this.activeLevel.set(lv);
    this.levelPage.set({ 1: 1, 2: 1, 3: 1 });
  }

  levelNumColor(lv: number): string { return lv === 1 ? '#F4788A' : lv === 2 ? '#8B5CF6' : '#22C55E'; }

  pagedStudents(students: any[], level: number): any[] {
    const page = this.levelPage()[level] ?? 1;
    const start = (page - 1) * this.PAGE_SIZE;
    return students.slice(start, start + this.PAGE_SIZE);
  }

  totalPages(students: any[]): number {
    return Math.max(1, Math.ceil(students.length / this.PAGE_SIZE));
  }

  setLevelPage(level: number, page: number): void {
    this.levelPage.update(m => ({ ...m, [level]: page }));
  }

  addStudent(): void {
    this.router.navigate(['/auth/create-student']);
  }

  assignLessonToLevel(lv: number): void {
    this.router.navigate(['/teacher/lessons'], { queryParams: { level: lv } });
  }

  viewStudent(studentId: string, name: string): void {
    this.router.navigate(['/teacher/students', studentId], { queryParams: { name } });
  }

  sendFeedback(studentId: string, name: string): void {
    this.router.navigate(['/teacher/feedback'], { queryParams: { studentId, name } });
  }
}
