import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SubscriptionUpgradeToastComponent } from './shared/subscription-upgrade-toast/subscription-upgrade-toast.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SubscriptionUpgradeToastComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('story-app');
}
