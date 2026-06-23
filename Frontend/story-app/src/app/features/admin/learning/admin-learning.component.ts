import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminSidebarComponent } from '../shared/admin-sidebar.component';
import { LearningService } from '../../../services/learning.service';
import { environment } from '../../../../environments/environment';
import {
  LetterContentDto,
  WordContentDto,
  SentenceContentDto
} from '../../../models/learning.models';

type Tab = 'letters' | 'words' | 'sentences';

@Component({
  selector: 'app-admin-learning',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminSidebarComponent],
  templateUrl: './admin-learning.component.html',
  styleUrl: './admin-learning.component.css'
})
export class AdminLearningComponent implements OnInit {
  private readonly svc = inject(LearningService);
  readonly api = environment.apiUrl;

  readonly activeTab    = signal<Tab>('letters');
  readonly isLoading    = signal(false);
  readonly isSaving     = signal(false);
  readonly saveError    = signal<string | null>(null);
  readonly saveSuccess  = signal(false);

  // ── Letters ──────────────────────────────────────────────────────────────
  readonly letters      = signal<LetterContentDto[]>([]);
  readonly editingLetter = signal<LetterContentDto | null>(null);
  readonly letterForm   = signal({
    letter: '', letterName: '', exampleWord: '',
    displaySentence: '', audioText: '', writingTarget: '',
    isPublished: true, sortOrder: 0
  });
  letterImageFile: File | null = null;
  letterImagePreview = signal<string | null>(null);

  // ── Words ─────────────────────────────────────────────────────────────────
  readonly words        = signal<WordContentDto[]>([]);
  readonly editingWord  = signal<WordContentDto | null>(null);
  readonly wordForm     = signal({
    displayWord: '', audioText: '', relatedLetter: '',
    isPublished: true, sortOrder: 0
  });
  wordImageFile: File | null = null;
  wordImagePreview = signal<string | null>(null);

  // ── Sentences ─────────────────────────────────────────────────────────────
  readonly sentences       = signal<SentenceContentDto[]>([]);
  readonly editingSentence = signal<SentenceContentDto | null>(null);
  readonly sentenceForm    = signal({
    option1: '', option1Audio: '',
    option2: '', option2Audio: '',
    option3: '', option3Audio: '',
    correctOptionIndex: 1, isPublished: true, sortOrder: 0
  });
  sentenceImageFile: File | null = null;
  sentenceImagePreview = signal<string | null>(null);

  readonly arabicLetters = [
    'أ','ب','ت','ث','ج','ح','خ','د','ذ','ر','ز','س','ش',
    'ص','ض','ط','ظ','ع','غ','ف','ق','ك','ل','م','ن','ه','و','ي'
  ];

  ngOnInit(): void { this.loadAll(); }

