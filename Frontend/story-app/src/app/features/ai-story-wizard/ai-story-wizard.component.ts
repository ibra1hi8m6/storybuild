import { Component, signal, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StoryService } from '../../services/story';
import { AppStateService } from '../../services/app-state-service';

type StyleKey = 'cartoon' | 'fantasy' | 'realistic';

@Component({
  selector: 'app-ai-story-wizard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './ai-story-wizard.component.html',
  styleUrl: './ai-story-wizard.component.css'
})
export class AiStoryWizardComponent {
  private readonly router  = inject(Router);
  private readonly service = inject(StoryService);
  readonly state           = inject(AppStateService);

  readonly navItems = [
    { icon: '📊', label: 'لوحتي',          route: '/dashboard' },
    { icon: '✏️', label: 'محتوى التعلم',   route: '/learning' },
    // { icon: '📋', label: 'تقدّمي',          route: '/progress' },
    { icon: '🏆', label: 'إنجازاتي',       route: '/achievements' },
    { icon: '📖', label: 'قصصي',           route: '/my-stories' },
    { icon: '✨', label: 'قصص ذكية',       route: '/ai-story' },
  ];

  readonly step      = signal(1);
  readonly isLoading = signal(false);
  readonly error     = signal<string | null>(null);

  readonly selectedChar  = signal<string | null>(null);
  readonly selectedEnv   = signal<string | null>(null);
  readonly selectedStyle = signal<StyleKey | null>(null);
  charName = '';

  readonly characters = [
    { key: 'lion',      emoji: '🦁', label: 'أسد' },
    { key: 'girl',      emoji: '👧', label: 'بنت' },
    { key: 'boy',       emoji: '👦', label: 'ولد' },
    { key: 'wizard',    emoji: '🧙', label: 'ساحر' },
    { key: 'astronaut', emoji: '👨‍🚀', label: 'رائد فضاء' },
    { key: 'rabbit',    emoji: '🐰', label: 'أرنب' },
  ];

  readonly environments = [
    { key: 'forest', emoji: '🌲', label: 'غابة' },
    { key: 'city',   emoji: '🏙️', label: 'مدينة' },
    { key: 'space',  emoji: '🚀', label: 'فضاء' },
    { key: 'school', emoji: '🏫', label: 'مدرسة' },
    { key: 'ocean',  emoji: '🌊', label: 'محيط' },
  ];

  readonly styles: { key: StyleKey; emoji: string; label: string }[] = [
    { key: 'cartoon',   emoji: '🎨', label: 'كرتون' },
    { key: 'fantasy',   emoji: '✨', label: 'خيالي' },
    { key: 'realistic', emoji: '📷', label: 'واقعي' },
  ];

  isStepDone(n: number): boolean {
    if (n === 1) return this.selectedChar() !== null;
    if (n === 2) return this.selectedEnv() !== null;
    return false;
  }

  nextStep(): void { if (this.step() < 3) this.step.update(s => s + 1); }
  prevStep(): void { if (this.step() > 1) this.step.update(s => s - 1); }

  generate(): void {
    if (!this.selectedChar() || !this.selectedEnv() || !this.selectedStyle()) return;
    this.step.set(4);
    this.isLoading.set(true);
    this.error.set(null);

    const charLabel = this.characters.find(c => c.key === this.selectedChar())?.label ?? '';
    const childName = this.charName.trim() || 'بطل القصة';
    const theme     = this.environments.find(e => e.key === this.selectedEnv())?.label ?? '';

    const studentId = this.state.currentUser()?.id;
    this.service.generateStory({ childName, character: charLabel, theme, studentId }).subscribe({
      next: story => {
        this.isLoading.set(false);
        this.router.navigate(['/books', story.id, 'read']);
      },
      error: (err: any) => {
        this.error.set(err?.error?.error ?? err?.message ?? 'تعذّر توليد القصة. حاول مرة أخرى.');
        this.isLoading.set(false);
        this.step.set(3);
      }
    });
  }

  logout(): void { this.state.logout(); this.router.navigate(['/auth/login']); }

  get chosenTags(): { emoji: string; label: string }[] {
    const tags: { emoji: string; label: string }[] = [];
    const c = this.characters.find(x => x.key === this.selectedChar());
    const e = this.environments.find(x => x.key === this.selectedEnv());
    const s = this.styles.find(x => x.key === this.selectedStyle());
    if (c) tags.push({ emoji: c.emoji, label: this.charName || c.label });
    if (e) tags.push({ emoji: e.emoji, label: e.label });
    if (s) tags.push({ emoji: s.emoji, label: s.label });
    return tags;
  }
}
