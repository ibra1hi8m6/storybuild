import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { AuthService, StudentSummary } from '../../../services/auth.service';
import { StudentGroupDto, LessonSummary, AssignLessonRequest } from '../../../models/story.models';
import { TeacherSidebarComponent } from '../teacher-shell/teacher-sidebar.component';
import { SubscriptionService, MySubscription } from '../../../services/subscription.service';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-teacher-groups',
  standalone: true,
  imports: [CommonModule, FormsModule, TeacherSidebarComponent],
  templateUrl: './teacher-groups.html',
  styleUrl: './teacher-groups.css'
})
export class TeacherGroupsComponent implements OnInit {
  private readonly svc    = inject(StoryService);
  private readonly state  = inject(AppStateService);
  private readonly auth   = inject(AuthService);
  private readonly subSvc = inject(SubscriptionService);

  readonly subscription = signal<MySubscription | null>(null);
  readonly atGroupLimit = computed(() => {
    const s = this.subscription();
    const max = s?.maxGroups ?? 1;
    const count = s?.groupsCount ?? this.groups().length;
    return count >= max;
  });

  teacherId      = signal('');
  groups         = signal<StudentGroupDto[]>([]);
  myStudents     = signal<StudentSummary[]>([]);
  directStudents = signal<{ id: string; name: string; level: number }[]>([]);
  loading        = signal(false);
  error          = signal('');
  message        = signal('');

  // Create group
  newGroupName = '';

  // Direct student
  directInput = '';

  // Paginated student picker (per group)
  pickerOpen:   Record<string, boolean> = {};
  pickerPage:   Record<string, number>  = {};
  pickerSearch: Record<string, string>  = {};

  // Assign lesson
  lessons    = signal<LessonSummary[]>([]);
  assignForm: Record<string, { lessonId: string; type: 'Student' | 'Group'; studentId: string }> = {};

  ngOnInit(): void {
    this.subSvc.getMySubscription().subscribe({ next: s => this.subscription.set(s), error: () => {} });
    const user = this.state.currentUser();
    if (user?.id) {
      this.teacherId.set(user.id);
      this.loadGroups();
      this.loadDirectStudents();
      this.loadLessons();
      this.auth.getMyStudents().subscribe({ next: s => this.myStudents.set(s) });
    }
  }

  loadGroups(): void {
    this.loading.set(true);
    this.svc.getTeacherGroups(this.teacherId()).subscribe({
      next:  g => { this.groups.set(g); this.loading.set(false); },
      error: () => { this.error.set('فشل تحميل المجموعات.'); this.loading.set(false); }
    });
  }

  loadDirectStudents(): void {
    this.svc.getDirectStudents(this.teacherId()).subscribe({
      next: s => this.directStudents.set(s)
    });
  }

  addDirect(): void {
    const id = this.directInput.trim();
    if (!id) return;
    this.svc.addDirectStudent(this.teacherId(), id).subscribe({
      next: s => {
        this.directStudents.update(list => [...list.filter(x => x.id !== s.id), s]);
        this.directInput = '';
        this.showMessage('تمت إضافة الطالب.');
      },
      error: err => this.error.set(err?.error?.error ?? 'فشل إضافة الطالب.')
    });
  }

  removeDirect(studentId: string): void {
    this.svc.removeDirectStudent(this.teacherId(), studentId).subscribe({
      next: () => {
        this.directStudents.update(list => list.filter(s => s.id !== studentId));
        this.showMessage('تمت إزالة الطالب.');
      },
      error: () => this.error.set('فشل إزالة الطالب.')
    });
  }

  loadLessons(): void {
    this.svc.getMyLessons(this.teacherId()).subscribe({
      next: l => this.lessons.set(l)
    });
  }

