import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface MySubscription {
  userId: string;
  role: string;
  isDemo: boolean;
  activePlan: string;
  expiresAt: string | null;
  isActive: boolean;
  isSchoolTeacher?: boolean;
  inheritedFromSchool?: boolean;
  maxStudentsPerClass?: number;
  childrenCount?: number;
  maxChildren?: number;
  studentsCount?: number;
  maxStudents?: number;
  groupsCount?: number;
  maxGroups?: number;
  classesCount?: number;
  maxClasses?: number;
  teachersCount?: number;
  maxTeachers?: number;
}

export interface ActivationCodeDto {
  id: string;
  code: string;
  plan: string;
  durationDays: number;
  maxUses: number;
  usedCount: number;
  isActive: boolean;
  expiresAt: string | null;
  notes: string | null;
  createdAt: string;
}

export const PLAN_LABELS: Record<string, string> = {
  Free:            'مجاني',
  ParentPremium:   'مميز (أولياء الأمور)',
  ParentFamily:    'عائلي',
  TeacherFree:     'معلم (مجاني)',
  TeacherPremium:  'معلم مميز',
  SchoolTrial:     'مدرسة (تجريبي)',
  SchoolPremium:   'مدرسة مميزة',
  DemoFullAccess:  'عرض توضيحي',
};

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private readonly http = inject(HttpClient);
  private readonly api  = environment.apiUrl;

  getMySubscription(): Observable<MySubscription> {
    return this.http.get<MySubscription>(`${this.api}/api/subscriptions/me`);
  }

  activate(code: string): Observable<{ message: string; plan: string; expiresAt: string }> {
    return this.http.post<any>(`${this.api}/api/subscriptions/activate`, { code });
  }

  getAdminCodes(): Observable<ActivationCodeDto[]> {
    return this.http.get<ActivationCodeDto[]>(`${this.api}/api/subscriptions/codes`);
  }

  createCode(req: {
    plan: string;
    durationDays: number;
    maxUses: number;
    expiresAt?: string | null;
    notes?: string | null;
    code?: string | null;
  }): Observable<ActivationCodeDto> {
    return this.http.post<ActivationCodeDto>(`${this.api}/api/subscriptions/codes`, req);
  }

  deactivateCode(id: string): Observable<any> {
    return this.http.patch<any>(`${this.api}/api/subscriptions/codes/${id}/deactivate`, {});
  }
}
