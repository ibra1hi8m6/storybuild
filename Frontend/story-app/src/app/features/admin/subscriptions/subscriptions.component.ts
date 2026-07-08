import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminSidebarComponent } from '../shared/admin-sidebar.component';
import { SubscriptionService, ActivationCodeDto, PLAN_LABELS } from '../../../services/subscription.service';

@Component({
  selector: 'app-subscriptions',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminSidebarComponent],
  templateUrl: './subscriptions.component.html',
  styleUrl: './subscriptions.component.css'
})
export class SubscriptionsComponent implements OnInit {
  private readonly subSvc = inject(SubscriptionService);

  readonly isLoading   = signal(false);
  readonly codes       = signal<ActivationCodeDto[]>([]);
  readonly showCreate  = signal(false);
  readonly isSaving    = signal(false);
  readonly saveError   = signal<string | null>(null);
  readonly saveSuccess = signal<string | null>(null);
  readonly copiedId    = signal<string | null>(null);

  readonly form = {
    plan:         'ParentPremium',
    durationDays: 365,
    maxUses:      1,
    code:         '',
    notes:        '',
    expiresAt:    '',
  };

  readonly availablePlans = [
    { value: 'ParentPremium',  label: 'مميز (أولياء الأمور) — 3 أطفال' },
    { value: 'TeacherPremium', label: 'معلم مميز — 30 طالب / 5 مجموعات' },
    { value: 'SchoolPremium',  label: 'مدرسة مميزة — 20 فصل / 10 معلمين' },
  ];

  readonly planLabels = PLAN_LABELS;

  ngOnInit(): void { this.loadCodes(); }

  loadCodes(): void {
    this.isLoading.set(true);
    this.subSvc.getAdminCodes().subscribe({
      next:  c => { this.codes.set(c); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  createCode(): void {
    this.isSaving.set(true);
    this.saveError.set(null);
    this.saveSuccess.set(null);
    this.subSvc.createCode({
      plan:         this.form.plan,
      durationDays: this.form.durationDays,
      maxUses:      this.form.maxUses,
      code:         this.form.code.trim() || null,
      notes:        this.form.notes.trim() || null,
      expiresAt:    this.form.expiresAt || null,
    }).subscribe({
      next: c => {
        this.isSaving.set(false);
        this.saveSuccess.set(`تم إنشاء الكود: ${c.code}`);
        this.form.code = '';
        this.form.notes = '';
        this.form.expiresAt = '';
        this.loadCodes();
      },
      error: (err: Error) => {
        this.isSaving.set(false);
        this.saveError.set(err.message);
      }
    });
  }

  deactivate(id: string): void {
    this.subSvc.deactivateCode(id).subscribe({
      next: () => this.codes.update(list => list.map(c => c.id === id ? { ...c, isActive: false } : c)),
      error: () => {}
    });
  }

  copyCode(code: string, id: string): void {
    navigator.clipboard.writeText(code).then(() => {
      this.copiedId.set(id);
      setTimeout(() => this.copiedId.set(null), 2000);
    });
  }

  planLabel(plan: string): string { return PLAN_LABELS[plan] ?? plan; }

  formatDate(d: string | null): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('ar-SA');
  }
}
