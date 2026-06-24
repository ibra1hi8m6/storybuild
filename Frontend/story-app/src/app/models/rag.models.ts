export interface KnowledgeDocumentDto {
  id:           string;
  fileName:     string;
  documentType: string;
  letter?:      string;
  level?:       number;
  tags?:        string;
  chunkCount:   number;
  ingestedAt:   string;
}

export interface RagSearchResult {
  chunkText:  string;
  score:      number;
  sourceFile: string;
  letter?:    string;
  level?:     number;
}

export interface GenerateLessonRequest {
  topic:      string;
  letter?:    string;
  level?:     number;
  childName?: string;
}

export interface IngestDocumentResponse {
  documentId: string;
  fileName:   string;
  chunkCount: number;
  message:    string;
}

export interface RagPageChunkDto {
  id:         string;
  sourceFile: string;
  pageNumber: number;
  sentence:   string;
  wordCount:  number;
  imageUrl:   string;
  level:      number;
  letter:     string;
  letterName: string;
}

export interface IngestEducationalPdfRequest {
  level:      number;
  letter:     string;
  letterName: string;
}

export interface GenerateLessonV2Request {
  topic:            string;
  letter?:          string;
  level:            number;
  creatorId?:       string;
  creatorRole:      string;
  targetStudentId?: string;
  targetGroupId?:   string;
}
