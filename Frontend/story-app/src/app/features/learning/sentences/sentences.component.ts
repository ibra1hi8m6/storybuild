import { Component, signal, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { LearningService } from '../../../services/learning.service';
import { AppStateService } from '../../../services/app-state-service';
import { SubscriptionAlertService } from '../../../core/subscription-alert.service';
import { SentenceContentDto } from '../../../models/learning.models';

@Component({
  selector: 'app-sentences',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  templateUrl: './sentences.component.html',
  styleUrl: './sentences.component.css'
})
export class SentencesComponent implements OnInit {
  private readonly svc    = inject(LearningService);
  private readonly router = inject(Router);
  private readonly alert  = inject(SubscriptionAlertService);
  readonly state = inject(AppStateService);

  readonly sentences = signal<SentenceContentDto[]>([]);
  readonly isLoading = signal(true);

  ngOnInit(): void {
    const studentId = this.state.currentUser()?.id;
    this.svc.getSentencesCatalog(studentId).subscribe({
      next: d  => { this.sentences.set(d); this.isLoading.set(false); },
      error: () => {
        this.svc.getSentences().subscribe({
          next: d  => { this.sentences.set(d); this.isLoading.set(false); },
          error: () => this.isLoading.set(false)
        });
      }
    });
  }

  open(item: SentenceContentDto): void {
    if (item.isLocked) {
      this.alert.show('هذا النشاط متاح في الخطة المميزة. فعّل اشتراكك للوصول إلى جميع أنشطة الجمل.', 'Sentences');
      this.router.navigate(['/upgrade']);
      return;
    }
    this.router.navigate(['/learning/sentences', item.id]);
  }

  goBack(): void { this.router.navigate(['/learning/words-sentences']); }
}
