import { Component, signal, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { LearningService } from '../../../services/learning.service';
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
  private readonly svc    = inject(LearningService);
  private readonly router = inject(Router);
  readonly api = environment.apiUrl;

  readonly letters   = signal<LetterContentDto[]>([]);
  readonly isLoading = signal(true);

  ngOnInit(): void {
    this.svc.getLetters().subscribe({
      next: d  => { this.letters.set(d); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  openLetter(id: string): void { this.router.navigate(['/learning/letters', id]); }
  goRecognition(): void { this.router.navigate(['/learning/letters/recognition']); }
  goBack(): void { this.router.navigate(['/learning']); }
}
