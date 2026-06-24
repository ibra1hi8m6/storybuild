import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { StudentGroupDto } from '../models/groups.models';
import { AssignLessonRequest, LessonAssignmentDto } from '../models/assignments.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class GroupsService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  getTeacherGroups(teacherId: string): Observable<StudentGroupDto[]> {
    return this.http.get<StudentGroupDto[]>(`${this.api}/api/groups/teacher/${teacherId}`);
  }

  createGroup(teacherId: string, name: string): Observable<StudentGroupDto> {
    return this.http.post<StudentGroupDto>(
      `${this.api}/api/groups/teacher/${teacherId}`, { name }
    );
  }

  addGroupMember(groupId: string, studentId: string): Observable<void> {
    return this.http.post<void>(`${this.api}/api/groups/${groupId}/members`, { studentId });
  }

  removeGroupMember(groupId: string, studentId: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/groups/${groupId}/members/${studentId}`);
  }

  deleteGroup(groupId: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/groups/${groupId}`);
  }

  assignLesson(req: AssignLessonRequest): Observable<{ id: string; message: string }> {
    return this.http.post<{ id: string; message: string }>(`${this.api}/api/groups/assign`, req);
  }

  getAssignedLessons(studentId: string): Observable<LessonAssignmentDto[]> {
    return this.http.get<LessonAssignmentDto[]>(`${this.api}/api/groups/assigned/student/${studentId}`);
  }

  getTeacherAssignments(teacherId: string): Observable<LessonAssignmentDto[]> {
    return this.http.get<LessonAssignmentDto[]>(`${this.api}/api/groups/assignments/teacher/${teacherId}`);
  }
}
