export interface GenerateStoryRequest {
  childName: string;
  character: string;
  theme:     string;
  studentId?: string;
}

export interface StoryPage {
  pageId:     string;
  pageNumber: number;
  sentence:   string;
  imageUrl:   string;
  isUnlocked: boolean;
}

export interface StoryResponse {
  id:         string;
  title:      string;
  isApproved: boolean;
  pages:      StoryPage[];
  source?:    number; // 0 = AiGenerated, 1 = PdfImport
}

export interface UploadedStoryDto {
  id:            string;
  title:         string;
  coverImageUrl: string;
  pageCount:     number;
  createdAt:     string;
  pages:         StoryPage[];
}
