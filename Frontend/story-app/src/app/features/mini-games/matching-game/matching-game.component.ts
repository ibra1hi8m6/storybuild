import { Component, EventEmitter, inject, Input, OnInit, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { GameAudioService } from '../services/game-audio.service';

export interface MatchPair { word: string; imageUrl: string; }

@Component({
  selector: 'app-matching-game',
  standalone: true,
  imports: [CommonModule, DragDropModule],
  template: `
    <div class="matching-game" dir="rtl">
      <h2 class="game-title">🔗 وصّل الكلمة بالصورة</h2>

      <div class="game-grid">
        <div class="words-col">
          @for (pair of shuffledWords(); track pair.word) {
            <div class="word-chip draggable"
              cdkDrag
              [cdkDragData]="pair.word"
              [class.matched]="matchedWords().has(pair.word)">
              {{ pair.word }}
            </div>
          }
        </div>

        <div class="images-col">
          @for (pair of pairs; track pair.imageUrl) {
            <div class="image-drop"
              cdkDropList
              [id]="'img-' + pair.word"
              [cdkDropListConnectedTo]="wordListId"
              (cdkDropListDropped)="onDrop($event, pair.word)"
              [class.correct]="matchedWords().has(pair.word)"
              [class.wrong]="wrongTarget() === pair.word">
              <img [src]="pair.imageUrl" [alt]="pair.word" class="pair-image" />
              @if (matchedWords().has(pair.word)) {
                <div class="check-badge"><i class="bi bi-check-circle-fill"></i></div>
              }
            </div>
          }
        </div>
      </div>

      @if (allMatched()) {
        <div class="win-banner">🎉 أحسنت! أكملت التمرين!</div>
      }
    </div>
  `,
  styleUrls: ['./matching-game.component.css']
})
export class MatchingGameComponent implements OnInit {
  @Input() pairs: MatchPair[] = [];
  @Output() completed = new EventEmitter<void>();

  shuffledWords = signal<MatchPair[]>([]);
  matchedWords = signal<Set<string>>(new Set());
  wrongTarget = signal('');
  wordListId = 'word-list';

  private audio = inject(GameAudioService);

  ngOnInit(): void {
    this.shuffledWords.set([...this.pairs].sort(() => Math.random() - .5));
  }

  onDrop(event: CdkDragDrop<string>, targetWord: string): void {
    const dragged = event.item.data as string;
    if (dragged === targetWord) {
      this.audio.playSuccess();
      this.matchedWords.update(s => new Set([...s, targetWord]));
      if (this.matchedWords().size === this.pairs.length) {
        setTimeout(() => this.completed.emit(), 600);
      }
    } else {
      this.audio.playError();
      this.wrongTarget.set(targetWord);
      setTimeout(() => this.wrongTarget.set(''), 800);
    }
  }

  allMatched(): boolean {
    return this.matchedWords().size === this.pairs.length;
  }
}
