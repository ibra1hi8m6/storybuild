import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../services/auth.service';
import { AppStateService } from '../../../services/app-state-service';
import { StoryService } from '../../../services/story';

@Component({
  selector: 'app-create-student',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './create-student.component.html',
  styleUrl: './create-student.component.css',
})
export class CreateStudentComponent implements OnInit {
  private readonly auth   = inject(AuthService);
  private readonly state  = inject(AppStateService);
  private readonly router = inject(Router);
  private readonly route  = inject(ActivatedRoute);
  private readonly svc    = inject(StoryService);

  returnTo = 'parent';

  readonly isLoading           = signal(false);
  readonly error               = signal<string | null>(null);
  readonly done                = signal(false);
  readonly isTeacher           = computed(() => ['teacher', 'school'].includes(this.state.userRole()));
  readonly isSchoolTeacher     = computed(() => !!this.state.currentUser()?.schoolManagerId);
  readonly classrooms          = signal<any[]>([]);
  readonly selectedClassroomId = signal<string | null>(null);
  readonly classroomsLoading   = signal(false);

  form = {
    name:        '',
    age:         6,
    username:    '',
    nationalId:  '',
    imagePin1:   0,
    imagePin2:   null as number | null,
    level:       1,
    avatarEmoji: '🐰',
  };

  readonly avatarOptions = [
    '🐰','🦆','🐟','🐢','🐱','🦎','🚗','🚀',
    '🦈','🍓','🍎','🦋','🌸','⭐','🎈','🌙',
    '🦁','🐘','🦊','🐸','🦜','🦄','🐙','🌟',
  ];

  readonly icons = [
    { id:  1, emoji: '🐰', label: 'أرنب' }, { id:  2, emoji: '🦆', label: 'بطة' },
    { id:  3, emoji: '🐟', label: 'سمكة' }, { id:  4, emoji: '🐢', label: 'سلحفاة' },
    { id:  5, emoji: '🐱', label: 'قطة' },  { id:  6, emoji: '🦎', label: 'سحلية' },
    { id:  7, emoji: '🚗', label: 'سيارة' }, { id:  8, emoji: '🚕', label: 'تاكسي' },
    { id:  9, emoji: '🚀', label: 'صاروخ' }, { id: 10, emoji: '🚂', label: 'قطار' },
    { id: 11, emoji: '🦈', label: 'قرش' },  { id: 12, emoji: '⛵', label: 'قارب' },
    { id: 13, emoji: '🍓', label: 'فراولة' }, { id: 14, emoji: '🍎', label: 'تفاحة' },
    { id: 15, emoji: '🥕', label: 'جزرة' }, { id: 16, emoji: '🦋', label: 'فراشة' },
    { id: 17, emoji: '🌸', label: 'وردة' }, { id: 18, emoji: '⭐', label: 'نجمة' },
    { id: 19, emoji: '🎈', label: 'بالون' }, { id: 20, emoji: '🌙', label: 'قمر' },
  ];

  readonly selectedPins = signal<number[]>([]);

  ngOnInit(): void {
    const queryReturnTo = this.route.snapshot.queryParamMap.get('returnTo');
    const role = this.state.userRole();
    if (queryReturnTo) {
      this.returnTo = queryReturnTo;
    } else if (role === 'teacher') {
      this.returnTo = 'teacher';
    } else if (role === 'school') {
      this.returnTo = 'school';
    } else {
      this.returnTo = 'parent';
    }

    if (this.isSchoolTeacher()) {
      this.classroomsLoading.set(true);
      this.svc.getMyTeacherClassrooms().subscribe({
        next: (list) => {
          this.classrooms.set(list);
          if (list.length === 1) {
            this.selectedClassroomId.set(list[0].id);
          }
          this.classroomsLoading.set(false);
        },
        error: () => {
          this.classroomsLoading.set(false);
        }
      });
    }
  }

  goBack(): void {
    this.router.navigate([this.dashboardRoute()]);
  }

  private dashboardRoute(): string {
    if (this.returnTo === 'teacher') return '/teacher/students';
    if (this.returnTo === 'school')  return '/school/dashboard';
    return '/parent/dashboard';
  }

  togglePin(id: number): void {
    const pins = this.selectedPins();
    if (pins.includes(id)) {
      this.selectedPins.update(s => s.filter(x => x !== id));
    } else if (pins.length < 2) {
      this.selectedPins.update(s => [...s, id]);
    }
  }

  pinSelected(id: number): boolean { return this.selectedPins().includes(id); }
  pinOrder(id: number):    number   { return this.selectedPins().indexOf(id) + 1; }

  submit(): void {
    const pins = this.selectedPins();
    if (!this.form.name.trim())       { this.error.set('يرجى إدخال اسم الطفل.'); return; }
    if (!this.form.nationalId.trim()) { this.error.set('يرجى إدخال الرقم التعريفي للطفل.'); return; }
    if (!this.form.username.trim())   { this.error.set('يرجى إدخال اسم المستخدم.'); return; }
    if (pins.length < 1)              { this.error.set('يرجى اختيار رمز صورة واحد على الأقل.'); return; }

    if (this.isSchoolTeacher()) {
      if (this.classrooms().length === 0) { this.error.set('لم يتم تعيينك في أي فصل دراسي. تواصل مع مدير المدرسة.'); return; }
      if (!this.selectedClassroomId())    { this.error.set('يرجى اختيار الفصل الدراسي.'); return; }
    }

    this.isLoading.set(true);
    this.error.set(null);

    this.auth.createStudent({
      name:        this.form.name.trim(),
      age:         this.form.age,
      username:    this.form.username.trim().toLowerCase(),
      nationalId:  this.form.nationalId.trim(),
      imagePin1:   pins[0],
      imagePin2:   pins[1] ?? null,
      level:       this.isSchoolTeacher() ? undefined : this.form.level,
      avatarEmoji: this.form.avatarEmoji || undefined,
      classroomId: this.selectedClassroomId() ?? undefined,
    }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.done.set(true);
        setTimeout(() => this.router.navigate([this.dashboardRoute()]), 1500);
      },
      error: err => {
        this.isLoading.set(false);
        this.error.set(err?.error?.error ?? err?.error?.message ?? 'فشل إنشاء الحساب. حاول مرة أخرى.');
      }
    });
  }
}
