import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MessagingService } from '../services/messaging.service';
import { AppStateService } from '../../../services/app-state-service';
import { WebAudioService } from '../../fluency/services/web-audio.service';

type SendMode = 'text' | 'sticker' | 'voice';

const STICKERS = ['⭐', '🌟', '🏆', '🎉', '👏', '💪', '🌈', '❤️', '🦁', '🐝'];

@Component({
  selector: 'app-teacher-feedback',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="feedback-page" dir="rtl">
      <div class="feedback-header">
        <h1><i class="bi bi-chat-heart-fill"></i> إرسال تشجيع للطالب</h1>
        @if (studentName()) {
          <p class="student-name">الطالب: <strong>{{ studentName() }}</strong></p>
        }
      </div>

      <div class="mode-tabs">
        <button class="tab-btn" [class.active]="mode() === 'text'" (click)="mode.set('text')">
          <i class="bi bi-chat-left-text"></i> رسالة نصية
        </button>
        <button class="tab-btn" [class.active]="mode() === 'sticker'" (click)="mode.set('sticker')">
          <i class="bi bi-emoji-smile"></i> ملصق
        </button>
        <button class="tab-btn" [class.active]="mode() === 'voice'" (click)="mode.set('voice')">
          <i class="bi bi-mic"></i> رسالة صوتية
        </button>
      </div>

      @if (mode() === 'text') {
        <div class="text-panel">
          <textarea class="msg-textarea" [(ngModel)]="textContent"
            placeholder="اكتب رسالة تشجيع..." rows="4" dir="rtl"></textarea>
          <button class="send-btn" (click)="sendText()" [disabled]="!textContent.trim() || sending()">
            <i class="bi bi-send-fill"></i> إرسال
          </button>
        </div>
      }

      @if (mode() === 'sticker') {
        <div class="sticker-panel">
          <p class="panel-hint">اختر ملصقاً</p>
          <div class="sticker-grid">
            @for (s of stickers; track s) {
              <button class="sticker-btn" [class.selected]="selectedSticker() === s"
                (click)="selectedSticker.set(s)">{{ s }}</button>
            }
          </div>
          <button class="send-btn" (click)="sendSticker()" [disabled]="!selectedSticker() || sending()">
            <i class="bi bi-send-fill"></i> إرسال الملصق
          </button>
        </div>
      }

      @if (mode() === 'voice') {
        <div class="voice-panel">
          @if (!audioService.isRecording()) {
            <button class="record-btn" (click)="startRecording()">
              <i class="bi bi-mic-fill"></i>
            </button>
            <p>اضغط للتسجيل</p>
          } @else {
            <div class="pulse-ring">
              <button class="record-btn recording" (click)="stopAndSend()">
                <i class="bi bi-stop-fill"></i>
              </button>
            </div>
            <p class="timer">{{ audioService.formattedDuration }}</p>
            <p class="hint-text">اضغط للإيقاف والإرسال</p>
          }
          @if (sending()) {
            <div class="spinner-border text-primary mt-3"></div>
          }
        </div>
      }

      @if (sent()) {
        <div class="success-banner">
          <i class="bi bi-check-circle-fill"></i> تم الإرسال بنجاح!
        </div>
      }
    </div>
  `,
  styleUrls: ['./teacher-feedback.component.css']
})
export class TeacherFeedbackComponent implements OnInit {
  mode = signal<SendMode>('text');
  textContent = '';
  selectedSticker = signal('');
  sending = signal(false);
  sent = signal(false);
  studentId = signal('');
  studentName = signal('');
  stickers = STICKERS;

  private route = inject(ActivatedRoute);
  private msgService = inject(MessagingService);
  private appState = inject(AppStateService);
  audioService = inject(WebAudioService);

  ngOnInit(): void {
    this.studentId.set(this.route.snapshot.queryParamMap.get('studentId') ?? '');
    this.studentName.set(this.route.snapshot.queryParamMap.get('name') ?? '');
  }

  async sendText(): Promise<void> {
    await this.doSend(() => {
      const teacher = this.appState.currentUser()!;
      return this.msgService.sendText(teacher.id, this.studentId(), this.textContent);
    });
    this.textContent = '';
  }

  async sendSticker(): Promise<void> {
    await this.doSend(() => {
      const teacher = this.appState.currentUser()!;
      return this.msgService.sendSticker(teacher.id, this.studentId(), this.selectedSticker());
    });
    this.selectedSticker.set('');
  }

  async startRecording(): Promise<void> {
    await this.audioService.startRecording();
  }

  async stopAndSend(): Promise<void> {
    const blob = await this.audioService.stopRecording();
    await this.doSend(() => {
      const teacher = this.appState.currentUser()!;
      return this.msgService.sendVoice(teacher.id, this.studentId(), blob);
    });
  }

  private async doSend(fn: () => Promise<unknown>): Promise<void> {
    this.sending.set(true);
    this.sent.set(false);
    try {
      await fn();
      this.sent.set(true);
      setTimeout(() => this.sent.set(false), 3000);
    } finally {
      this.sending.set(false);
    }
  }
}
