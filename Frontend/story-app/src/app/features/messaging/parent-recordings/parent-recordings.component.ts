import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';

interface RecordingEntry {
  id: string;
  pageId: string;
  pageType: string;
  audioFileUrl: string;
  durationSeconds: number;
  createdAt: string;
  report?: {
    wcpm: number;
    accuracyScore: number;
    expectedText: string;
    extractedText: string;
    mispronouncedWordsJson: string;
    createdAt: string;
  };
}

@Component({
  selector: 'app-parent-recordings',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="recordings-page" dir="rtl">
      <div class="page-header">
        <h1><i class="bi bi-mic-fill"></i> تسجيلات طفلك</h1>
      </div>

      @if (loading()) {
        <div class="loading-state">
          <div class="spinner-border text-primary"></div>
        </div>
      } @else if (recordings().length === 0) {
        <div class="empty-state">
          <div class="empty-icon">🎤</div>
          <h3>لا توجد تسجيلات بعد</h3>
          <p>ستظهر هنا تسجيلات طفلك بعد ممارسة القراءة</p>
        </div>
      } @else {
        <div class="recording-list">
          @for (rec of recordings(); track rec.id) {
            <div class="rec-card">
              <div class="rec-meta">
                <span class="page-badge">{{ rec.pageType }}</span>
                <span class="rec-date">{{ rec.createdAt | date:'d MMM yyyy, HH:mm' }}</span>
              </div>

              <audio [src]="rec.audioFileUrl" controls class="audio-player"></audio>

              @if (rec.report) {
                <div class="report-section">
                  <div class="score-row">
                    <div class="score-pill" [class.good]="rec.report.accuracyScore >= 70"
                      [class.bad]="rec.report.accuracyScore < 70">
                      <span class="score-label">الدقة</span>
                      <span class="score-val">{{ rec.report.accuracyScore | number:'1.0-0' }}%</span>
                    </div>
                    <div class="score-pill">
                      <span class="score-label">كلمة/دقيقة</span>
                      <span class="score-val">{{ rec.report.wcpm | number:'1.0-0' }}</span>
                    </div>
                  </div>
                  @if (mispronounced(rec).length > 0) {
                    <div class="miss-section">
                      <p class="miss-label">كلمات تحتاج تدريباً:</p>
                      <div class="miss-chips">
                        @for (w of mispronounced(rec); track w) {
                          <span class="miss-chip">{{ w }}</span>
                        }
                      </div>
                    </div>
                  }
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
  styleUrls: ['./parent-recordings.component.css']
})
export class ParentRecordingsComponent implements OnInit {
  recordings = signal<RecordingEntry[]>([]);
  loading = signal(true);

  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);

  async ngOnInit(): Promise<void> {
    const studentId = this.route.snapshot.paramMap.get('id')!;
    try {
      const data = await firstValueFrom(
        this.http.get<RecordingEntry[]>(`${environment.apiUrl}/api/parent-portal/child/${studentId}/recordings`)
      );
      this.recordings.set(data);
    } finally {
      this.loading.set(false);
    }
  }

  mispronounced(rec: RecordingEntry): string[] {
    if (!rec.report) return [];
    try { return JSON.parse(rec.report.mispronouncedWordsJson) as string[]; }
    catch { return []; }
  }
}
