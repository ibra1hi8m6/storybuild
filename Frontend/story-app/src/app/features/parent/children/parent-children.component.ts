import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { AuthService, StudentSummary } from '../../../services/auth.service';
import { SubscriptionService, MySubscription, PLAN_LABELS } from '../../../services/subscription.service';

@Component({
  selector: 'app-parent-children',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './parent-children.component.html',
})
export class ParentChildrenComponent implements OnInit {
  private readonly auth    = inject(AuthService);
  private readonly subSvc  = inject(SubscriptionService);

  readonly isLoading    = signal(false);
  readonly children     = signal<StudentSummary[]>([]);
  readonly error        = signal<string | null>(null);
  readonly editingId    = signal<string | null>(null);
  readonly savingId     = signal<string | null>(null);
  readonly saveError    = signal<string | null>(null);
  readonly subscription = signal<MySubscription | null>(null);

  readonly atChildLimit = computed(() => {
    const sub = this.subscription();
    const max = sub?.maxChildren ?? 1;
    const count = sub?.childrenCount ?? this.children().length;
    return count >= max;
  });

  readonly planLabel = computed(() => {
    const s = this.subscription();
    return s ? (PLAN_LABELS[s.activePlan] ?? s.activePlan) : '—';
  });

  readonly activationCode    = signal('');
  readonly isActivating      = signal(false);
  readonly activationError   = signal<string | null>(null);
  readonly activationSuccess = signal<string | null>(null);

  readonly levels = [
    { value: 1, label: 'المستوى الأول', color: '#F4788A' },
    { value: 2, label: 'المستوى الثاني', color: '#8B5CF6' },
    { value: 3, label: 'المستوى الثالث', color: '#22C55E' },
  ];

  ngOnInit(): void {
    this.subSvc.getMySubscription().subscribe({ next: s => this.subscription.set(s), error: () => {} });
    this.isLoading.set(true);
    this.auth.getMyStudents().subscribe({
      next:  c => { this.children.set(c); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.error.set('تعذّر تحميل قائمة الأطفال.'); }
    });
  }

  toggleEdit(id: string): void {
    this.editingId.set(this.editingId() === id ? null : id);
    this.saveError.set(null);
  }

  setLevel(child: StudentSummary, level: number): void {
    if (child.level === level) { this.editingId.set(null); return; }
    this.savingId.set(child.id);
    this.saveError.set(null);
    this.auth.updateChildLevel(child.id, level).subscribe({
      next: () => {
        this.children.update(list => list.map(c => c.id === child.id ? { ...c, level } : c));
        this.savingId.set(null);
        this.editingId.set(null);
      },
      error: () => {
        this.savingId.set(null);
        this.saveError.set('تعذّر تحديث المستوى. حاول مجدداً.');
      }
    });
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

  levelLabel(l: number): string { return `المستوى ${l}`; }
  levelColor(l: number): string { return ['','#F4788A','#8B5CF6','#22C55E'][l] ?? '#F4788A'; }
}
