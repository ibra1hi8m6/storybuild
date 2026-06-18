import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface MessageDto {
  id: string;
  senderId: string;
  senderName: string;
  receiverId: string;
  content: string;
  type: 'Text' | 'Voice' | 'Sticker';
  isRead: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class MessagingService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/messages`;

  async sendText(senderId: string, receiverId: string, content: string): Promise<MessageDto> {
    return firstValueFrom(
      this.http.post<MessageDto>(`${this.base}/send`, { senderId, receiverId, content, type: 'Text' })
    );
  }

  async sendSticker(senderId: string, receiverId: string, sticker: string): Promise<MessageDto> {
    return firstValueFrom(
      this.http.post<MessageDto>(`${this.base}/send`, { senderId, receiverId, content: sticker, type: 'Sticker' })
    );
  }

  async sendVoice(senderId: string, receiverId: string, audio: Blob): Promise<MessageDto> {
    const form = new FormData();
    form.append('senderId', senderId);
    form.append('receiverId', receiverId);
    form.append('audio', audio, 'voice.webm');
    return firstValueFrom(this.http.post<MessageDto>(`${this.base}/send-voice`, form));
  }

  async getInbox(userId: string): Promise<MessageDto[]> {
    return firstValueFrom(this.http.get<MessageDto[]>(`${this.base}/inbox/${userId}`));
  }

  async getUnreadCount(userId: string): Promise<number> {
    const res = await firstValueFrom(this.http.get<{ count: number }>(`${this.base}/unread-count/${userId}`));
    return res.count;
  }

  async markRead(messageId: string, userId: string): Promise<void> {
    await firstValueFrom(this.http.post(`${this.base}/${messageId}/mark-read/${userId}`, {}));
  }
}
