import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { TeacherSidebarComponent } from '../teacher-shell/teacher-sidebar.component';
import { StoryService } from '../../../services/story';
import { StudentDashboardDto, WritingAttemptHistory, ReadingAttemptHistory } from '../../../models/story.models';

type TabId = 'overview' | 'writing' | 'reading';

@Component({
  selector: 'app-student-detail',
  standalone: true,
  imports: [CommonModule, DecimalPipe, RouterLink, TeacherSidebarComponent],
  templateUrl: './student-detail.component.html',
})
export class StudentDetailComponent implements OnInit {
  private readonly svc   = inject(StoryService);
  private readonly route = inject(ActivatedRoute);

  readonly isLoading      = signal(false);
  readonly studentName    = signal('');
  readonly data           = signal<StudentDashboardDto | null>(null);
  readonly error          = signal<string | null>(null);
  readonly activeTab      = signal<TabId>('overview');
  readonly writingHistory = signal<WritingAttemptHistory[]>([]);
  readonly readingHistory = signal<ReadingAttemptHistory[]>([]);
  readonly historyLoading = signal(false);

  readonly tabs: { id: TabId; label: string; icon: string }[] = [
    { id: 'overview', label: 'نظرة عامة',    icon: '📊' },
    { id: 'writing',  label: 'الكتابة',       icon: '✏️' },
    { id: 'reading',  label: 'القراءة',       icon: '🎤' },
  ];

  ngOnInit(): void {
    const studentId = this.route.snapshot.paramMap.get('studentId') ?? '';
    const name = this.route.snapshot.queryParamMap.get('name') ?? studentId;
    this.studentName.set(name);
    if (!studentId) return;
    this.isLoading.set(true);
    this.svc.getStudentDashboard(studentId).subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.error.set('لم يتم العثور على بيانات هذا الطالب.'); }
    });
  }

  selectTab(id: TabId): void {
    this.activeTab.set(id);
    const studentId = this.route.snapshot.paramMap.get('studentId') ?? '';
    if (id === 'writing' && this.writingHistory().length === 0) {
      this.historyLoading.set(true);
      this.svc.getWritingHistory(studentId).subscribe({
        next:  h => { this.writingHistory.set(h); this.historyLoading.set(false); },
        error: () => this.historyLoading.set(false)
      });
    }
    if (id === 'reading' && this.readingHistory().length === 0) {
      this.historyLoading.set(true);
      this.svc.getReadingHistory(studentId).subscribe({
        next:  h => { this.readingHistory.set(h); this.historyLoading.set(false); },
        error: () => this.historyLoading.set(false)
      });
    }
  }

  scoreColor(s: number): string { return s >= 80 ? '#22C55E' : s >= 50 ? '#F59E0B' : '#EF4444'; }
  formatDate(d: string): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('ar-SA', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}
