export interface AssignLessonRequest {
  lessonId:         string;
  targetType:       'Student' | 'Group' | 'Level';
  targetStudentId?: string;
  targetGroupId?:   string;
  targetLevel?:     number;
}

export interface LessonAssignmentDto {
  id:                string;
  lessonId:          string;
  lessonTitle:       string;
  targetType:        string;
  targetStudentId?:  string;
  targetStudentName?: string;
  targetGroupId?:    string;
  targetGroupName?:  string;
  assignedAt:        string;
}

export interface AssignmentDto {
  assignmentId:  string;
  lessonId:      string;
  lessonTitle:   string;
  letter:        string;
  level:         number;
  targetType:    string;
  assignedAt:    string;
  isSubmitted:   boolean;
  writingScore:  number;
  isComplete:    boolean;
}

export interface AssignmentSubmissionDto {
  submissionId:   string;
  assignmentId:   string;
  studentId:      string;
  childName:      string;
  pagesCompleted: number;
  totalPages:     number;
  writingScore:   number;
  isComplete:     boolean;
  submittedAt:    string;
}

export interface TeacherAssignmentOverview {
  assignmentId:    string;
  lessonId:        string;
  lessonTitle:     string;
  letter:          string;
  level:           number;
  targetType:      string;
  assignedAt:      string;
  submissionCount: number;
  completedCount:  number;
  avgScore:        number;
}
