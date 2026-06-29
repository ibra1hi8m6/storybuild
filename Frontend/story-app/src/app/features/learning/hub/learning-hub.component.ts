import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AppStateService } from '../../../services/app-state-service';

@Component({
  selector: 'app-learning-hub',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './learning-hub.component.html',
  styleUrl: './learning-hub.component.css'
})
export class LearningHubComponent {
  private readonly router = inject(Router);
  readonly state           = inject(AppStateService);

  readonly navItems = [
    { icon: '📊', label: 'لوحتي',          route: '/dashboard' },
    { icon: '✏️', label: 'محتوى التعلم',   route: '/learning' },
    // { icon: '📋', label: 'تقدّمي',          route: '/progress' },
    { icon: '🏆', label: 'إنجازاتي',       route: '/achievements' },
    { icon: '📖', label: 'قصصي',           route: '/my-stories' },
    { icon: '✨', label: 'قصص ذكية',       route: '/ai-story' },
  ];

  go(route: string): void { this.router.navigate([route]); }
  logout(): void { this.state.logout(); this.router.navigate(['/auth/login']); }
}
