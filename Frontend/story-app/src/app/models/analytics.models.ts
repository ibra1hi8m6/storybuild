export interface SkillStat  { attempts: number; correct: number; }
export interface LessonStat { title: string; letter: string; attempts: number; correct: number; }
export interface WeaknessMap {
  letters: Record<string, SkillStat>;
  lessons: Record<string, LessonStat>;
}

export interface WeakLetterDto {
  letter:       string;
  attempts:     number;
  correct:      number;
  accuracy:     number;
  activityType: string;
  lastSeenAt:   string;
}

export interface StudentAnalyticsDto {
  studentId:       string;
  childName:       string;
  level:           number;
  overallAccuracy: number;
  weakLetters:     WeakLetterDto[];
}

export interface AnalyticsSummaryDto {
  totalStudents:         number;
  classAvgAccuracy:      number;
  students:              StudentAnalyticsDto[];
  mostCommonWeakLetters: WeakLetterDto[];
}
