import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

interface TtsResponse {
  audioUrl: string;
  fromCache: boolean;
}

@Injectable({ providedIn: 'root' })
export class TtsService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  // In-memory session cache: key = "text|voice" → audioUrl
  private readonly sessionCache = new Map<string, string>();
  private currentAudio: HTMLAudioElement | null = null;

  async play(text: string, voice = 'Kore'): Promise<void> {
    if (!text?.trim()) return;
    this.stop();

    const key      = `${text}|${voice}`;
    let   audioUrl = this.sessionCache.get(key);

    if (!audioUrl) {
      try {
        const res = await firstValueFrom(
          this.http.post<TtsResponse>(`${this.api}/api/audio/tts`, { text, voice })
        );
        audioUrl = res.audioUrl;
        this.sessionCache.set(key, audioUrl);
      } catch {
        this.fallback(text);
        return;
      }
    }

    await this.playUrl(audioUrl);
  }

  stop(): void {
    if (this.currentAudio) {
      this.currentAudio.pause();
      this.currentAudio.currentTime = 0;
      this.currentAudio = null;
    }
    if (typeof window !== 'undefined' && 'speechSynthesis' in window) {
      window.speechSynthesis.cancel();
    }
  }

  private playUrl(url: string): Promise<void> {
    return new Promise(resolve => {
      const audio = new Audio(url);
      this.currentAudio = audio;
      audio.onended  = () => { this.currentAudio = null; resolve(); };
      audio.onerror  = () => { this.currentAudio = null; resolve(); };
      audio.play().catch(() => { this.currentAudio = null; resolve(); });
    });
  }

  private fallback(text: string): void {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) return;
    window.speechSynthesis.cancel();
    const u = new SpeechSynthesisUtterance(text);
    u.lang = 'ar-SA';
    u.rate = 0.85;
    window.speechSynthesis.speak(u);
  }
}
