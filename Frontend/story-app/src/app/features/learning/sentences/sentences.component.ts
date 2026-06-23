import { Component, signal, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { LearningService } from '../../../services/learning.service';
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

  readonly sentences = signal<SentenceContentDto[]>([]);
  readonly isLoading = signal(true);

  ngOnInit(): void {
    this.svc.getSentences().subscribe({
      next: d  => { this.sentences.set(d); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  open(id: string): void { this.router.navigate(['/learning/sentences', id]); }
  goBack(): void { this.router.navigate(['/learning/words-sentences']); }
}
