export interface ProgressResponse {
  storyId:         string;
  childName:       string;
  currentPage:     number;
  totalQuestions:  number;
  correctAnswers:  number;
  scorePercentage: number;
  examCompleted:   boolean;
}
