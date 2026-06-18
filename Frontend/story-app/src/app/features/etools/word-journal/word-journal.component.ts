import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { VocabularyService, WordJournalDto } from '../services/vocabulary.service';
import { AppStateService } from '../../../services/app-state-service';

@Component({
  selector: 'app-word-journal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="journal-page" dir="rtl">
      <div class="journal-header">
        <h1><i class="bi bi-journal-bookmark-fill"></i> مفرداتي</h1>
        <p class="subtitle">الكلمات التي جمعتها أثناء القراءة</p>
      </div>

      @if (loading()) {
        <div class="loading-state">
          <div class="spinner-border text-primary" role="status"></div>
        </div>
      } @else if (entries().length === 0) {
        <div class="empty-state">
          <div class="empty-icon">📖</div>
          <h3>لا توجد مفردات بعد</h3>
          <p>أضف كلمات من قراءتك لتظهر هنا</p>
        </div>
      } @else {
        <div class="journal-grid">
          @for (entry of entries(); track entry.id) {
            <div class="word-card">
              @if (entry.imageUrl) {
                <img [src]="entry.imageUrl" [alt]="entry.word" class="word-image" />
              } @else {
                <div class="word-icon">📝</div>
              }
              <div class="word-body">
                <span class="arabic-word">{{ entry.word }}</span>
                <p class="word-def">{{ entry.definition }}</p>
              </div>
              <div class="card-actions">
                <button class="tts-btn" (click)="speak(entry.word)" title="استمع">
                  <i class="bi bi-volume-up-fill"></i>
                </button>
                <button class="del-btn" (click)="remove(entry)" title="حذف">
                  <i class="bi bi-trash3"></i>
                </button>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
  styleUrls: ['./word-journal.component.css']
})
export class WordJournalComponent implements OnInit {
  entries = signal<WordJournalDto[]>([]);
  loading = signal(true);

  private vocabService = inject(VocabularyService);
  private appState = inject(AppStateService);

  async ngOnInit(): Promise<void> {
    const user = this.appState.currentUser();
    if (!user) return;
    try {
      const data = await this.vocabService.getJournal(user.id);
      this.entries.set(data);
    } finally {
      this.loading.set(false);
    }
  }

  speak(word: string): void {
    const utt = new SpeechSynthesisUtterance(word);
    utt.lang = 'ar-SA';
    speechSynthesis.speak(utt);
  }

  async remove(entry: WordJournalDto): Promise<void> {
    const user = this.appState.currentUser();
    if (!user) return;
    await this.vocabService.removeFromJournal(entry.id, user.id);
    this.entries.update(list => list.filter(e => e.id !== entry.id));
  }
}
