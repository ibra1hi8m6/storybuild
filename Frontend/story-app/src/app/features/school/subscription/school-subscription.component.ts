import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';
import { SubscriptionService, MySubscription, PLAN_LABELS } from '../../../services/subscription.service';

@Component({
  selector: 'app-school-subscription',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './school-subscription.component.html',
})
export class SchoolSubscriptionComponent implements OnInit {
  private readonly svc    = inject(StoryService);
  private readonly subSvc = inject(SubscriptionService);

  readonly isLoading    = signal(false);
  readonly dashboard    = signal<any>(null);
  readonly classrooms   = signal<any[]>([]);
  readonly subscription = signal<MySubscription | null>(null);

  readonly activationCode    = signal('');
  readonly isActivating      = signal(false);
  readonly activationError   = signal<string | null>(null);
  readonly activationSuccess = signal<string | null>(null);

  readonly totalStudents   = computed(() => this.dashboard()?.totalStudents  ?? 0);
  readonly totalTeachers   = computed(() => this.subscription()?.teachersCount ?? this.dashboard()?.totalTeachers ?? 0);
  readonly totalClassrooms = computed(() => this.subscription()?.classesCount ?? this.classrooms().length);

  readonly planName  = computed(() => {
    const s = this.subscription();
    return s ? (PLAN_LABELS[s.activePlan] ?? s.activePlan) : 'مجاني (تجريبي)';
  });

  readonly planLimit = computed(() => {
    const s = this.subscription();
    const plan = s?.activePlan ?? 'Free';
    if (plan === 'SchoolPremium') {
      return {
        classes:  s?.maxClasses  ?? 20,
        teachers: s?.maxTeachers ?? 10,
      };
    }
    return { classes: 1, teachers: 1 };
  });

  readonly renewDate = computed(() => {
    const exp = this.subscription()?.expiresAt;
    if (!exp) return 'غير محدد';
    return new Date(exp).toLocaleDateString('ar-SA');
  });

  readonly contactEmail = 'support@lughati.com';

  ngOnInit(): void {
    this.isLoading.set(true);
    this.subSvc.getMySubscription().subscribe({
      next: s => { this.subscription.set(s); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
    this.svc.getSchoolDashboard().subscribe({
      next:  d => this.dashboard.set(d),
      error: () => {}
    });
    this.svc.getSchoolClassrooms().subscribe({
      next:  list => this.classrooms.set(list),
      error: () => {}
    });
  }

  activate(): void {
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

  usagePct(used: number, limit: number): number {
    return limit > 0 ? Math.min(Math.round(used / limit * 100), 100) : 0;
  }

  usageColor(pct: number): string {
    return pct >= 90 ? '#EF4444' : pct >= 70 ? '#F59E0B' : '#22C55E';
  }
}
