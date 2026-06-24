export interface WritingMistake {
  type:        string;
  expected:    string;
  actual:      string;
  description: string;
}

export interface WritingCorrectionResponse {
  extractedText:    string;
  expectedSentence: string;
  similarityScore:  number;
  isAccepted:       boolean;
  message:          string;
  displayMessage:   string;
  spokenFeedback:   string;
  mistakes:         WritingMistake[];
  tips:             string[];
}

export interface WritingAttemptHistory {
  id:               string;
  lessonPageId:     string;
  expectedSentence: string;
  extractedText:    string;
  similarityScore:  number;
  isAccepted:       boolean;
  attemptNumber:    number;
  displayMessage:   string;
  mistakes:         WritingMistake[];
  tips:             string[];
  imageUrl:         string;
  attemptedAt:      string;
}

export interface ReadingAttemptHistory {
  recordingId:        string;
  pageId:             string;
  pageType:           string;
  expectedText:       string;
  extractedText:      string;
  wcpm:               number;
  accuracyScore:      number;
  isAccepted:         boolean;
  attemptNumber:      number;
  displayMessage:     string;
  mispronouncedWords: string[];
  tips:               string[];
  audioUrl:           string;
  createdAt:          string;
}
