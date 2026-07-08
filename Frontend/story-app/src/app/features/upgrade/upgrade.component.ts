import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { SubscriptionService } from '../../services/subscription.service';

@Component({
  selector: 'app-upgrade',
  standalone: true,
  imports: [CommonModule, RouterLink, NavbarComponent, FormsModule],
  templateUrl: './upgrade.component.html',
  styleUrl: './upgrade.component.css'
})
export class UpgradeComponent {
  private readonly subSvc = inject(SubscriptionService);

  readonly activationCode    = signal('');
  readonly isActivating      = signal(false);
  readonly activationError   = signal<string | null>(null);
  readonly activationSuccess = signal<string | null>(null);

  readonly plans = [
    {
      id: 'ParentPremium',
      name: 'مميز (أولياء الأمور)',
      icon: '👨‍👩‍👧',
      color: '#F4788A',
      audience: 'للأهل',
      features: [
        'حتى 3 أطفال في حساب واحد',
        'جميع الحروف والكلمات والجمل',
        'كل الكتيبات والقصص',
        'تقارير تقدّم مفصّلة',
        'تسجيلات القراءة والتقييم الصوتي',
      ]
    },
    {
      id: 'TeacherPremium',
      name: 'معلم مميز',
      icon: '👩‍🏫',
      color: '#8B5CF6',
      audience: 'للمعلمين',
      features: [
        'حتى 30 طالباً',
        'حتى 5 مجموعات',
        'جميع المحتوى التعليمي كاملاً',
        'لوحة تحكم الطلاب',
        'تحليلات الفصل ومستويات الأداء',
        'مولّد الدروس بالذكاء الاصطناعي',
      ]
    },
    {
      id: 'SchoolPremium',
      name: 'مدرسة مميزة',
      icon: '🏫',
      color: '#0EA5E9',
      audience: 'للمدارس',
      features: [
        'حتى 20 فصلاً دراسياً',
        'حتى 10 معلمين',
        'إدارة الفصول والمعلمين',
        'تقارير على مستوى المدرسة',
        'دعم مخصص',
      ]
    },
  ];

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
      },
      error: (err: Error) => {
        this.isActivating.set(false);
        this.activationError.set(err.message);
      }
    });
  }
}
