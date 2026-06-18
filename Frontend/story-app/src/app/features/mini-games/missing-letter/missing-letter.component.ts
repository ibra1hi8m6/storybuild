import { Component, EventEmitter, inject, Input, OnInit, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { GameAudioService } from '../services/game-audio.service';

export interface MissingLetterQuestion {
  word: string;
  missingIndex: number;
  imageUrl?: string;
  distractors: string[];
}

@Component({
  selector: 'app-missing-letter',
  standalone: true,
  imports: [CommonModule, DragDropModule],
  template: `
    <div class="ml-game" dir="rtl">
      <h2 class="game-title">🍎 أكمل الكلمة بالحرف الناقص</h2>

      <div class="word-display">
        @for (char of displayChars(); track $index) {
          @if (char === '_') {
            <div class="letter-slot"
              cdkDropList
              [id]="'slot-' + $index"
              (cdkDropListDropped)="onDrop($event)"
              [class.filled]="filled()"
              [class.wrong-slot]="wrongSlot()">
              @if (filled()) {
                <span class="filled-letter">{{ correctLetter() }}</span>
              } @else {
                <span class="slot-dash">_</span>
              }
            </div>
          } @else {
            <div class="letter-box">{{ char }}</div>
          }
        }
      </div>

      @if (question.imageUrl) {
        <img [src]="question.imageUrl" [alt]="question.word" class="word-image" />
      }

      <div class="letters-pool">
        @for (letter of letters(); track letter + $index) {
          <div class="letter-apple"
            cdkDrag [cdkDragData]="letter"
            [class.used]="usedLetter() === letter">
            {{ letter }}
          </div>
        }
      </div>
    </div>
  `,
  styleUrls: ['./missing-letter.component.css']
})
export class MissingLetterComponent implements OnInit {
  @Input() question!: MissingLetterQuestion;
  @Output() completed = new EventEmitter<void>();

  displayChars = signal<string[]>([]);
  letters = signal<string[]>([]);
  filled = signal(false);
  wrongSlot = signal(false);
  usedLetter = signal('');

  private audio = inject(GameAudioService);

  get correctLetter(): ReturnType<typeof signal<string>> {
    return signal(this.question?.word[this.question.missingIndex] ?? '');
  }

  ngOnInit(): void {
    const { word, missingIndex, distractors } = this.question;
    const chars = word.split('');
    chars[missingIndex] = '_';
    this.displayChars.set(chars);

    const pool = [word[missingIndex], ...distractors].sort(() => Math.random() - .5);
    this.letters.set(pool);
  }

  onDrop(event: CdkDragDrop<string>): void {
    const dropped = event.item.data as string;
    const correct = this.question.word[this.question.missingIndex];
    if (dropped === correct) {
      this.filled.set(true);
      this.audio.playSuccess();
      setTimeout(() => this.completed.emit(), 700);
    } else {
      this.wrongSlot.set(true);
      this.audio.playError();
      setTimeout(() => this.wrongSlot.set(false), 600);
    }
  }
}
