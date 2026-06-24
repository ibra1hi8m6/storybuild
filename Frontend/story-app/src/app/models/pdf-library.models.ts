export interface PdfDocumentDto {
  id:                  string;
  title:               string;
  letter:              string;
  level:               number;
  pageCount:           number;
  embeddedPageCount:   number;
  embeddingsGenerated: boolean;
  uploadedAt:          string;
}

export interface PdfPageDto {
  id:         string;
  pageNumber: number;
  sentence:   string;
  imageUrl:   string;
  isEmbedded: boolean;
}

export interface PdfDocumentDetailDto extends PdfDocumentDto {
  pages: PdfPageDto[];
}

export interface EmbedResultDto {
  embeddedCount: number;
  message:       string;
}

export interface PdfLibraryStatsDto {
  totalPdfs:     number;
  totalPages:    number;
  totalEmbedded: number;
  lastUpdated:   string | null;
}
