import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { StoryService } from '../../services/story';
import { AppStateService } from '../../services/app-state-service';
import { LevelProgressDto } from '../../models/story.models';

@Component({
  selector: 'app-levels',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  templateUrl: './levels.component.html',
  styleUrl: './levels.component.css'
})
export class LevelsComponent implements OnInit {
  private readonly router  = inject(Router);
  private readonly service = inject(StoryService);
  private readonly state   = inject(AppStateService);

  readonly isLoading     = signal(false);
  readonly levels        = signal<LevelProgressDto[]>([]);
  readonly retakeMsg     = signal<string | null>(null);
  readonly retakeError   = signal<string | null>(null);
  readonly retakeLoading = signal(false);

  // The level the student currently belongs to (not locked, highest progress)
  readonly currentLevelData = computed(() => {
    const ls = this.levels();
    // Find the player's active (non-locked) level with most progress
    const active = ls.filter(l => !l.locked);
    return active.at(-1) ?? null;
  });

  readonly isCurrentLevelComplete = computed(() => {
    const l = this.currentLevelData();
    return l != null && l.totalLessons > 0 && l.lessonsCompleted >= l.totalLessons;
  });

  ngOnInit(): void {
    const childName = this.state.childName();
    if (!childName) return;
    this.isLoading.set(true);
    this.service.getLevelProgress(childName).subscribe({
      next:  data => { this.levels.set(data); this.isLoading.set(false); },
      error: ()   => this.isLoading.set(false)
    });
  }

  openLevel(level: LevelProgressDto): void {
    if (!level.locked) this.router.navigate(['/levels', level.level, 'books']);
  }

  progressPct(level: LevelProgressDto): number {
    return level.totalLessons > 0
      ? Math.round(level.lessonsCompleted / level.totalLessons * 100) : 0;
  }

  starsEarned(level: LevelProgressDto): number {
    return level.totalStars > 0
      ? Math.round(level.stars / level.totalStars * 5) : 0;
  }

  requestRetake(): void {
    this.retakeMsg.set(null);
    this.retakeError.set(null);
    this.retakeLoading.set(true);
    this.service.requestPlacementRetake().subscribe({
      next: res => {
        this.retakeLoading.set(false);
        this.retakeMsg.set(res.message);
        // Navigate to placement after short delay so user sees the success message
        setTimeout(() => this.router.navigate(['/placement']), 2000);
      },
      error: (err: any) => {
        this.retakeLoading.set(false);
        this.retakeError.set(err?.error?.error ?? 'تعذّر طلب إعادة الاختبار.');
      }
    });
  }

  readonly starDots = [1, 2, 3, 4, 5];
}
