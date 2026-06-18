import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class WebAudioService {
  readonly isRecording = signal(false);
  readonly durationSeconds = signal(0);

  private mediaRecorder: MediaRecorder | null = null;
  private chunks: Blob[] = [];
  private stream: MediaStream | null = null;
  private timerInterval: ReturnType<typeof setInterval> | null = null;

  async startRecording(): Promise<void> {
    if (this.isRecording()) return;

    this.stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    this.chunks = [];
    this.durationSeconds.set(0);

    const mimeType = MediaRecorder.isTypeSupported('audio/webm;codecs=opus')
      ? 'audio/webm;codecs=opus'
      : 'audio/webm';

    this.mediaRecorder = new MediaRecorder(this.stream, { mimeType });
    this.mediaRecorder.ondataavailable = e => {
      if (e.data.size > 0) this.chunks.push(e.data);
    };

    this.mediaRecorder.start(250);
    this.isRecording.set(true);

    this.timerInterval = setInterval(() => {
      this.durationSeconds.update(s => s + 1);
    }, 1000);
  }

  stopRecording(): Promise<Blob> {
    return new Promise((resolve, reject) => {
      if (!this.mediaRecorder || !this.isRecording()) {
        reject(new Error('Not recording'));
        return;
      }

      if (this.timerInterval) {
        clearInterval(this.timerInterval);
        this.timerInterval = null;
      }

      this.mediaRecorder.onstop = () => {
        const blob = new Blob(this.chunks, { type: this.mediaRecorder!.mimeType });
        this.chunks = [];
        this.isRecording.set(false);
        this.stream?.getTracks().forEach(t => t.stop());
        resolve(blob);
      };

      this.mediaRecorder.stop();
    });
  }

  get formattedDuration(): string {
    const s = this.durationSeconds();
    return `${String(Math.floor(s / 60)).padStart(2, '0')}:${String(s % 60).padStart(2, '0')}`;
  }
}
