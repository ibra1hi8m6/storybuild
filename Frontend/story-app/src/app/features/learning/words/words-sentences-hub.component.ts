import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';

@Component({
  selector: 'app-words-sentences-hub',
  standalone: true,
  imports: [NavbarComponent],
  templateUrl: './words-sentences-hub.component.html',
  styleUrl: './words-sentences-hub.component.css'
})
export class WordsSentencesHubComponent {
  private readonly router = inject(Router);
  go(route: string): void { this.router.navigate([route]); }
  goBack(): void { this.router.navigate(['/learning']); }
}
