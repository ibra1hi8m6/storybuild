import { Component, signal, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { LearningService } from '../../../services/learning.service';
import { AppStateService } from '../../../services/app-state-service';
import { SubscriptionAlertService } from '../../../core/subscription-alert.service';
import { LetterContentDto } from '../../../models/learning.models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-letters',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  templateUrl: './letters.component.html',
  styleUrl: './letters.component.css'
})
export class LettersComponent implements OnInit {
  private readonly svc   = inject(LearningService);
  private readonly router = inject(Router);
  private readonly alert  = inject(SubscriptionAlertService);
  readonly state = inject(AppStateService);
  readonly api = environment.apiUrl;

  readonly letters   = signal<LetterContentDto[]>([]);
  readonly isLoading = signal(true);

  ngOnInit(): void {
    const studentId = this.state.currentUser()?.id;
    this.svc.getLettersCatalog(studentId).subscribe({
      next: d => {
        console.log(`[Letters] catalog: total=${d.length}, locked=${d.filter(x => x.isLocked).length}`);
        this.letters.set(d);
        this.isLoading.set(false);
      },
      error: () => {
        this.svc.getLetters().subscribe({
          next: d  => { this.letters.set(d); this.isLoading.set(false); },
          error: () => this.isLoading.set(false)
        });
      }
    });
  }

  openLetter(item: LetterContentDto): void {
    if (item.isLocked) {
      this.alert.show('هذا الحرف متاح في الخطة المميزة. فعّل اشتراكك للوصول إلى جميع الحروف.', 'Letters');
      this.router.navigate(['/upgrade']);
      return;
    }
    this.router.navigate(['/learning/letters', item.id]);
  }

  goRecognition(): void { this.router.navigate(['/learning/letters/recognition']); }
  goBack(): void { this.router.navigate(['/learning']); }
}
