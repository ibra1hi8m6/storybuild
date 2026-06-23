import {
  Component, Input, Output, EventEmitter, signal, computed,
  OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, inject
} from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-listen-mode',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
<div class="listen-panel" dir="rtl">

  <!-- Sentence with highlighted word -->
  <p class="listen-sentence">
    @for (word of words(); track $index) {
      <span
        dir="rtl"
        class="word"
        [class.word-active]="$index === activeWordIdx()"
        [class.word-done]="$index < activeWordIdx()">{{ word }}</span>&#x200f; }
  </p>

  <!-- Controls -->
  <div class="listen-controls">
    <button class="btn-listen" [class.playing]="isPlaying()"
            (click)="toggle()" [disabled]="!supported">
      <i class="bi" [class.bi-play-circle-fill]="!isPlaying()"
                    [class.bi-pause-circle-fill]="isPlaying()"></i>
      {{ isPlaying() ? 'إيقاف' : 'استمع للجملة' }}
    </button>
  </div>

  @if (!supported) {
    <p class="warn-text">المتصفح لا يدعم الصوت. انتقل مباشرة للقراءة.</p>
  }

  <!-- Next step -->
  <button class="btn-next" (click)="onNext()">
    <i class="bi bi-book-open me-2"></i> الآن اقرأ بنفسك
  </button>

</div>
  `,
  styleUrls: ['./listen-mode.component.css']
})
export class ListenModeComponent implements OnDestroy {
  @Input() set sentence(v: string) {
    this._sentence = v;
    this.words.set(v.trim().split(/\s+/).filter(w => w.length > 0));
    this.stop();
  }
  @Output() next = new EventEmitter<void>();

  private cdr = inject(ChangeDetectorRef);
  private _sentence = '';
  private utterance: SpeechSynthesisUtterance | null = null;

  readonly words = signal<string[]>([]);
  readonly activeWordIdx = signal(-1);
  readonly isPlaying = signal(false);
  readonly supported = typeof speechSynthesis !== 'undefined';

  toggle() {
    if (this.isPlaying()) this.stop();
    else this.speak();
  }

  private speak() {
    if (!this.supported || !this._sentence) return;
    speechSynthesis.cancel();

    this.utterance = new SpeechSynthesisUtterance(this._sentence);
    this.utterance.lang = 'ar-SA';
    this.utterance.rate = 0.75;

    // Word-boundary highlighting
    this.utterance.onboundary = (e) => {
      if (e.name !== 'word') return;
      // Find which word index corresponds to the charIndex
      const upTo = this._sentence.substring(0, e.charIndex);
      const idx = upTo.trim().split(/\s+/).filter(w => w.length > 0).length;
      this.activeWordIdx.set(idx);
      this.cdr.markForCheck();
    };

    this.utterance.onend = () => {
      this.isPlaying.set(false);
      this.activeWordIdx.set(-1);
      this.cdr.markForCheck();
    };
    this.utterance.onerror = () => {
      this.isPlaying.set(false);
      this.activeWordIdx.set(-1);
      this.cdr.markForCheck();
    };

    this.isPlaying.set(true);
    speechSynthesis.speak(this.utterance);
  }

  private stop() {
    if (this.supported) speechSynthesis.cancel();
    this.isPlaying.set(false);
    this.activeWordIdx.set(-1);
  }

  onNext() {
    this.stop();
    this.next.emit();
  }

  ngOnDestroy() { this.stop(); }
}