  createGroup(): void {
    if (!this.newGroupName.trim() || this.atGroupLimit()) return;
    this.svc.createGroup(this.teacherId(), this.newGroupName.trim()).subscribe({
      next: g => {
        this.groups.update(list => [...list, g]);
        this.newGroupName = '';
        this.showMessage('تم إنشاء المجموعة.');
        this.subSvc.getMySubscription().subscribe({ next: s => this.subscription.set(s), error: () => {} });
      },
      error: (err: any) => this.error.set(err?.error?.message ?? err?.error?.error ?? 'فشل إنشاء المجموعة.')
    });
  }

  // ── Picker helpers ──────────────────────────────────────────

  togglePicker(groupId: string): void {
    this.pickerOpen[groupId] = !this.pickerOpen[groupId];
    if (this.pickerOpen[groupId]) {
      this.pickerPage[groupId]   = 0;
      this.pickerSearch[groupId] = '';
    }
  }

  closePicker(groupId: string): void {
    this.pickerOpen[groupId] = false;
  }

  filteredStudents(groupId: string): StudentSummary[] {
    const q = (this.pickerSearch[groupId] ?? '').trim().toLowerCase();
    const group = this.groups().find(g => g.id === groupId);
    const memberIds = new Set(group?.members.map(m => m.studentId) ?? []);
    return this.myStudents()
      .filter(s => !memberIds.has(s.id))
      .filter(s => !q || s.name.toLowerCase().includes(q));
  }

  pagedStudents(groupId: string): StudentSummary[] {
    const page = this.pickerPage[groupId] ?? 0;
    return this.filteredStudents(groupId).slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE);
  }

  totalPages(groupId: string): number {
    return Math.max(1, Math.ceil(this.filteredStudents(groupId).length / PAGE_SIZE));
  }

  prevPage(groupId: string): void {
    if ((this.pickerPage[groupId] ?? 0) > 0) this.pickerPage[groupId]--;
  }

  nextPage(groupId: string): void {
    if ((this.pickerPage[groupId] ?? 0) < this.totalPages(groupId) - 1) this.pickerPage[groupId]++;
  }

  onSearchChange(groupId: string): void {
    this.pickerPage[groupId] = 0;
  }

  // ── Member actions ───────────────────────────────────────────

  addMember(groupId: string, studentId: string): void {
    if (!studentId) return;
    this.svc.addGroupMember(groupId, studentId).subscribe({
      next: () => { this.closePicker(groupId); this.loadGroups(); this.showMessage('تمت إضافة الطالب.'); },
      error: (err: any) => this.error.set(err?.error?.message ?? err?.error?.error ?? 'فشل إضافة الطالب.')
    });
  }

  removeMember(groupId: string, studentId: string): void {
    this.svc.removeGroupMember(groupId, studentId).subscribe({
      next: () => { this.loadGroups(); this.showMessage('تمت إزالة الطالب.'); },
      error: () => this.error.set('فشل إزالة الطالب.')
    });
  }

  deleteGroup(groupId: string): void {
    if (!confirm('هل تريد حذف هذه المجموعة؟')) return;
    this.svc.deleteGroup(groupId).subscribe({
      next: () => { this.groups.update(list => list.filter(g => g.id !== groupId)); this.showMessage('تم حذف المجموعة.'); },
      error: () => this.error.set('فشل حذف المجموعة.')
    });
  }

  assignLesson(groupId: string): void {
    const form = this.assignForm[groupId];
    if (!form?.lessonId) return;
    const req: AssignLessonRequest = {
      lessonId:        form.lessonId,
      targetType:      form.type,
      targetGroupId:   form.type === 'Group'   ? groupId        : undefined,
      targetStudentId: form.type === 'Student' ? form.studentId : undefined
    };
    this.svc.assignLesson(req).subscribe({
      next: () => this.showMessage('تم تعيين الدرس بنجاح.'),
      error: () => this.error.set('فشل تعيين الدرس.')
    });
  }

  initAssignForm(groupId: string): void {
    if (!this.assignForm[groupId])
      this.assignForm[groupId] = { lessonId: '', type: 'Group', studentId: '' };
  }

  private showMessage(msg: string): void {
    this.message.set(msg);
    setTimeout(() => this.message.set(''), 3000);
  }
}
