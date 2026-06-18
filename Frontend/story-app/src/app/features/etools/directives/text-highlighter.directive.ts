import { Directive, ElementRef, EventEmitter, HostListener, Input, Output } from '@angular/core';

export interface SelectionEvent {
  selectedText: string;
  startOffset: number;
  endOffset: number;
  colorHex: string;
}

@Directive({ selector: '[appTextHighlighter]', standalone: true })
export class TextHighlighterDirective {
  @Input() highlightColor = '#FFE066';
  @Input() highlighterActive = false;
  @Output() textSelected = new EventEmitter<SelectionEvent>();

  constructor(private el: ElementRef<HTMLElement>) {}

  @HostListener('mouseup')
  @HostListener('touchend')
  onSelectionEnd(): void {
    if (!this.highlighterActive) return;
    const sel = window.getSelection();
    if (!sel || sel.rangeCount === 0 || sel.isCollapsed) return;

    const range = sel.getRangeAt(0);
    const container = this.el.nativeElement;
    if (!container.contains(range.commonAncestorContainer)) return;

    const selectedText = sel.toString().trim();
    if (!selectedText) return;

    const preRange = document.createRange();
    preRange.selectNodeContents(container);
    preRange.setEnd(range.startContainer, range.startOffset);
    const startOffset = preRange.toString().length;
    const endOffset = startOffset + selectedText.length;

    this.textSelected.emit({ selectedText, startOffset, endOffset, colorHex: this.highlightColor });
    sel.removeAllRanges();
  }
}
