import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { ProgressService, ProgressSummary } from '../../../services/progress.service';

@Component({
  selector: 'app-child-progress',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './child-progress.component.html',
})
export class ChildProgressComponent implements OnInit {
  private readonly progress = inject(ProgressService);
  private readonly route    = inject(ActivatedRoute);

  readonly isLoading = signal(false);
  readonly childName = signal('');
  readonly data      = signal<ProgressSummary | null>(null);
  readonly error     = signal<string | null>(null);

  readonly sections = [
    { icon: '✏️', label: 'الحروف',  completedKey: 'lettersCompleted'   as const, totalKey: 'lettersTotal'   as const, color: '#F4788A' },
    { icon: '📝', label: 'الكلمات', completedKey: 'wordsCompleted'     as const, totalKey: 'wordsTotal'     as const, color: '#8B5CF6' },
    { icon: '💬', label: 'الجمل',   completedKey: 'sentencesCompleted' as const, totalKey: 'sentencesTotal' as const, color: '#0EA5E9' },
    { icon: '📚', label: 'الدروس',  completedKey: 'lessonsCompleted'   as const, totalKey: 'lessonsTotal'   as const, color: '#10B981' },
    { icon: '📖', label: 'القصص',   completedKey: 'storiesCompleted'   as const, totalKey: 'storiesTotal'   as const, color: '#F59E0B' },
  ];

  ngOnInit(): void {
    const studentId = this.route.snapshot.paramMap.get('studentId') ?? '';
    const name      = this.route.snapshot.queryParamMap.get('name') ?? studentId;
    this.childName.set(name);
    if (!studentId) return;
    this.isLoading.set(true);
    this.progress.getSummary(studentId).subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.error.set('لم يتم العثور على بيانات.'); }
    });
  }

  pct(completed: number, total: number): number {
    return total > 0 ? Math.round(completed / total * 100) : 0;
  }
}
