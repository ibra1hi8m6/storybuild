import {
  Component, Input, Output, EventEmitter, signal, inject, OnDestroy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { WebAudioService } from '../services/web-audio.service';
import { FluencyApiService, FluencyReportDto } from '../services/fluency-api.service';
import { AppStateService } from '../../../services/app-state-service';

type RecordState = 'idle' | 'recording' | 'evaluating' | 'result';

@Component({
  selector: 'app-record-mode',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './record-mode.component.html',
  styleUrls: ['./record-mode.component.css']
})
export class RecordModeComponent implements OnDestroy {
  @Input() sentence = '';
  @Input() pageId = '';
  @Input() pageType: 'Story' | 'Lesson' = 'Story';
  @Output() passed = new EventEmitter<void>();

  private audio  = inject(WebAudioService);
  private api    = inject(FluencyApiService);
  private appState = inject(AppStateService);

  readonly state       = signal<RecordState>('idle');
  readonly report      = signal<FluencyReportDto | null>(null);
  readonly error       = signal('');
  readonly isRecording = this.audio.isRecording;
  readonly duration    = this.audio;

  async startRecording() {
    this.error.set('');
    this.report.set(null);
    try {
      await this.audio.startRecording();
      this.state.set('recording');
    } catch {
      this.error.set('تعذّر الوصول للمايكروفون. تأكد من السماح بالإذن.');
    }
  }

  async stopAndEvaluate() {
    this.state.set('evaluating');
    try {
      const blob = await this.audio.stopRecording();
      const user = this.appState.currentUser();
      if (!user) throw new Error('Not logged in');

      const result = await this.api.evaluate({
        studentId: user.id,
        pageId: this.pageId,
        pageType: this.pageType,
        expectedText: this.sentence,
        audioBlob: blob
      });

      this.report.set(result);
      this.state.set('result');
    } catch (e: any) {
      this.error.set('حدث خطأ أثناء التقييم. حاول مرة أخرى.');
      this.state.set('idle');
    }
  }

  retry() {
    this.report.set(null);
    this.error.set('');
    this.state.set('idle');
  }

  onNext() { this.passed.emit(); }

  get scoreColor(): string {
    const s = this.report()?.accuracyScore ?? 0;
    if (s >= 80) return '#22C55E';
    if (s >= 70) return '#F59E0B';
    return '#EF4444';
  }

  get scoreLabel(): string {
    const s = this.report()?.accuracyScore ?? 0;
    if (s >= 80) return 'ممتاز!';
    if (s >= 70) return 'جيد جداً!';
    return 'حاول مرة أخرى';
  }

  // Word-by-word comparison: expected words coloured green/red
  get wordComparison(): { word: string; correct: boolean }[] {
    const r = this.report();
    if (!r) return [];
    const normalize = (w: string) => w.replace(/[ً-ٰٟـ]/g, '').trim();
    const extractedSet = new Set(
      r.extractedText.split(/[\s،.؟!،"]+/).filter(Boolean).map(normalize)
    );
    return r.expectedText
      .split(/[\s،.؟!،"]+/)
      .filter(Boolean)
      .map(word => ({ word, correct: extractedSet.has(normalize(word)) }));
  }

  ngOnDestroy() {
    if (this.audio.isRecording()) {
      this.audio.stopRecording().catch(() => {});
    }
  }
}
