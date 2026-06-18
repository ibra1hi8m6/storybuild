import { Component, EventEmitter, inject, Input, OnChanges, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { VocabularyService } from '../../services/vocabulary.service';
import { AppStateService } from '../../../../services/app-state-service';

@Component({
  selector: 'app-vocabulary-popup',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (visible && word()) {
      <div class="vocab-backdrop" (click)="close()"></div>
      <div class="vocab-popup" dir="rtl">
        <button class="close-btn" (click)="close()"><i class="bi bi-x-lg"></i></button>
        <div class="word-header">
          <span class="the-word">{{ word() }}</span>
          <button class="tts-btn" (click)="speak()" title="استمع"><i class="bi bi-volume-up-fill"></i></button>
        </div>
        <p class="definition">{{ definition() }}</p>
        @if (addedToJournal()) {
          <div class="added-badge"><i class="bi bi-check-circle-fill"></i> أُضيفت للمفردات</div>
        } @else {
          <button class="add-btn" (click)="addToJournal()" [disabled]="saving()">
            <i class="bi bi-bookmark-plus"></i> أضف للمفردات
          </button>
        }
      </div>
    }
  `,
  styleUrls: ['./vocabulary-popup.component.css']
})
export class VocabularyPopupComponent implements OnChanges {
  @Input() visible = false;
  @Input() word = signal('');
  @Input() definition = signal('');
  @Output() closed = new EventEmitter<void>();

  saving = signal(false);
  addedToJournal = signal(false);

  private vocabService = inject(VocabularyService);
  private appState = inject(AppStateService);

  ngOnChanges(): void {
    this.addedToJournal.set(false);
  }

  speak(): void {
    const utt = new SpeechSynthesisUtterance(this.word());
    utt.lang = 'ar-SA';
    speechSynthesis.speak(utt);
  }

  async addToJournal(): Promise<void> {
    const user = this.appState.currentUser();
    if (!user) return;
    this.saving.set(true);
    try {
      await this.vocabService.addToJournal(user.id, this.word(), this.definition());
      this.addedToJournal.set(true);
    } finally {
      this.saving.set(false);
    }
  }

  close(): void {
    this.closed.emit();
  }
}
