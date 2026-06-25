export interface ProgressResponse {
  storyId:         string;
  studentId:       string;
  currentPage:     number;
  totalQuestions:  number;
  correctAnswers:  number;
  scorePercentage: number;
  examCompleted:   boolean;
}
