import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { SubscriptionAlertService } from './subscription-alert.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const subscriptionAlert = inject(SubscriptionAlertService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // ── 402 Payment Required — subscription gate ──────────────────────────
      if (error.status === 402 && error.error?.requiresUpgrade) {
        const message = error.error?.message || 'هذه الميزة تحتاج إلى اشتراك للمتابعة.';
        const feature = error.error?.feature  ?? 'subscription';
        subscriptionAlert.show(message, feature);
        return throwError(() => new Error(message));
      }

      // ── All other errors ──────────────────────────────────────────────────
      let message = 'حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.';

      if (error.error?.error) {
        message = error.error.error;
      } else if (error.error?.message) {
        message = error.error.message;
      } else if (error.status === 0) {
        message = 'تعذّر الاتصال بالخادم. تأكد من تشغيل الخادم.';
      } else if (error.status === 422) {
        message = 'فشل إنشاء القصة. يرجى المحاولة مرة أخرى.';
      }

      return throwError(() => new Error(message));
    })
  );
};