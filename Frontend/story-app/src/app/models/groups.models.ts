export interface StudentGroupMemberDto {
  studentId:   string;
  studentName: string;
  addedAt:     string;
}

export interface StudentGroupDto {
  id:          string;
  name:        string;
  teacherId:   string;
  memberCount: number;
  createdAt:   string;
  members:     StudentGroupMemberDto[];
}

export interface CreateGroupRequest {
  name: string;
}

export interface AddGroupMemberRequest {
  studentId: string;
}
