import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MessagingService, MessageDto } from '../services/messaging.service';
import { AppStateService } from '../../../services/app-state-service';

@Component({
  selector: 'app-student-inbox',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="inbox-page" dir="rtl">
      <div class="inbox-header">
        <h1><i class="bi bi-chat-heart-fill"></i> رسائل المعلم</h1>
      </div>

      @if (loading()) {
        <div class="loading-state">
          <div class="spinner-border text-primary"></div>
        </div>
      } @else if (messages().length === 0) {
        <div class="empty-state">
          <div class="empty-icon">💌</div>
          <h3>لا توجد رسائل بعد</h3>
          <p>ستظهر هنا رسائل وتشجيعات معلمك</p>
        </div>
      } @else {
        <div class="message-list">
          @for (msg of messages(); track msg.id) {
            <div class="message-card" [class.unread]="!msg.isRead" (click)="markRead(msg)">
              <div class="msg-icon">
                @if (msg.type === 'Sticker') {
                  <span class="sticker-display">{{ msg.content }}</span>
                } @else if (msg.type === 'Voice') {
                  <i class="bi bi-mic-fill text-primary"></i>
                } @else {
                  <i class="bi bi-chat-left-text-fill text-primary"></i>
                }
              </div>
              <div class="msg-body">
                @if (msg.type === 'Text') {
                  <p class="msg-text">{{ msg.content }}</p>
                } @else if (msg.type === 'Voice') {
                  <audio [src]="msg.content" controls class="msg-audio"></audio>
                } @else {
                  <span class="sticker-big">{{ msg.content }}</span>
                }
                <span class="msg-time">{{ msg.createdAt | date:'d MMM, HH:mm' }}</span>
              </div>
              @if (!msg.isRead) {
                <span class="unread-dot"></span>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
  styleUrls: ['./student-inbox.component.css']
})
export class StudentInboxComponent implements OnInit {
  messages = signal<MessageDto[]>([]);
  loading = signal(true);

  private msgService = inject(MessagingService);
  private appState = inject(AppStateService);

  async ngOnInit(): Promise<void> {
    const user = this.appState.currentUser();
    if (!user) return;
    try {
      const data = await this.msgService.getInbox(user.id);
      this.messages.set(data);
    } finally {
      this.loading.set(false);
    }
  }

  async markRead(msg: MessageDto): Promise<void> {
    if (msg.isRead) return;
    const user = this.appState.currentUser();
    if (!user) return;
    await this.msgService.markRead(msg.id, user.id);
    this.messages.update(list =>
      list.map(m => m.id === msg.id ? { ...m, isRead: true } : m)
    );
  }
}
