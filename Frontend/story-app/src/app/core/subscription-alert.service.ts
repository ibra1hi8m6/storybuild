import { Injectable, signal } from '@angular/core';

export interface SubscriptionAlert {
  message: string;
  feature: string;
}

@Injectable({ providedIn: 'root' })
export class SubscriptionAlertService {
  readonly alert = signal<SubscriptionAlert | null>(null);

  show(message: string, feature: string): void {
    this.alert.set({ message, feature });
  }

  dismiss(): void {
    this.alert.set(null);
  }
}
