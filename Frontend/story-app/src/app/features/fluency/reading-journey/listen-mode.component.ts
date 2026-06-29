import {
  Component, Input, Output, EventEmitter, signal,
  OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { TtsService } from '../../../services/tts.service';

@Component({
  selector: 'app-listen-mode',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
<div class="listen-panel" dir="rtl">

  <!-- Sentence display -->
  <p class="listen-sentence">
    @for (word of words(); track $index) {
      <span dir="rtl" class="word">{{ word }}</span>&#x200f; }
  </p>

  <!-- Controls -->
  <div class="listen-controls">
    <button class="btn-listen" [class.playing]="isPlaying()"
            (click)="toggle()">
      <i class="bi" [class.bi-play-circle-fill]="!isPlaying()"
                    [class.bi-pause-circle-fill]="isPlaying()"></i>
      {{ isPlaying() ? 'إيقاف' : 'استمع للجملة' }}
    </button>
  </div>

  <!-- Next step -->
  <button class="btn-next" (click)="onNext()">
    <i class="bi bi-arrow-left me-2"></i> {{ nextLabel }}
  </button>

</div>
  `,
  styleUrls: ['./listen-mode.component.css']
})
export class ListenModeComponent implements OnDestroy {
  @Input() set sentence(v: string) {
    this._sentence = v;
    this.words.set(v.trim().split(/\s+/).filter(w => w.length > 0));
    this.tts.stop();
    this.isPlaying.set(false);
  }
  @Input() nextLabel = 'الآن اقرأ بنفسك';
  @Output() next = new EventEmitter<void>();

  private readonly cdr = inject(ChangeDetectorRef);
  private readonly tts = inject(TtsService);
  private _sentence = '';

  readonly words     = signal<string[]>([]);
  readonly isPlaying = signal(false);

  toggle() {
    if (this.isPlaying()) {
      this.tts.stop();
      this.isPlaying.set(false);
    } else {
      void this.speak();
    }
  }

  private async speak() {
    if (!this._sentence) return;
    this.isPlaying.set(true);
    this.cdr.markForCheck();
    try {
      await this.tts.play(this._sentence);
    } finally {
      this.isPlaying.set(false);
      this.cdr.markForCheck();
    }
  }

  onNext() {
    this.tts.stop();
    this.isPlaying.set(false);
    this.next.emit();
  }

  ngOnDestroy() { this.tts.stop(); }
}
