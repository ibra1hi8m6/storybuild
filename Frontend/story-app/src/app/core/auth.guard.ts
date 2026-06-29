import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { AppStateService } from '../services/app-state-service';

export const authGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn()) return true;
  return router.createUrlTree(['/auth/login']);
};

export const studentGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && auth.isStudent()) return true;
  return router.createUrlTree(['/auth/login']);
};

export const parentGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && auth.isParent()) return true;
  return router.createUrlTree(['/auth/login']);
};

export const teacherGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && (auth.isTeacher() || auth.isAdmin())) return true;
  return router.createUrlTree(['/auth/login']);
};

export const schoolGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && (auth.isSchoolAdmin() || auth.isTeacher() || auth.isAdmin())) return true;
  return router.createUrlTree(['/auth/login']);
};

export const adminGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && auth.isAdmin()) return true;
  return router.createUrlTree(['/auth/login']);
};

// Level guards — only block students below the required level.
// Non-students (teachers, admins, parents) always pass through.

export const level2Guard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const state  = inject(AppStateService);
  const router = inject(Router);
  if (!auth.isLoggedIn()) return router.createUrlTree(['/auth/login']);
  if (!auth.isStudent()) return true;
  const level = state.currentUser()?.level ?? 1;
  if (level >= 2) return true;
  return router.createUrlTree(['/dashboard']);
};

export const level3Guard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const state  = inject(AppStateService);
  const router = inject(Router);
  if (!auth.isLoggedIn()) return router.createUrlTree(['/auth/login']);
  if (!auth.isStudent()) return true;
  const level = state.currentUser()?.level ?? 1;
  if (level >= 3) return true;
  return router.createUrlTree(['/dashboard']);
};
