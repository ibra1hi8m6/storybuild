import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink, RouterLinkActive } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';
import { AuthService } from '../../../services/auth.service';
import { AppStateService } from '../../../services/app-state-service';
import { SubscriptionService, MySubscription } from '../../../services/subscription.service';

interface TeacherRow {
  id:       string;
  name:     string;
  email:    string;
  subject:  string;
  students: number;
  avgScore: number;
  joinedAt: string;
}

@Component({
  selector: 'app-school-teachers',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './school-teachers.component.html',
})
export class SchoolTeachersComponent implements OnInit {
  private readonly svc    = inject(StoryService);
  private readonly auth   = inject(AuthService);
  private readonly state  = inject(AppStateService);
  private readonly route  = inject(ActivatedRoute);
  private readonly subSvc = inject(SubscriptionService);

  readonly subscription = signal<MySubscription | null>(null);
  readonly atTeacherLimit = computed(() => {
    const s = this.subscription();
    const max = s?.maxTeachers ?? 1;
    const count = s?.teachersCount ?? this.teachers().length;
    return count >= max;
  });

  readonly isLoading    = signal(false);
  readonly isSaving     = signal(false);
  readonly teachers     = signal<TeacherRow[]>([]);
  readonly searchTerm   = signal('');
  readonly showForm     = signal(false);
  readonly showPassword = signal(false);
  readonly showResetPwd = signal(false);

  form = { name: '', email: '', password: '' };
  formError  = '';
  formSuccess = '';

  // Password reset
  readonly showResetModal = signal(false);
  readonly isResetting    = signal(false);
  resetTeacherId   = '';
  resetTeacherName = '';
  resetNewPassword = '';
  resetError       = '';
  resetSuccess     = '';

  readonly filtered = computed(() => {
    const q = this.searchTerm().toLowerCase();
    return !q ? this.teachers() : this.teachers().filter(t =>
      t.name.toLowerCase().includes(q) || t.email.toLowerCase().includes(q)
    );
  });

  ngOnInit(): void {
    this.subSvc.getMySubscription().subscribe({ next: s => this.subscription.set(s), error: () => {} });
    if (this.route.snapshot.queryParamMap.get('openForm') === '1') {
      this.showForm.set(true);
    }
    this.isLoading.set(true);
    this.auth.getSchoolTeachers().subscribe({
      next: list => {
        this.teachers.set(list.map(t => ({
          id:       t.id,
          name:     t.name,
          email:    t.email,
          subject:  'اللغة العربية',
          students: t.studentCount ?? 0,
          avgScore: 0,
          joinedAt: '',
        })));
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  toggleForm(): void {
    this.showForm.update(v => !v);
    this.form = { name: '', email: '', password: '' };
    this.formError  = '';
    this.formSuccess = '';
  }

  createTeacher(): void {
    this.formError  = '';
    this.formSuccess = '';
    if (!this.form.name.trim() || !this.form.email.trim() || !this.form.password.trim()) {
      this.formError = 'يرجى تعبئة جميع الحقول.';
      return;
    }
    if (this.form.password.length < 6) {
      this.formError = 'كلمة المرور يجب أن تكون 6 أحرف على الأقل.';
      return;
    }

    const schoolManagerId = this.state.currentUser()?.id;

    this.isSaving.set(true);
    this.auth.registerWithoutSession({
      fullName:        this.form.name.trim(),
      email:           this.form.email.trim(),
      password:        this.form.password,
      role:            'teacher',
      schoolManagerId: schoolManagerId ?? undefined,
    }).subscribe({
      next: res => {
        const newTeacher: TeacherRow = {
          id:       res.userId,
          name:     res.name,
          email:    this.form.email.trim(),
          subject:  'اللغة العربية',
          students: 0,
          avgScore: 0,
          joinedAt: new Date().toISOString().slice(0, 10),
        };
        this.teachers.update(list => [newTeacher, ...list]);
        this.formSuccess = `تم إنشاء حساب المعلم ${res.name} بنجاح.`;
        this.form = { name: '', email: '', password: '' };
        this.isSaving.set(false);
      },
      error: (err: any) => {
        this.formError = err?.error?.message ?? err?.error?.error ?? 'تعذّر إنشاء الحساب. تحقق من البيانات.';
        this.isSaving.set(false);
      }
    });
  }

  setSearch(v: string) { this.searchTerm.set(v); }
  scoreColor(s: number): string { return s >= 80 ? '#22C55E' : s >= 60 ? '#F59E0B' : '#EF4444'; }

  openResetModal(t: TeacherRow): void {
    this.resetTeacherId   = t.id;
    this.resetTeacherName = t.name;
    this.resetNewPassword = '';
    this.resetError       = '';
    this.resetSuccess     = '';
    this.showResetModal.set(true);
  }

  closeResetModal(): void { this.showResetModal.set(false); }

  confirmReset(): void {
    this.resetError   = '';
    this.resetSuccess = '';
    if (this.resetNewPassword.length < 6) {
      this.resetError = 'كلمة المرور يجب أن تكون 6 أحرف على الأقل.';
      return;
    }
    this.isResetting.set(true);
    this.auth.resetTeacherPassword(this.resetTeacherId, this.resetNewPassword).subscribe({
      next: res => {
        this.resetSuccess = res.message;
        this.isResetting.set(false);
        setTimeout(() => this.showResetModal.set(false), 2000);
      },
      error: (err: any) => {
        this.resetError = err?.error?.error ?? 'تعذّر إعادة تعيين كلمة المرور.';
        this.isResetting.set(false);
      }
    });
  }
}
