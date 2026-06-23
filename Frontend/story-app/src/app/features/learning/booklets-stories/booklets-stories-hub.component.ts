import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';

@Component({
  selector: 'app-booklets-stories-hub',
  standalone: true,
  imports: [NavbarComponent],
  templateUrl: './booklets-stories-hub.component.html',
  styleUrl: './booklets-stories-hub.component.css'
})
export class BookletsStoriesHubComponent {
  private readonly router = inject(Router);
  go(route: string): void { this.router.navigate([route]); }
  goBack(): void { this.router.navigate(['/learning']); }
}
