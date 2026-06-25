import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

const LS_PREFIX   = 'tts_v1_';
const LS_MAX      = 150;
const SAMPLE_RATE = 24000;

@Injectable({ providedIn: 'root' })
export class TtsService {
  private currentAudio: HTMLAudioElement | null = null;
  private stopCurrent:  (() => void) | null     = null;

  async play(text: string, voice = 'Kore', onStart?: () => void): Promise<void> {
    if (!text?.trim()) return;
    this.stop();

    const t   = text.trim();
    let   pcm = this.cacheGet(t);

    if (!pcm) {
      pcm = await this.callGemini(t, voice);
      if (pcm) this.cacheSet(t, pcm);
    }

    if (pcm) {
      await this.playPcm(pcm, t, onStart);
    } else {
      onStart?.();
      this.fallback(t);
    }
  }

  stop(): void {
    if (this.stopCurrent) { this.stopCurrent(); this.stopCurrent = null; }
    if (this.currentAudio) { this.currentAudio.pause(); this.currentAudio = null; }
    if (typeof window !== 'undefined' && 'speechSynthesis' in window)
      window.speechSynthesis.cancel();
  }

  // ── Gemini TTS direct call ────────────────────────────────────────────────

  private async callGemini(text: string, voice: string): Promise<string | null> {
    const key = environment.geminiApiKey;
    if (!key || key === 'YOUR_GEMINI_API_KEY_HERE') return null;
    try {
      const url  = `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-tts:generateContent?key=${key}`;
      const body = {
        contents: [{
          parts: [{ text: `اقرأ هذا النص العربي بصوت واضح وودي للأطفال: ${text}` }]
        }],
        generationConfig: {
          responseModalities: ['AUDIO'],
          speechConfig: { voiceConfig: { prebuiltVoiceConfig: { voiceName: voice } } }
        }
      };
      const res  = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });
      if (!res.ok) return null;
      const json = await res.json();
      return (json?.candidates?.[0]?.content?.parts?.[0]?.inlineData?.data as string) ?? null;
    } catch {
      return null;
    }
  }

  // ── PCM → WAV → Blob URL → HTMLAudioElement ───────────────────────────────

  private async playPcm(base64Pcm: string, fallbackText: string, onStart?: () => void): Promise<void> {
    const blobUrl = this.buildWavUrl(base64Pcm);
    try {
      await this.playUrl(blobUrl, fallbackText, onStart);
    } finally {
      URL.revokeObjectURL(blobUrl);
    }
  }

  private buildWavUrl(base64Pcm: string): string {
    const binary = atob(base64Pcm);
    const pcm    = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) pcm[i] = binary.charCodeAt(i);

    const channels = 1, bitsPerSample = 16;
    const byteRate   = SAMPLE_RATE * channels * bitsPerSample / 8;
    const blockAlign = channels * bitsPerSample / 8;

    const buf = new ArrayBuffer(44 + pcm.length);
    const v   = new DataView(buf);
    const s   = (off: number, str: string) =>
      [...str].forEach((c, i) => v.setUint8(off + i, c.charCodeAt(0)));

    s(0,  'RIFF'); v.setUint32(4,  36 + pcm.length, true);
    s(8,  'WAVE');
    s(12, 'fmt ');            v.setUint32(16, 16,          true);
    v.setUint16(20, 1,           true); // PCM
    v.setUint16(22, channels,    true);
    v.setUint32(24, SAMPLE_RATE, true);
    v.setUint32(28, byteRate,    true);
    v.setUint16(32, blockAlign,  true);
    v.setUint16(34, bitsPerSample, true);
    s(36, 'data'); v.setUint32(40, pcm.length, true);
    new Uint8Array(buf, 44).set(pcm);

    return URL.createObjectURL(new Blob([buf], { type: 'audio/wav' }));
  }

  private playUrl(url: string, fallbackText: string, onStart?: () => void): Promise<void> {
    return new Promise(resolve => {
      let settled = false;
      const done = (doFallback: boolean) => {
        if (settled) return;
        settled = true;
        this.currentAudio = null;
        this.stopCurrent  = null;
        if (doFallback) this.fallback(fallbackText);
        resolve();
      };

      const audio       = new Audio(url);
      this.currentAudio = audio;
      this.stopCurrent  = () => done(false);

      audio.onended = () => done(false);
      audio.onerror = () => done(true);
      audio.play().then(() => onStart?.()).catch(() => done(true));
    });
  }

  // ── Browser speechSynthesis fallback ─────────────────────────────────────

  private fallback(text: string): void {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) return;
    window.speechSynthesis.cancel();
    const u = new SpeechSynthesisUtterance(text);
    u.lang = 'ar-SA';
    u.rate = 0.85;
    window.speechSynthesis.speak(u);
  }

  // ── localStorage cache ────────────────────────────────────────────────────

  private cacheGet(text: string): string | null {
    try { return localStorage.getItem(LS_PREFIX + text); }
    catch { return null; }
  }

  private cacheSet(text: string, pcm: string): void {
    try {
      const keys = Object.keys(localStorage).filter(k => k.startsWith(LS_PREFIX));
      if (keys.length >= LS_MAX) localStorage.removeItem(keys[0]);
      localStorage.setItem(LS_PREFIX + text, pcm);
    } catch { /* storage quota exceeded — skip cache */ }
  }
}
