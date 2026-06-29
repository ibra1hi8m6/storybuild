import { LessonSummary } from './lesson.models';
import { LessonAssignmentDto } from './assignments.models';

export interface TopContentDto {
  id:              string;
  title:           string;
  type:            string;
  completionCount: number;
  avgScore:        number;
}

export interface ExamHistoryDto {
  storyTitle:     string;
  score:          number;
  correctAnswers: number;
  totalQuestions: number;
  completedAt:    string;
}

export interface RecentActivityDto {
  childName:    string;
  activityType: string;
  title:        string;
  score?:       number;
  isAccepted?:  boolean;
  occurredAt:   string;
}

export interface PerformanceBandDto {
  band:  string;
  count: number;
  color: string;
}

export interface SkillBarDto {
  label: string;
  pct:   number;
}

export interface ClassroomStatsDto {
  name:        string;
  teacher:     string;
  students:    number;
  avgProgress: number;
}

export interface LevelDistributionDto {
  level: number;
  label: string;
  pct:   number;
  color: string;
}

export interface LevelProgressDto {
  level:            number;
  title:            string;
  subtitle:         string;
  icon:             string;
  tag:              string;
  locked:           boolean;
  stars:            number;
  totalStars:       number;
  lessonsCompleted: number;
  totalLessons:     number;
  avgScore:         number;
  unlockCondition:  string | null;
}

export interface StudentSummaryDto {
  id:               string;
  childName:        string;
  stars:            number;
  storiesRead:      number;
  lessonsCompleted: number;
  avgScore:         number;
  writingAccepted:  number;
  writingAttempts:  number;
  performanceLevel: string;
  lastActivity:     string | null;
  level?:           number;
  classroomName?:   string | null;
}

export interface StudentDashboardDto {
  childName:             string;
  stars:                 number;
  storiesRead:           number;
  lessonsCompleted:      number;
  examsCompleted:        number;
  avgScore:              number;
  writingAttempts:       number;
  writingAccepted:       number;
  writingAcceptanceRate: number;
  performanceLevel:      string;
  currentStreak:         number;
  weeklyActivity:        number[];
  inProgressLessons:     LessonSummary[];
  topStories:            TopContentDto[];
  topLessons:            TopContentDto[];
  examHistory:           ExamHistoryDto[];
  recentActivity:        RecentActivityDto[];
}

export interface ParentDashboardDto {
  childName:             string;
  stars:                 number;
  storiesRead:           number;
  lessonsCompleted:      number;
  examsCompleted:        number;
  avgScore:              number;
  writingAccepted:       number;
  writingAcceptanceRate: number;
  performanceLevel:      string;
  currentStreak:         number;
  weeklyActivity:        number[];
  inProgressLessons:     LessonSummary[];
  recentAssignments:     LessonAssignmentDto[];
  skillBars:             SkillBarDto[];
  topStories:            TopContentDto[];
  examHistory:           ExamHistoryDto[];
  recentActivity:        RecentActivityDto[];
  lettersCompleted:      number;
  lettersTotal:          number;
  wordsCompleted:        number;
  wordsTotal:            number;
  sentencesCompleted:    number;
  sentencesTotal:        number;
  lessonsTotal:          number;
  storiesTotal:          number;
}

export interface TeacherDashboardDto {
  totalStudents:    number;
  activeThisWeek:   number;
  avgClassScore:    number;
  topStories:       TopContentDto[];
  topLessons:       TopContentDto[];
  students:         StudentSummaryDto[];
  performanceBands: PerformanceBandDto[];
}

export interface SchoolDashboardDto {
  totalStudents:     number;
  totalTeachers:     number;
  activeThisWeek:    number;
  avgSchoolScore:    number;
  totalStories:      number;
  totalLessons:      number;
  topContent:        TopContentDto[];
  recentActivities:  RecentActivityDto[];
  performanceBands:  PerformanceBandDto[];
  classrooms:        ClassroomStatsDto[];
  levelDistribution: LevelDistributionDto[];
  lettersAvgPct?:    number;
  lettersTotal?:     number;
  wordsAvgPct?:      number;
  wordsTotal?:       number;
  sentencesAvgPct?:  number;
  sentencesTotal?:   number;
  lessonsAvgPct?:    number;
  lessonsTotal?:     number;
  storiesAvgPct?:    number;
  storiesTotal?:     number;
}
