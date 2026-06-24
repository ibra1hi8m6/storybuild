import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AssignmentDto, AssignmentSubmissionDto, TeacherAssignmentOverview } from '../models/assignments.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AssignmentsService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  getStudentAssignments(studentId: string): Observable<AssignmentDto[]> {
    return this.http.get<AssignmentDto[]>(`${this.api}/api/assignments/student/${studentId}`);
  }

  submitAssignment(assignmentId: string, body: {
    studentId: string; childName: string;
    pagesCompleted: number; totalPages: number;
    writingScore: number; isComplete: boolean;
  }): Observable<AssignmentSubmissionDto> {
    return this.http.post<AssignmentSubmissionDto>(
      `${this.api}/api/assignments/${assignmentId}/submit`, body);
  }

  getAssignmentSubmissions(assignmentId: string): Observable<AssignmentSubmissionDto[]> {
    return this.http.get<AssignmentSubmissionDto[]>(
      `${this.api}/api/assignments/${assignmentId}/submissions`);
  }

  getTeacherAssignmentOverview(teacherId: string): Observable<TeacherAssignmentOverview[]> {
    return this.http.get<TeacherAssignmentOverview[]>(
      `${this.api}/api/assignments/teacher/${teacherId}/overview`);
  }
}
