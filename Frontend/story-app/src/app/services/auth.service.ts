import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';

export interface AuthResponse {
  token:       string;
  userId:      string;
  name:        string;
  role:        string;
  expiresAt:   string;
  schoolManagerId?: string;
}

export interface StudentAuthResponse {
  token:        string;
  studentId:    string;
  name:         string;
  level:        number;
  placementDone:boolean;
  expiresAt:    string;
  avatarEmoji?: string | null;
}

export interface StudentSummary {
  id:            string;
  name:          string;
  age:           number;
  username:      string;
  level:         number;
  placementDone: boolean;
  avatarUrl:     string | null;
  avatarEmoji?:  string | null;
}

export interface CreateStudentRequest {
  name:          string;
  age:           number;
  username:      string;
  nationalId:    string;
  imagePin1:     number;
  imagePin2:     number | null;
  level?:        number;
  avatarEmoji?:  string;
  classroomId?:  string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http   = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly api    = environment.apiUrl;

  private readonly TOKEN_KEY = 'lughati_token';
  private readonly USER_KEY  = 'lughati_user';

  // Signals
  private readonly _token = signal<string | null>(
    typeof localStorage !== 'undefined' ? localStorage.getItem(this.TOKEN_KEY) : null
  );
  private readonly _user = signal<AuthResponse | StudentAuthResponse | null>(
    this.loadUserFromStorage()
  );

  readonly isLoggedIn = computed(() => this._token() !== null);
  readonly currentUser = this._user.asReadonly();
  readonly token = this._token.asReadonly();

  readonly userRole = computed(() => {
    const u = this._user();
    if (!u) return '';
    return 'role' in u ? u.role : 'student';
  });

  readonly isStudent     = computed(() => this.userRole() === 'student');
  readonly isParent      = computed(() => this.userRole() === 'parent');
  readonly isTeacher     = computed(() => this.userRole() === 'teacher');
  readonly isSchoolAdmin = computed(() => this.userRole() === 'schooladmin');
  readonly isAdmin       = computed(() =>
    this.userRole() === 'systemadmin' || this.userRole() === 'admin');

  // ── Adult register (self — persists session) ───────────────────────────────
  register(body: { fullName: string; email: string; password: string; role: string; schoolManagerId?: string }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.api}/api/auth/register`, body).pipe(
      tap(res => this.persistSession(res))
    );
  }

  // ── Create account on behalf of someone (school admin creates teacher) ─────
  registerWithoutSession(body: { fullName: string; email: string; password: string; role: string; schoolManagerId?: string }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.api}/api/auth/register`, body);
  }

  // ── Adult login ────────────────────────────────────────────────────────────
  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.api}/api/auth/login`, { email, password }).pipe(
      tap(res => this.persistSession(res))
    );
  }

  // ── Student login (username + image PIN) ───────────────────────────────────
  studentLogin(username: string, imagePin1: number, imagePin2: number | null): Observable<StudentAuthResponse> {
    return this.http.post<StudentAuthResponse>(`${this.api}/api/auth/students/login`, {
      username, imagePin1, imagePin2
    }).pipe(
      tap(res => this.persistStudentSession(res))
    );
  }

  // ── Create student (called by logged-in parent/teacher) ────────────────────
  createStudent(req: CreateStudentRequest): Observable<StudentAuthResponse> {
    return this.http.post<StudentAuthResponse>(`${this.api}/api/auth/students`, req);
  }

  // ── List children / students ───────────────────────────────────────────────
  getMyStudents(): Observable<StudentSummary[]> {
    return this.http.get<StudentSummary[]>(`${this.api}/api/auth/students`);
  }

  // ── Delete student (teacher or parent who owns the student) ──────────────────
  deleteStudent(studentId: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/auth/students/${studentId}`);
  }

  // ── Parent/teacher updates a child's level ─────────────────────────────────
  updateChildLevel(studentId: string, level: number): Observable<StudentAuthResponse> {
    return this.http.patch<StudentAuthResponse>(`${this.api}/api/auth/students/${studentId}/level`, { level });
  }

  // ── Student updates own level after placement test ─────────────────────────
  updateMyLevel(level: number): Observable<any> {
    return this.http.patch<any>(`${this.api}/api/auth/students/me/level`, { level });
  }

  // ── School admin: get teachers belonging to this school ───────────────────
  getSchoolTeachers(): Observable<{ id: string; name: string; email: string; studentCount: number }[]> {
    return this.http.get<{ id: string; name: string; email: string; studentCount: number }[]>(`${this.api}/api/auth/school/teachers`);
  }

  // ── School admin: reset a teacher's password ──────────────────────────────
  resetTeacherPassword(teacherId: string, newPassword: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.api}/api/auth/school/teachers/${teacherId}/reset-password`,
      { newPassword }
    );
  }

  // ── Logout ─────────────────────────────────────────────────────────────────
  logout(): void {
    this._token.set(null);
    this._user.set(null);
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(this.TOKEN_KEY);
      localStorage.removeItem(this.USER_KEY);
      localStorage.removeItem('lughati_child');
    }
    this.router.navigate(['/auth/login']);
  }

  // ── Internal helpers ───────────────────────────────────────────────────────
  private persistSession(res: AuthResponse): void {
    this._token.set(res.token);
    this._user.set(res);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(this.TOKEN_KEY, res.token);
      localStorage.setItem(this.USER_KEY, JSON.stringify(res));
    }
  }

  private persistStudentSession(res: StudentAuthResponse): void {
    this._token.set(res.token);
    this._user.set(res);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(this.TOKEN_KEY, res.token);
      localStorage.setItem(this.USER_KEY, JSON.stringify({ ...res, role: 'student' }));
    }
  }

  private loadUserFromStorage(): AuthResponse | StudentAuthResponse | null {
    try {
      if (typeof localStorage === 'undefined') return null;
      const raw = localStorage.getItem(this.USER_KEY);
      return raw ? JSON.parse(raw) : null;
    } catch { return null; }
  }
}
