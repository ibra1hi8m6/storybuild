import { Injectable, inject } from '@angular/core';
import { Router, NavigationStart } from '@angular/router';
import { filter } from 'rxjs/operators';
import { environment } from '../../environments/environment';

const LS_PREFIX   = 'tts_v1_';
const LS_MAX      = 150;
const SAMPLE_RATE = 24000;

@Injectable({ providedIn: 'root' })
export class TtsService {
  private currentAudio: HTMLAudioElement | null = null;
  private stopCurrent:  (() => void) | null     = null;
  private playAbort:    AbortController | null  = null;
  private playGeneration = 0;

  constructor() {
    inject(Router).events.pipe(
      filter(e => e instanceof NavigationStart)
    ).subscribe(() => this.stop());
  }

  // Play from bundled asset file; fall back to Gemini TTS if file missing
  async playFromAsset(assetUrl: string, fallbackText: string, voice = 'Kore'): Promise<void> {
    this.stop();
    try {
      const res = await fetch(assetUrl);
      if (res.ok) {
        const blob   = await res.blob();
        const url    = URL.createObjectURL(blob);
        await this.playUrlSilent(url).finally(() => URL.revokeObjectURL(url));
        return;
      }
    } catch { /* file missing — fall through to Gemini */ }
    await this.play(fallbackText, voice);
  }

  async play(text: string, voice = 'Kore', onStart?: () => void): Promise<void> {
    if (!text?.trim()) return;
    this.stop();

    const generation = this.playGeneration;
    const abort      = new AbortController();
    this.playAbort   = abort;

    const t   = text.trim();
    let   pcm = this.cacheGet(t);

    if (!pcm) {
      pcm = await this.callGemini(t, voice, abort.signal);
      if (pcm) this.cacheSet(t, pcm);
    }

    // If stop() was called while we were fetching, do not play
    if (this.playGeneration !== generation || abort.signal.aborted) return;

    if (pcm) {
      await this.playPcm(pcm, onStart);
    } else {
      await this.playBrowserSpeech(text, onStart);
    }
  }

  stop(): void {
    this.playGeneration++;
    this.playAbort?.abort();
    this.playAbort = null;
    // Capture audio reference before stopCurrent() clears it
    const audio = this.currentAudio;
    if (this.stopCurrent) { this.stopCurrent(); this.stopCurrent = null; }
    if (audio) { audio.pause(); this.currentAudio = null; }
    if ('speechSynthesis' in window) window.speechSynthesis.cancel();
  }

  // ── Gemini TTS ────────────────────────────────────────────────────────────

  private currentKeyIdx = 0;

  private async callGemini(text: string, voice: string, signal?: AbortSignal): Promise<string | null> {
    const keys = environment.geminiApiKeys;
    if (!keys?.length) return null;

    for (let attempt = 0; attempt < keys.length; attempt++) {
      if (signal?.aborted) return null;
      const key = keys[this.currentKeyIdx];

      // 6-second timeout per attempt so we fall back to browser TTS quickly
      const timeoutCtrl = new AbortController();
      const timeoutId   = setTimeout(() => timeoutCtrl.abort(), 6000);
      if (signal) signal.addEventListener('abort', () => timeoutCtrl.abort(), { once: true });

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
        const res = await fetch(url, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(body),
          signal: timeoutCtrl.signal
        });
        clearTimeout(timeoutId);

        if (signal?.aborted) return null;

        if (res.status === 429) {
          this.currentKeyIdx = (this.currentKeyIdx + 1) % keys.length;
          continue;
        }
        if (!res.ok) return null;

        const json = await res.json();
        return (json?.candidates?.[0]?.content?.parts?.[0]?.inlineData?.data as string) ?? null;
      } catch {
        clearTimeout(timeoutId);
        if (signal?.aborted) return null;
        // timeout or network error — try next key, then fall through to browser TTS
        continue;
      }
    }

    return null; // all keys exhausted or timed out → caller uses browser TTS
  }

  // ── PCM → WAV → play ─────────────────────────────────────────────────────

  private async playPcm(base64Pcm: string, onStart?: () => void): Promise<void> {
    const blobUrl = this.buildWavUrl(base64Pcm);
    try {
      await this.playUrlSilent(blobUrl, onStart);
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
    v.setUint16(20, 1,           true);
    v.setUint16(22, channels,    true);
    v.setUint32(24, SAMPLE_RATE, true);
    v.setUint32(28, byteRate,    true);
    v.setUint16(32, blockAlign,  true);
    v.setUint16(34, bitsPerSample, true);
    s(36, 'data'); v.setUint32(40, pcm.length, true);
    new Uint8Array(buf, 44).set(pcm);

    return URL.createObjectURL(new Blob([buf], { type: 'audio/wav' }));
  }

  // Plays a URL; resolves when done, never triggers browser TTS
  private playUrlSilent(url: string, onStart?: () => void): Promise<void> {
    return new Promise(resolve => {
      let settled = false;
      const done = () => {
        if (settled) return;
        settled = true;
        this.currentAudio = null;
        this.stopCurrent  = null;
        resolve();
      };

      const audio       = new Audio(url);
      this.currentAudio = audio;
      this.stopCurrent  = done;

      audio.onended = done;
      audio.onerror = done;
      audio.play().then(() => onStart?.()).catch(done);
    });
  }

  // ── Browser SpeechSynthesis fallback ─────────────────────────────────────

  private playBrowserSpeech(text: string, onStart?: () => void): Promise<void> {
    return new Promise(resolve => {
      if (!('speechSynthesis' in window)) { onStart?.(); resolve(); return; }

      window.speechSynthesis.cancel();

      let fired = false;
      const speak = () => {
        if (fired) return;
        fired = true;

        const utterance = new SpeechSynthesisUtterance(text);
        // Pick an Arabic voice if the browser has one; otherwise let the browser choose
        const voices  = window.speechSynthesis.getVoices();
        const arabic  = voices.find(v => v.lang.startsWith('ar'));
        if (arabic) utterance.voice = arabic;
        utterance.lang  = 'ar-SA';
        utterance.rate  = 0.85;
        utterance.pitch = 1.1;

        utterance.onstart = () => onStart?.();
        utterance.onend   = () => { this.stopCurrent = null; resolve(); };
        utterance.onerror = () => { this.stopCurrent = null; resolve(); };

        this.stopCurrent = () => { window.speechSynthesis.cancel(); resolve(); };
        window.speechSynthesis.speak(utterance);
      };

      // Voices may not be loaded yet on first use — wait for the event then speak
      if (window.speechSynthesis.getVoices().length) {
        speak();
      } else {
        window.speechSynthesis.onvoiceschanged = speak;
        setTimeout(speak, 500); // fallback if the event never fires
      }
    });
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
    } catch { /* storage quota exceeded */ }
  }
}
