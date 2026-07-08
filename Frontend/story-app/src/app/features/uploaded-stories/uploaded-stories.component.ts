import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { StoryService } from '../../services/story';
import { SubscriptionAlertService } from '../../core/subscription-alert.service';
import { UploadedStoryDto } from '../../models/story.models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-uploaded-stories',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  templateUrl: './uploaded-stories.component.html',
  styleUrl: './uploaded-stories.component.css'
})
export class UploadedStoriesComponent implements OnInit {
  private readonly svc    = inject(StoryService);
  private readonly router = inject(Router);
  private readonly alert  = inject(SubscriptionAlertService);
  readonly api = environment.apiUrl;

  readonly stories   = signal<UploadedStoryDto[]>([]);
  readonly isLoading = signal(true);

  ngOnInit() {
    this.svc.getUploadedStoriesCatalog().subscribe({
      next: s  => { this.stories.set(s); this.isLoading.set(false); },
      error: () => {
        this.svc.getUploadedStories().subscribe({
          next: s  => { this.stories.set(s); this.isLoading.set(false); },
          error: () => this.isLoading.set(false)
        });
      }
    });
  }

  open(item: UploadedStoryDto) {
    if (item.isLocked) {
      this.alert.show('هذه القصة متاحة في الخطة المميزة. فعّل اشتراكك للوصول إلى جميع القصص.', 'Stories');
      this.router.navigate(['/upgrade']);
      return;
    }
    this.router.navigate(['/uploaded-stories', item.id, 'journey']);
  }

  resolveImg(url: string) {
    if (!url) return '';
    return url.startsWith('http') ? url : `${this.api}${url}`;
  }

  goBack(): void { this.router.navigate(['/learning/booklets-stories']); }
}
