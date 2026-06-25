import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { RouterLink, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';
import { AuthService } from '../../../services/auth.service';
import { RecentActivityDto } from '../../../models/story.models';

@Component({
  selector: 'app-parent-notifications',
  standalone: true,
  imports: [CommonModule, DecimalPipe, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './parent-notifications.component.html',
})
export class ParentNotificationsComponent implements OnInit {
  private readonly svc   = inject(StoryService);
  private readonly auth  = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  readonly isLoading     = signal(false);
  readonly students      = signal<{ id: string; name: string }[]>([]);
  readonly selectedChild = signal<string>('');
  readonly activities    = signal<RecentActivityDto[]>([]);
  readonly selectedStudentId = computed(() =>
    this.students().find(s => s.name === this.selectedChild())?.id ?? ''
  );

  ngOnInit(): void {
    const preselect = this.route.snapshot.queryParamMap.get('child') ?? '';
    this.isLoading.set(true);
    this.auth.getMyStudents().subscribe({
      next: studentList => {
        this.students.set(studentList.map(s => ({ id: s.id, name: s.name })));
        if (studentList.length === 0) { this.isLoading.set(false); return; }
        const match = preselect ? studentList.find(s => s.name === preselect || s.id === preselect) : null;
        const first = match ?? studentList[0];
        this.selectChild(first.id, first.name);
      },
      error: () => this.isLoading.set(false)
    });
  }

  selectChild(studentId: string, name: string): void {
    this.selectedChild.set(name);
    this.isLoading.set(true);
    this.svc.getParentDashboard(studentId).subscribe({
      next:  d => { this.activities.set(d.recentActivity ?? []); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  activityIcon(type: string): string {
    const m: Record<string,string> = { exam: '📝', story: '📖', lesson: '✏️', writing: '🖊️' };
    return m[type] ?? '🔔';
  }

  formatDate(d: string): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('ar-SA', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  }
}
