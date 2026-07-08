import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { TeacherSidebarComponent } from '../../teacher/teacher-shell/teacher-sidebar.component';
import { SubscriptionService, MySubscription, PLAN_LABELS } from '../../../services/subscription.service';

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
  private readonly subSvc  = inject(SubscriptionService);

  readonly subscription = signal<MySubscription | null>(null);

  readonly teacherName = computed(() => {
    const u = this.state.currentUser();
    return u?.name ?? 'المعلم';
  });

  readonly planLabel = computed(() => {
    const s = this.subscription();
    return s ? (PLAN_LABELS[s.activePlan] ?? s.activePlan) : 'مجاني';
  });

  readonly maxStudents = computed(() => {
    const s = this.subscription();
    return s?.maxStudents ?? 5;
  });

  readonly maxGroups = computed(() => {
    const s = this.subscription();
    return s?.maxGroups ?? 1;
  });

  readonly studentsCount = computed(() => {
    const s = this.subscription();
    return s?.studentsCount ?? this.data()?.totalStudents ?? 0;
  });

  readonly groupsCount = computed(() => {
    const s = this.subscription();
    return s?.groupsCount ?? 0;
  });

  readonly atStudentLimit = computed(() => {
    if (this.isSchoolTeacher()) return false;
    return this.studentsCount() >= this.maxStudents();
  });
  readonly atGroupLimit   = computed(() => this.groupsCount()   >= this.maxGroups());

  readonly activationCode    = signal('');
  readonly isActivating      = signal(false);
  readonly activationError   = signal<string | null>(null);
  readonly activationSuccess = signal<string | null>(null);

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
    const d = this.data();
    // School teacher: use server-built classroom groups so empty classrooms appear
    if (this.isSchoolTeacher() && d?.classrooms) {
      const t = this.searchTerm().toLowerCase().trim();
      return (d.classrooms as any[]).map((cls: any) => ({
        classroomId: cls.classroomId as string,
        name:        cls.classroomName as string,
        students:    t
          ? (cls.students as any[]).filter((s: any) => s.childName.toLowerCase().includes(t))
          : (cls.students as any[])
      }));
    }
    // Private teacher: group from flat students list
    const students = this.filteredStudents();
    const map = new Map<string, { classroomId: string | null; name: string; students: any[] }>();
    for (const s of students) {
      const id   = (s as any).classroomId   as string | null ?? null;
      const name = (s as any).classroomName as string        ?? 'بدون فصل';
      const key  = id ?? name;
      if (!map.has(key)) map.set(key, { classroomId: id, name, students: [] });
      map.get(key)!.students.push(s);
    }
    return Array.from(map.values());
  });

  ngOnInit(): void {
    this.subSvc.getMySubscription().subscribe({ next: s => this.subscription.set(s), error: () => {} });
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
    if (this.atStudentLimit()) return;
    this.router.navigate(['/auth/create-student']);
  }

  activateCode(): void {
    const code = this.activationCode().trim();
    if (!code) return;
    this.isActivating.set(true);
    this.activationError.set(null);
    this.activationSuccess.set(null);
    this.subSvc.activate(code).subscribe({
      next: res => {
        this.isActivating.set(false);
        this.activationSuccess.set(res.message);
        this.activationCode.set('');
        this.subSvc.getMySubscription().subscribe({ next: s => this.subscription.set(s), error: () => {} });
      },
      error: (err: Error) => {
        this.isActivating.set(false);
        this.activationError.set(err.message);
      }
    });
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
