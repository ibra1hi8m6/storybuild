import { Component, EventEmitter, inject, Input, OnInit, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { GameAudioService } from '../services/game-audio.service';

@Component({
  selector: 'app-ordering-game',
  standalone: true,
  imports: [CommonModule, DragDropModule],
  template: `
    <div class="ordering-game" dir="rtl">
      <h2 class="game-title">🚂 رتّب الكلمات لتكوين جملة</h2>

      <div class="sentence-hint">
        <p class="hint-label">الجملة الصحيحة:</p>
        <p class="hint-text">{{ correctSentence }}</p>
      </div>

      <div class="train-track">
        <div cdkDropList cdkDropListOrientation="horizontal"
          [cdkDropListData]="wordOrder()"
          class="train-list"
          (cdkDropListDropped)="onDrop($event)">
          @for (word of wordOrder(); track word + $index) {
            <div class="train-cart" cdkDrag>
              <div class="cart-body">{{ word }}</div>
              <div class="cart-wheels"><span class="wheel"></span><span class="wheel"></span></div>
            </div>
          }
        </div>
      </div>

      <button class="check-btn" (click)="check()">
        <i class="bi bi-check2-all"></i> تحقق
      </button>

      @if (result() === 'correct') {
        <div class="result-banner correct">✅ ممتاز! الترتيب صحيح!</div>
      } @else if (result() === 'wrong') {
        <div class="result-banner wrong">❌ حاول مرة أخرى</div>
      }
    </div>
  `,
  styleUrls: ['./ordering-game.component.css']
})
export class OrderingGameComponent implements OnInit {
  @Input() words: string[] = [];
  @Input() correctSentence = '';
  @Output() completed = new EventEmitter<void>();

  wordOrder = signal<string[]>([]);
  result = signal<'idle' | 'correct' | 'wrong'>('idle');

  private audio = inject(GameAudioService);

  ngOnInit(): void {
    this.wordOrder.set([...this.words].sort(() => Math.random() - .5));
  }

  onDrop(event: CdkDragDrop<string[]>): void {
    const arr = [...this.wordOrder()];
    moveItemInArray(arr, event.previousIndex, event.currentIndex);
    this.wordOrder.set(arr);
    this.result.set('idle');
  }

  check(): void {
    const formed = this.wordOrder().join(' ');
    if (formed === this.correctSentence) {
      this.result.set('correct');
      this.audio.playSuccess();
      setTimeout(() => this.completed.emit(), 1000);
    } else {
      this.result.set('wrong');
      this.audio.playError();
      setTimeout(() => this.result.set('idle'), 1500);
    }
  }
}
