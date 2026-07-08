import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { SubscriptionAlertService } from '../../core/subscription-alert.service';

@Component({
  selector: 'app-subscription-upgrade-toast',
  standalone: true,
  template: `
    @if (svc.alert(); as alert) {
      <div class="upgrade-toast" role="alert" aria-live="assertive">
        <span class="icon">🔒</span>
        <div class="body">
          <p class="msg">{{ alert.message }}</p>
          <button class="upgrade-btn" (click)="goUpgrade()">ترقية الاشتراك</button>
        </div>
        <button class="close" (click)="svc.dismiss()" aria-label="إغلاق">✕</button>
      </div>
    }
  `,
  styles: [`
    @import url('https://fonts.googleapis.com/css2?family=Baloo+Bhaijaan+2:wght@600;700&display=swap');

    .upgrade-toast {
      position: fixed; bottom: 1.5rem; right: 1.5rem; left: 1.5rem;
      max-width: 520px; margin: 0 auto;
      background: #fff7ed; border: 2px solid #fed7aa;
      border-radius: 16px; padding: 1rem 1.2rem;
      display: flex; align-items: flex-start; gap: 0.8rem;
      box-shadow: 0 8px 30px rgba(0, 0, 0, 0.15);
      font-family: 'Baloo Bhaijaan 2', sans-serif;
      direction: rtl;
      animation: slideUp 0.3s ease-out;
      z-index: 2100;
    }

    @keyframes slideUp {
      from { opacity: 0; transform: translateY(20px); }
      to   { opacity: 1; transform: translateY(0); }
    }

    .icon { font-size: 1.4rem; flex-shrink: 0; padding-top: 2px; }

    .body {
      flex: 1;
      display: flex; flex-direction: column; gap: 0.5rem;
    }

    .msg {
      margin: 0;
      font-size: 0.95rem; font-weight: 700; color: #92400e;
    }

    .upgrade-btn {
      align-self: flex-start;
      background: #f97316; color: #fff;
      border: none; border-radius: 10px;
      padding: 0.35rem 0.9rem;
      font-size: 0.9rem; font-weight: 700;
      font-family: inherit; cursor: pointer;
      transition: background 0.15s;
    }
    .upgrade-btn:hover { background: #ea580c; }

    .close {
      background: none; border: none; cursor: pointer;
      font-size: 1rem; color: #92400e;
      padding: 2px 6px; border-radius: 8px;
      flex-shrink: 0; align-self: flex-start;
      transition: background 0.15s;
    }
    .close:hover { background: #fed7aa; }
  `]
})
export class SubscriptionUpgradeToastComponent {
  protected readonly svc = inject(SubscriptionAlertService);
  private  readonly router = inject(Router);

  goUpgrade(): void {
    this.svc.dismiss();
    this.router.navigate(['/upgrade']);
  }
}
