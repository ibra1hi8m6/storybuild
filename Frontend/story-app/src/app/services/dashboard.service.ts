import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  StudentDashboardDto, ParentDashboardDto, TeacherDashboardDto,
  SchoolDashboardDto, LevelProgressDto
} from '../models/dashboard.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  getStudentDashboard(studentId: string): Observable<StudentDashboardDto> {
    return this.http.get<StudentDashboardDto>(`${this.api}/api/dashboard/student/${studentId}`);
  }

  getParentDashboard(studentId: string): Observable<ParentDashboardDto> {
    return this.http.get<ParentDashboardDto>(`${this.api}/api/dashboard/parent/${studentId}`);
  }

  getTeacherStudentView(studentId: string): Observable<StudentDashboardDto> {
    return this.http.get<StudentDashboardDto>(`${this.api}/api/dashboard/teacher/student/${studentId}`);
  }

  getTeacherDashboard(): Observable<TeacherDashboardDto> {
    return this.http.get<TeacherDashboardDto>(`${this.api}/api/dashboard/teacher`);
  }

  getSchoolDashboard(): Observable<SchoolDashboardDto> {
    return this.http.get<SchoolDashboardDto>(`${this.api}/api/dashboard/school`);
  }

  getKnownStudentNames(): Observable<string[]> {
    return this.http.get<string[]>(`${this.api}/api/dashboard/students`);
  }

  getLevelProgress(studentId: string): Observable<LevelProgressDto[]> {
    return this.http.get<LevelProgressDto[]>(`${this.api}/api/dashboard/levels/progress/${studentId}`);
  }

  requestPlacementRetake(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.api}/api/placement/retake`, {});
  }

  updateStudentLevel(level: number): Observable<any> {
    return this.http.patch<any>(`${this.api}/api/auth/students/me/level`, { level });
  }

  getPlacementQuestions(): Observable<any[]> {
    return this.http.get<any[]>(`${this.api}/api/placement/questions`);
  }

  submitPlacement(request: { answers: { questionId: string; answer: string }[] }): Observable<any> {
    return this.http.post<any>(`${this.api}/api/placement/submit`, request);
  }

  getSchoolClassrooms(): Observable<any[]> {
    return this.http.get<any[]>(`${this.api}/api/classrooms`);
  }

  createClassroom(body: { name: string; level: number; teacherId: string }): Observable<any> {
    return this.http.post<any>(`${this.api}/api/classrooms`, body);
  }

  addStudentToClassroom(classroomId: string, studentId: string): Observable<any> {
    return this.http.post<any>(`${this.api}/api/classrooms/${classroomId}/students`, { studentId });
  }

  getMyTeacherClassrooms(): Observable<any[]> {
    return this.http.get<any[]>(`${this.api}/api/classrooms/my`);
  }

  getClassroomDetail(id: string): Observable<any> {
    return this.http.get<any>(`${this.api}/api/classrooms/${id}`);
  }

  editClassroom(id: string, body: { name?: string; level?: number; teacherId?: string }): Observable<any> {
    return this.http.put<any>(`${this.api}/api/classrooms/${id}`, body);
  }

  deleteClassroom(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/classrooms/${id}`);
  }

  removeStudentFromClassroom(classroomId: string, studentId: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/classrooms/${classroomId}/students/${studentId}`);
  }

  searchSchoolStudents(q: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.api}/api/classrooms/school-students?q=${encodeURIComponent(q)}`);
  }

  getClassroomsReport(): Observable<any[]> {
    return this.http.get<any[]>(`${this.api}/api/classrooms/report`);
  }
}