  loadAll(): void {
    this.isLoading.set(true);
    this.svc.getAllLetters().subscribe({ next: d => this.letters.set(d), error: () => {} });
    this.svc.getAllWords().subscribe({ next: d => this.words.set(d), error: () => {} });
    this.svc.getAllSentences().subscribe({ next: d => { this.sentences.set(d); this.isLoading.set(false); }, error: () => this.isLoading.set(false) });
  }

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
    this.resetForms();
  }

  // ── Letter actions ────────────────────────────────────────────────────────

  startEditLetter(l: LetterContentDto | null): void {
    this.editingLetter.set(l);
    this.letterImagePreview.set(l?.imagePath ?? null);
    this.letterForm.set(l ? {
      letter: l.letter, letterName: l.letterName, exampleWord: l.exampleWord,
      displaySentence: l.displaySentence, audioText: l.audioText,
      writingTarget: l.writingTarget, isPublished: l.isPublished, sortOrder: l.sortOrder
    } : {
      letter: '', letterName: '', exampleWord: '', displaySentence: '',
      audioText: '', writingTarget: '', isPublished: true, sortOrder: 0
    });
    this.letterImageFile = null;
  }

  onLetterImage(e: Event): void {
    const f = (e.target as HTMLInputElement).files?.[0] ?? null;
    this.letterImageFile = f;
    if (f) {
      const r = new FileReader();
      r.onload = ev => this.letterImagePreview.set(ev.target!.result as string);
      r.readAsDataURL(f);
    }
  }

  saveLetter(): void {
    const f = this.letterForm();
    const fd = new FormData();
    fd.append('letter', f.letter);
    fd.append('letterName', f.letterName);
    fd.append('exampleWord', f.exampleWord);
    fd.append('displaySentence', f.displaySentence);
    fd.append('audioText', f.audioText);
    fd.append('writingTarget', f.writingTarget);
    fd.append('isPublished', String(f.isPublished));
    fd.append('sortOrder', String(f.sortOrder));
    if (this.letterImageFile) fd.append('image', this.letterImageFile);

    this.isSaving.set(true);
    this.saveError.set(null);
    const editing = this.editingLetter();
    const obs = editing
      ? this.svc.updateLetter(editing.id, fd)
      : this.svc.createLetter(fd);

    obs.subscribe({
      next: () => { this.saveSuccess.set(true); setTimeout(() => this.saveSuccess.set(false), 2000); this.svc.getAllLetters().subscribe(d => this.letters.set(d)); this.editingLetter.set(null); this.isSaving.set(false); },
      error: err => { this.saveError.set(err?.error?.message ?? 'حدث خطأ'); this.isSaving.set(false); }
    });
  }

  deleteLetter(id: string): void {
    if (!confirm('هل تريد حذف هذا الحرف؟')) return;
    this.svc.deleteLetter(id).subscribe({ next: () => this.svc.getAllLetters().subscribe(d => this.letters.set(d)), error: () => {} });
  }

  toggleLetterPublish(l: LetterContentDto): void {
    this.svc.toggleLetterPublish(l.id, !l.isPublished).subscribe({
      next: () => this.svc.getAllLetters().subscribe(d => this.letters.set(d)), error: () => {}
    });
  }

  // ── Word actions ──────────────────────────────────────────────────────────

  startEditWord(w: WordContentDto | null): void {
    this.editingWord.set(w);
    this.wordImagePreview.set(w?.imagePath ?? null);
    this.wordForm.set(w ? {
      displayWord: w.displayWord, audioText: w.audioText,
      relatedLetter: w.relatedLetter, isPublished: w.isPublished, sortOrder: w.sortOrder
    } : { displayWord: '', audioText: '', relatedLetter: '', isPublished: true, sortOrder: 0 });
    this.wordImageFile = null;
  }

  onWordImage(e: Event): void {
    const f = (e.target as HTMLInputElement).files?.[0] ?? null;
    this.wordImageFile = f;
    if (f) {
      const r = new FileReader();
      r.onload = ev => this.wordImagePreview.set(ev.target!.result as string);
      r.readAsDataURL(f);
    }
  }

  saveWord(): void {
    const f = this.wordForm();
    const fd = new FormData();
    fd.append('displayWord', f.displayWord);
    fd.append('audioText', f.audioText);
    fd.append('relatedLetter', f.relatedLetter);
    fd.append('isPublished', String(f.isPublished));
    fd.append('sortOrder', String(f.sortOrder));
    if (this.wordImageFile) fd.append('image', this.wordImageFile);

    this.isSaving.set(true);
    this.saveError.set(null);
    const editing = this.editingWord();
    const obs = editing
      ? this.svc.updateWord(editing.id, fd)
      : this.svc.createWord(fd);

    obs.subscribe({
      next: () => { this.saveSuccess.set(true); setTimeout(() => this.saveSuccess.set(false), 2000); this.svc.getAllWords().subscribe(d => this.words.set(d)); this.editingWord.set(null); this.isSaving.set(false); },
      error: err => { this.saveError.set(err?.error?.message ?? 'حدث خطأ'); this.isSaving.set(false); }
    });
  }

  deleteWord(id: string): void {
    if (!confirm('هل تريد حذف هذه الكلمة؟')) return;
    this.svc.deleteWord(id).subscribe({ next: () => this.svc.getAllWords().subscribe(d => this.words.set(d)), error: () => {} });
  }

  // ── Sentence actions ──────────────────────────────────────────────────────

  startEditSentence(s: SentenceContentDto | null): void {
    this.editingSentence.set(s);
    this.sentenceImagePreview.set(s?.imagePath ?? null);
    this.sentenceForm.set(s ? {
      option1: s.option1, option1Audio: s.option1Audio,
      option2: s.option2, option2Audio: s.option2Audio,
      option3: s.option3, option3Audio: s.option3Audio,
      correctOptionIndex: s.correctOptionIndex, isPublished: s.isPublished, sortOrder: s.sortOrder
    } : {
      option1: '', option1Audio: '', option2: '', option2Audio: '',
      option3: '', option3Audio: '', correctOptionIndex: 1, isPublished: true, sortOrder: 0
    });
    this.sentenceImageFile = null;
  }

  onSentenceImage(e: Event): void {
    const f = (e.target as HTMLInputElement).files?.[0] ?? null;
    this.sentenceImageFile = f;
    if (f) {
      const r = new FileReader();
      r.onload = ev => this.sentenceImagePreview.set(ev.target!.result as string);
      r.readAsDataURL(f);
    }
  }

  saveSentence(): void {
    const f = this.sentenceForm();
    const fd = new FormData();
    fd.append('option1', f.option1); fd.append('option1Audio', f.option1Audio);
    fd.append('option2', f.option2); fd.append('option2Audio', f.option2Audio);
    fd.append('option3', f.option3); fd.append('option3Audio', f.option3Audio);
    fd.append('correctOptionIndex', String(f.correctOptionIndex));
    fd.append('isPublished', String(f.isPublished));
    fd.append('sortOrder', String(f.sortOrder));
    if (this.sentenceImageFile) fd.append('image', this.sentenceImageFile);

    this.isSaving.set(true);
    this.saveError.set(null);
    const editing = this.editingSentence();
    const obs = editing
      ? this.svc.updateSentence(editing.id, fd)
      : this.svc.createSentence(fd);

    obs.subscribe({
      next: () => { this.saveSuccess.set(true); setTimeout(() => this.saveSuccess.set(false), 2000); this.svc.getAllSentences().subscribe(d => this.sentences.set(d)); this.editingSentence.set(null); this.isSaving.set(false); },
      error: err => { this.saveError.set(err?.error?.message ?? 'حدث خطأ'); this.isSaving.set(false); }
    });
  }

  deleteSentence(id: string): void {
    if (!confirm('هل تريد حذف هذا النشاط؟')) return;
    this.svc.deleteSentence(id).subscribe({ next: () => this.svc.getAllSentences().subscribe(d => this.sentences.set(d)), error: () => {} });
  }

  updateLetterFormField(field: string, value: any): void {
    this.letterForm.update(f => ({ ...f, [field]: value }));
  }

  updateWordFormField(field: string, value: any): void {
    this.wordForm.update(f => ({ ...f, [field]: value }));
  }

  updateSentenceFormField(field: string, value: any): void {
    this.sentenceForm.update(f => ({ ...f, [field]: value }));
  }

  sentenceOptionText(n: number): string {
    const f = this.sentenceForm();
    if (n === 1) return f.option1;
    if (n === 2) return f.option2;
    return f.option3;
  }

  sentenceOptionAudio(n: number): string {
    const f = this.sentenceForm();
    if (n === 1) return f.option1Audio;
    if (n === 2) return f.option2Audio;
    return f.option3Audio;
  }

  private resetForms(): void {
    this.editingLetter.set(null);
    this.editingWord.set(null);
    this.editingSentence.set(null);
  }
}
