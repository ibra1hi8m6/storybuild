export interface LetterContentDto {
  id: string;
  letter: string;
  letterName: string;
  exampleWord: string;
  displaySentence: string;
  audioText: string;
  writingTarget: string;
  imagePath: string;
  isPublished: boolean;
  sortOrder: number;
}

export interface WordContentDto {
  id: string;
  displayWord: string;
  audioText: string;
  relatedLetter: string;
  imagePath?: string;
  isPublished: boolean;
  sortOrder: number;
  nextId?: string;
}

export interface SentenceContentDto {
  id: string;
  imagePath: string;
  option1: string;
  option1Audio: string;
  option2: string;
  option2Audio: string;
  option3: string;
  option3Audio: string;
  correctOptionIndex: number;
  isPublished: boolean;
  sortOrder: number;
  nextId?: string;
}

export type LearningContentType =
  | 'LetterSound'
  | 'LetterRecognition'
  | 'WordPractice'
  | 'SentencePractice'
  | 'Booklet'
  | 'Story';

export type LearningAttemptType = 'Writing' | 'Reading';

export interface LearningAttemptDto {
  id: string;
  childName: string;
  contentType: LearningContentType;
  contentId: string;
  attemptType: LearningAttemptType;
  expectedText: string;
  detectedText: string;
  score: number;
  isCorrect: boolean;
  feedbackText: string;
  feedbackAudio?: string;
  createdAt: string;
}

export interface WritingMistakeDto {
  type: string;
  expected: string;
  actual: string;
  description: string;
}

export interface WritingCorrectionResponse {
  extractedText: string;
  expectedSentence: string;
  similarityScore: number;
  isAccepted: boolean;
  message: string;
  displayMessage: string;
  spokenFeedback: string;
  mistakes: WritingMistakeDto[];
  tips: string[];
}

export interface SaveLearningAttemptRequest {
  childName: string;
  studentId?: string;
  contentType: number;
  contentId: string;
  attemptType: number;
  expectedText: string;
  detectedText: string;
  score: number;
  isCorrect: boolean;
  feedbackText: string;
  feedbackAudio?: string;
}
