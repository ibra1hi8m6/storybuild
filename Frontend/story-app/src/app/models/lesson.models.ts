export interface LessonPage {
  pageId:      string;
  pageNumber:  number;
  sentence:    string;
  imageUrl:    string;
  isUnlocked:  boolean;
  isCoverPage: boolean;
}

export interface LessonSummary {
  id:            string;
  level:         number;
  letter:        string;
  letterName:    string;
  title:         string;
  coverImageUrl: string;
  pageCount:     number;
  isPublished:   boolean;
  status:        string;
  isLocked?:     boolean;
}

export interface LessonDetail {
  id:            string;
  level:         number;
  letter:        string;
  letterName:    string;
  title:         string;
  coverImageUrl: string;
  pages:         LessonPage[];
}

export interface ImportBookResponse {
  id:         string;
  title:      string;
  level:      number;
  letter:     string;
  letterName: string;
  pageCount:  number;
}

export interface AdminBooksPageDto {
  items:      LessonSummary[];
  totalCount: number;
  page:       number;
  pageSize:   number;
  totalPages: number;
}

export interface ManualPageDto {
  sentence: string;
}

export interface CreateManualBookRequest {
  title:      string;
  letterName: string;
  letter:     string;
  level:      number;
  pages:      ManualPageDto[];
}
