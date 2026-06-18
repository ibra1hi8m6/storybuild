import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class GameAudioService {
  private ctx: AudioContext | null = null;

  private getCtx(): AudioContext {
    if (!this.ctx) this.ctx = new AudioContext();
    return this.ctx;
  }

  playSuccess(): void {
    this.playTone([523, 659, 784], 0.15);
  }

  playError(): void {
    this.playTone([220, 196], 0.2);
  }

  private playTone(freqs: number[], dur: number): void {
    const ctx = this.getCtx();
    freqs.forEach((f, i) => {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.frequency.value = f;
      osc.type = 'sine';
      gain.gain.setValueAtTime(0.3, ctx.currentTime + i * dur);
      gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + i * dur + dur);
      osc.start(ctx.currentTime + i * dur);
      osc.stop(ctx.currentTime + i * dur + dur);
    });
  }
}
