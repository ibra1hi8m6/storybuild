export type QuizType = 0 | 1 | 2 | 3;
export const QuizType = {
  MCQ:      0 as QuizType,
  Matching: 1 as QuizType,
  DragDrop: 2 as QuizType,
  Ordering: 3 as QuizType,
};

export interface MatchPair {
  left:  string;
  right: string;
}

export interface QuestionDto {
  questionId:     string;
  questionNumber: number;
  type:           QuizType;
  text:           string;
  optionA?:       string;
  optionB?:       string;
  optionC?:       string;
  optionD?:       string;
  dataJson?:      string;
  imageUrl?:      string;
}

export interface ExamResponse {
  examId:    string;
  storyId:   string;
  questions: QuestionDto[];
}

export interface SubmitAnswer {
  questionId:   string;
  chosenAnswer: string;
}

export interface SubmitExamRequest {
  examId:    string;
  childName: string;
  answers:   SubmitAnswer[];
}

export interface AnswerFeedback {
  questionId:    string;
  type:          QuizType;
  chosenAnswer:  string;
  correctAnswer: string;
  isCorrect:     boolean;
}

export interface ExamResult {
  totalQuestions:  number;
  correctAnswers:  number;
  scorePercentage: number;
  feedback:        AnswerFeedback[];
}
