import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { AdminSidebarComponent } from '../admin/shared/admin-sidebar.component';
import { StoryService } from '../../services/story';
import { UploadedStoryDto } from '../../models/story.models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-admin-uploaded-stories',
  standalone: true,
  imports: [FormsModule, DatePipe, AdminSidebarComponent],
  templateUrl: './admin-uploaded-stories.component.html',
  styleUrl: './admin-uploaded-stories.component.css'
})
export class AdminUploadedStoriesComponent implements OnInit {
  private readonly svc = inject(StoryService);
  readonly api = environment.apiUrl;

  readonly stories    = signal<UploadedStoryDto[]>([]);
  readonly isLoading  = signal(false);
  readonly message    = signal<string | null>(null);
  readonly isSuccess  = signal(false);

  uploadTitle = '';
  selectedFile: File | null = null;

  ngOnInit() { this.load(); }

  onFileChange(e: Event) {
    this.selectedFile = (e.target as HTMLInputElement).files?.[0] ?? null;
  }

  upload() {
    if (!this.selectedFile) { this.showMsg('يرجى اختيار ملف PDF', false); return; }
    if (!this.uploadTitle.trim()) { this.showMsg('يرجى إدخال عنوان القصة', false); return; }

    this.isLoading.set(true);
    this.svc.uploadStoryPdf(this.uploadTitle.trim(), this.selectedFile).subscribe({
      next: s => {
        this.showMsg(`تم رفع "${s.title}" — ${s.pageCount} صفحة`, true);
        this.uploadTitle = '';
        this.selectedFile = null;
        this.isLoading.set(false);
        this.load();
      },
      error: err => {
        this.showMsg(err?.error?.error ?? 'فشل رفع القصة', false);
        this.isLoading.set(false);
      }
    });
  }

  delete(id: string, title: string) {
    if (!confirm(`هل تريد حذف "${title}"؟`)) return;
    this.svc.deleteUploadedStory(id).subscribe({
      next: () => { this.showMsg('تم الحذف', true); this.load(); },
      error: () => this.showMsg('فشل الحذف', false)
    });
  }

  resolveImg(url: string) {
    if (!url) return '';
    return url.startsWith('http') ? url : `${this.api}${url}`;
  }

  private load() {
    this.svc.getUploadedStories().subscribe({
      next: s => this.stories.set(s),
      error: () => {}
    });
  }

  private showMsg(msg: string, ok: boolean) {
    this.message.set(msg);
    this.isSuccess.set(ok);
    setTimeout(() => this.message.set(null), 4000);
  }
}
