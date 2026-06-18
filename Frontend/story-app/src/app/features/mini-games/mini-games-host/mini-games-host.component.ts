import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatchingGameComponent, MatchPair } from '../matching-game/matching-game.component';
import { OrderingGameComponent } from '../ordering-game/ordering-game.component';
import { MissingLetterComponent, MissingLetterQuestion } from '../missing-letter/missing-letter.component';
import { ConfettiService } from '../services/confetti.service';

type GameType = 'matching' | 'ordering' | 'missing';

interface GameQuestion {
  type: GameType;
  matchPairs?: MatchPair[];
  orderWords?: string[];
  correctSentence?: string;
  missingLetterQ?: MissingLetterQuestion;
}

const DEMO_GAMES: GameQuestion[] = [
  {
    type: 'matching',
    matchPairs: [
      { word: 'قطة', imageUrl: 'https://res.cloudinary.com/dehuie9ht/image/upload/v1/lughati/cat.jpg' },
      { word: 'كلب', imageUrl: 'https://res.cloudinary.com/dehuie9ht/image/upload/v1/lughati/dog.jpg' },
      { word: 'بيت', imageUrl: 'https://res.cloudinary.com/dehuie9ht/image/upload/v1/lughati/house.jpg' },
    ]
  },
  {
    type: 'ordering',
    orderWords: ['الشمس', 'تضيء', 'السماء', 'في'],
    correctSentence: 'الشمس تضيء في السماء'
  },
  {
    type: 'missing',
    missingLetterQ: {
      word: 'قمر',
      missingIndex: 1,
      distractors: ['ب', 'ت', 'د'],
      imageUrl: 'https://res.cloudinary.com/dehuie9ht/image/upload/v1/lughati/moon.jpg'
    }
  }
];

@Component({
  selector: 'app-mini-games-host',
  standalone: true,
  imports: [CommonModule, MatchingGameComponent, OrderingGameComponent, MissingLetterComponent],
  template: `
    <div class="games-host" dir="rtl">
      <div class="games-header">
        <h1><i class="bi bi-controller"></i> ألعاب اللغة</h1>
        <div class="progress-bar-wrap">
          <div class="progress-bar-fill"
            [style.width.%]="(currentIdx() / games.length) * 100"></div>
        </div>
        <p class="progress-text">{{ currentIdx() }} / {{ games.length }}</p>
      </div>

      @if (currentIdx() >= games.length) {
        <div class="all-done">
          <div class="done-icon">🏆</div>
          <h2>أحسنت! أنهيت كل الألعاب!</h2>
          <p>لقد حصلت على نجمة اليوم!</p>
          <button class="back-btn" (click)="goBack()">
            <i class="bi bi-arrow-right-circle-fill"></i> العودة
          </button>
        </div>
      } @else {
        <div class="game-card">
          @if (currentGame().type === 'matching') {
            <app-matching-game
              [pairs]="currentGame().matchPairs!"
              (completed)="nextGame()">
            </app-matching-game>
          } @else if (currentGame().type === 'ordering') {
            <app-ordering-game
              [words]="currentGame().orderWords!"
              [correctSentence]="currentGame().correctSentence!"
              (completed)="nextGame()">
            </app-ordering-game>
          } @else if (currentGame().type === 'missing') {
            <app-missing-letter
              [question]="currentGame().missingLetterQ!"
              (completed)="nextGame()">
            </app-missing-letter>
          }
        </div>
      }
    </div>
  `,
  styleUrls: ['./mini-games-host.component.css']
})
export class MiniGamesHostComponent implements OnInit {
  games: GameQuestion[] = DEMO_GAMES;
  currentIdx = signal(0);

  private confetti = inject(ConfettiService);
  private router = inject(Router);

  ngOnInit(): void {}

  currentGame(): GameQuestion {
    return this.games[this.currentIdx()];
  }

  nextGame(): void {
    const next = this.currentIdx() + 1;
    this.currentIdx.set(next);
    if (next >= this.games.length) {
      this.confetti.burst();
    }
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
