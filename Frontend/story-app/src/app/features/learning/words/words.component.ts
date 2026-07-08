import { Component, signal, inject, OnInit, computed } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { LearningService } from '../../../services/learning.service';
import { AppStateService } from '../../../services/app-state-service';
import { SubscriptionAlertService } from '../../../core/subscription-alert.service';
import { WordContentDto } from '../../../models/learning.models';

@Component({
  selector: 'app-words',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  templateUrl: './words.component.html',
  styleUrl: './words.component.css'
})
export class WordsComponent implements OnInit {
  private readonly svc    = inject(LearningService);
  private readonly router = inject(Router);
  private readonly alert  = inject(SubscriptionAlertService);
  readonly state = inject(AppStateService);

  readonly isLoading      = signal(true);
  readonly letters        = signal<string[]>([]);
  readonly words          = signal<WordContentDto[]>([]);
  readonly selectedLetter = signal<string | null>(null);

  readonly filteredWords = computed(() => {
    const l = this.selectedLetter();
    return l ? this.words().filter(w => w.relatedLetter === l) : this.words();
  });

  ngOnInit(): void {
    const studentId = this.state.currentUser()?.id;
    this.svc.getWordsCatalog(studentId).subscribe({
      next: d => {
        this.words.set(d);
        this.letters.set([...new Set(d.map(w => w.relatedLetter))].sort());
        this.isLoading.set(false);
      },
      error: () => {
        this.svc.getWordLetters().subscribe({ next: ls => this.letters.set(ls), error: () => {} });
        this.svc.getWords().subscribe({
          next: d  => { this.words.set(d); this.isLoading.set(false); },
          error: () => this.isLoading.set(false)
        });
      }
    });
  }

  selectLetter(l: string | null): void { this.selectedLetter.set(l); }

  openWord(item: WordContentDto): void {
    if (item.isLocked) {
      this.alert.show('هذه الكلمة متاحة في الخطة المميزة. فعّل اشتراكك للوصول إلى جميع الكلمات.', 'Words');
      this.router.navigate(['/upgrade']);
      return;
    }
    this.router.navigate(['/learning/words', item.id]);
  }

  goBack(): void { this.router.navigate(['/learning/words-sentences']); }
}
