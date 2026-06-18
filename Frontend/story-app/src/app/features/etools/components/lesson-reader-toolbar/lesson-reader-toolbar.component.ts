import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ToolMode = 'none' | 'highlight-yellow' | 'highlight-green' | 'highlight-blue' | 'stamp';

@Component({
  selector: 'app-lesson-reader-toolbar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="etools-toolbar" dir="rtl">
      <span class="toolbar-label">أدوات القراءة</span>

      <button class="tool-btn"
        [class.active]="activeTool() === 'highlight-yellow'"
        (click)="setTool('highlight-yellow')"
        title="تظليل أصفر">
        <span class="color-dot yellow"></span>
      </button>

      <button class="tool-btn"
        [class.active]="activeTool() === 'highlight-green'"
        (click)="setTool('highlight-green')"
        title="تظليل أخضر">
        <span class="color-dot green"></span>
      </button>

      <button class="tool-btn"
        [class.active]="activeTool() === 'highlight-blue'"
        (click)="setTool('highlight-blue')"
        title="تظليل أزرق">
        <span class="color-dot blue"></span>
      </button>

      <div class="divider"></div>

      <button class="tool-btn stamp-btn"
        [class.active]="activeTool() === 'stamp'"
        (click)="setTool('stamp')"
        title="ختم نجمة">
        ⭐
      </button>

      @if (activeTool() !== 'none') {
        <button class="tool-btn cancel-btn" (click)="setTool('none')" title="إلغاء">
          <i class="bi bi-x-circle"></i>
        </button>
      }

      <div class="divider"></div>

      <button class="tool-btn journal-btn" (click)="openJournal.emit()" title="مفرداتي">
        <i class="bi bi-journal-bookmark-fill"></i>
      </button>
    </div>
  `,
  styleUrls: ['./lesson-reader-toolbar.component.css']
})
export class LessonReaderToolbarComponent {
  @Output() toolChanged = new EventEmitter<ToolMode>();
  @Output() openJournal = new EventEmitter<void>();

  activeTool = signal<ToolMode>('none');

  colorMap: Record<string, string> = {
    'highlight-yellow': '#FFE066',
    'highlight-green':  '#B7F0A0',
    'highlight-blue':   '#A0D4F5',
  };

  setTool(tool: ToolMode): void {
    const next = this.activeTool() === tool ? 'none' : tool;
    this.activeTool.set(next);
    this.toolChanged.emit(next);
  }

  get activeColor(): string {
    return this.colorMap[this.activeTool()] ?? '';
  }
}
