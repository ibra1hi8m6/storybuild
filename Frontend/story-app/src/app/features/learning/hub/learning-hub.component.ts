import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';

@Component({
  selector: 'app-learning-hub',
  standalone: true,
  imports: [NavbarComponent],
  templateUrl: './learning-hub.component.html',
  styleUrl: './learning-hub.component.css'
})
export class LearningHubComponent {
  private readonly router = inject(Router);

  go(route: string): void { this.router.navigate([route]); }
}
