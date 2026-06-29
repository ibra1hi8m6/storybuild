import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AppStateService } from '../../../services/app-state-service';

@Component({
  selector: 'app-admin-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <!-- Hamburger button — mobile only -->
    <button class="hamburger-btn d-lg-none" (click)="menuOpen.set(true)" aria-label="فتح القائمة">☰</button>

    <!-- Backdrop — mobile only -->
    @if (menuOpen()) {
      <div class="sidebar-overlay d-lg-none" (click)="menuOpen.set(false)"></div>
    }

    <nav class="admin-sidebar" [class.open]="menuOpen()" dir="rtl">
      <div class="sidebar-logo">
        🛡️ لوحة الإدارة
        <button class="close-btn d-lg-none" (click)="menuOpen.set(false)" aria-label="إغلاق القائمة">✕</button>
      </div>
      <ul class="sidebar-nav">
        <li>
          <a class="nav-link" routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{exact:true}" (click)="menuOpen.set(false)">
            <span class="nav-icon">🏠</span> الصفحة الرئيسية
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/books" routerLinkActive="active" (click)="menuOpen.set(false)">
            <span class="nav-icon">📚</span> إدارة الكتب
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/learning" routerLinkActive="active" (click)="menuOpen.set(false)">
            <span class="nav-icon">✏️</span> محتوى التعلم
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/subscriptions" routerLinkActive="active" (click)="menuOpen.set(false)">
            <span class="nav-icon">💳</span> الاشتراكات
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/users" routerLinkActive="active" (click)="menuOpen.set(false)">
            <span class="nav-icon">👥</span> المستخدمون
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/schools" routerLinkActive="active" (click)="menuOpen.set(false)">
            <span class="nav-icon">🏫</span> إضافة مدرسة
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/stories" routerLinkActive="active" (click)="menuOpen.set(false)">
            <span class="nav-icon">📖</span> القصص
          </a>
        </li>
        <li>
          <a class="nav-link" routerLink="/admin/uploaded-stories" routerLinkActive="active" (click)="menuOpen.set(false)">
            <span class="nav-icon">📕</span> قصص PDF
          </a>
        </li>
      </ul>
      <div class="sidebar-footer">
        <button class="logout-btn" (click)="logout()">🚪 تسجيل الخروج</button>
      </div>
    </nav>
  `,
  styles: [`
    .admin-sidebar {
      width: 220px; height: 100vh; background: #1E1B4B; color: #fff;
      display: flex; flex-direction: column; padding: 24px 0; flex-shrink: 0;
      position: sticky; top: 0; overflow-y: auto;
    }
    .sidebar-logo {
      font-size: 16px; font-weight: 800; padding: 0 20px 24px;
      border-bottom: 1px solid rgba(255,255,255,.1);
      display: flex; align-items: center; justify-content: space-between;
    }
    .close-btn {
      background: transparent; border: none; color: rgba(255,255,255,.7);
      font-size: 18px; cursor: pointer; padding: 0 4px; line-height: 1;
    }
    .close-btn:hover { color: #fff; }
    .sidebar-nav { list-style: none; margin: 16px 0 0; padding: 0; flex: 1; }
    .sidebar-nav li { margin: 2px 0; }
    .nav-link {
      display: flex; align-items: center; gap: 10px; padding: 12px 20px;
      font-size: 14px; font-weight: 700; color: rgba(255,255,255,.7);
      text-decoration: none; border-radius: 0 24px 24px 0; margin-left: 8px;
      transition: all .2s;
    }
    .nav-link:hover { background: rgba(255,255,255,.1); color: #fff; }
    .nav-link.active { background: #F4788A; color: #fff; }
    .nav-icon { font-size: 18px; }
    .sidebar-footer { padding: 16px 20px; border-top: 1px solid rgba(255,255,255,.1); }
    .logout-btn {
      width: 100%; padding: 10px 14px; background: rgba(255,255,255,.08);
      border: 1px solid rgba(255,255,255,.15); border-radius: 10px;
      color: rgba(255,255,255,.8); font-size: 13px; font-weight: 700;
      font-family: Cairo, sans-serif; cursor: pointer; transition: all .2s;
    }
    .logout-btn:hover { background: rgba(239,68,68,.3); border-color: #EF4444; color: #fff; }

    /* Hamburger — mobile only */
    .hamburger-btn {
      position: fixed; top: 12px; right: 12px; z-index: 1060;
      background: #1E1B4B; color: #fff; border: none; border-radius: 10px;
      padding: 8px 12px; font-size: 20px; cursor: pointer;
      box-shadow: 0 2px 8px rgba(0,0,0,.3); line-height: 1;
    }

    /* Backdrop */
    .sidebar-overlay {
      position: fixed; inset: 0; background: rgba(0,0,0,.5); z-index: 1049;
    }

    /* Mobile: sidebar slides in from the right */
    @media (max-width: 991px) {
      .admin-sidebar {
        position: fixed; top: 0; right: 0; z-index: 1050;
        transform: translateX(100%); transition: transform .3s ease;
        box-shadow: -4px 0 24px rgba(0,0,0,.3);
        overflow-y: auto;
      }
      .admin-sidebar.open { transform: translateX(0); }
    }
  `]
})
export class AdminSidebarComponent {
  readonly menuOpen = signal(false);
  private readonly state  = inject(AppStateService);
  private readonly router = inject(Router);

  logout(): void {
    this.state.logout();
    this.router.navigate(['/auth/login']);
  }
}
