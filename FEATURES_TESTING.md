# دليل اختبار الميزات الجديدة — Lughati

> **المشكلة الحالية:** الميزات الجديدة (Module A/B/C/D) لا توجد لها روابط في التنقل.
> يجب إدخال الـ URL مباشرة في المتصفح حتى يتم إضافتها للـ sidebar والـ navbar.

---

## بيانات الدخول للاختبار

| الدور | الإيميل | كلمة المرور |
|---|---|---|
| Admin | `admin@lughati.com` | `Admin@Lughati2026` |
| معلم / طالب / ولي أمر | سجّل من `/auth/register` | — |

**الموقع:** `http://localhost:4200` (أو الـ port المشغّل لديك)

---

## ما الفرق بين المتاح والمكسور

### ✅ يعمل بالكامل (موجود في التنقل)
| الميزة | المسار | من يدخل |
|---|---|---|
| لوحة الطالب | `/dashboard` | طالب |
| المستويات والكتب | `/levels` → `/levels/:id/books` | طالب |
| قراءة القصة | `/books/:id/read` | طالب |
| الاختبار | `/exam` | طالب |
| نتيجة الاختبار | `/books/:id/quiz-result` | طالب |
| لوحة المعلم | `/teacher/students` | معلم |
| لوحة المدرسة | `/school/dashboard` | مدير مدرسة |
| لوحة ولي الأمر | `/parent/dashboard` | ولي أمر |
| لوحة الادمن | `/admin/content` | أدمن |

### ❌ جديد — لا يوجد رابط في أي صفحة (يجب إدخال URL يدوياً)
| الميزة | المسار | المشكلة |
|---|---|---|
| Module A: رحلة القراءة والتسجيل | `/books/:id/journey` | لا رابط من `/books/:id/read` |
| Module A: رحلة القراءة من درس | `/lessons/:id/journey` | لا رابط من `/lessons/:id` |
| Module B: مفرداتي | `/my-journal` | لا رابط في sidebar الطالب |
| Module C: ألعاب اللغة | `/mini-games` | لا رابط في أي مكان |
| Module D: صندوق الرسائل | `/inbox` | لا رابط في sidebar الطالب |
| Module D: إرسال تشجيع | `/teacher/feedback` | لا رابط في sidebar المعلم |
| Module D: تسجيلات الطفل | `/parent/child/:id/recordings` | لا رابط في لوحة ولي الأمر |

---

## Module A — رحلة القراءة والتسجيل الصوتي

### الوصف
الطالب يقرأ صفحات القصة بثلاث مراحل: **استمع → اقرأ → سجّل**
Gemini 2.5 Flash يحلّل الصوت ويعطي درجة دقة + كلمة/دقيقة.

### الـ Flow الصحيح
```
1. طالب يسجّل دخول
2. يذهب لـ /levels → يختار مستوى → يرى الكتب
3. يضغط على كتاب → /books/:id/read
4. [لا يوجد زر بعد] → يدخل يدوياً: /books/:id/journey
5. يضغط تبويب "استمع" → يسمع الجملة
6. يضغط تبويب "اقرأ" → يقرأ الجملة
7. يضغط تبويب "سجّل" → يسجّل صوته
8. يضغط إيقاف → Gemini يحلّل (3 محاولات)
9. يرى النتيجة: درجة دائرية + مقارنة كلمة بكلمة (أخضر/أحمر)
```

### خطوات الاختبار اليدوي

**1. احصل على ID كتاب:**
- سجّل دخول كطالب
- اذهب لـ `/levels` → اضغط مستوى → انسخ الـ `id` من URL

**2. افتح رحلة القراءة:**
```
http://localhost:4200/books/BOOK_ID_HERE/journey
```

**3. اختبر كل تبويب:**
- "استمع" → يجب أن تسمع الجملة تُقرأ بصوت (TTS)
- "اقرأ" → يجب أن تظهر الجملة واضحة
- "سجّل" → اضغط الميكروفون → اقرأ → اضغط إيقاف

**4. تحقق من النتيجة:**
- ✅ تظهر دائرة درجة (0-100%)
- ✅ تظهر شرائح كلمات (خضراء = صح، حمراء = غلط)
- ✅ يظهر "سمع الذكاء الاصطناعي: ..."
- ✅ إذا نجح > 70% يظهر زر "التالي" → تنتقل للصفحة التالية
- ✅ بعد آخر صفحة ينتقل لـ `/mini-games`

**5. اختبر الدرس (Lesson) أيضاً:**
```
http://localhost:4200/lessons/LESSON_ID_HERE/journey
```

### أخطاء متوقعة أثناء الاختبار

| الخطأ | السبب المحتمل |
|---|---|
| الصوت لا يعمل | المتصفح يحتاج إذن الميكروفون |
| "حدث خطأ أثناء التقييم" | Gemini API key غير مضبوط في `appsettings.Development.json` |
| الصفحات لا تظهر | الـ BOOK_ID غير موجود أو الكتاب ليس له صفحات |
| درجة = 0% | الجملة فارغة (الكتاب مرفوع بدون OCR) |

---

## Module B — أدوات القراءة والمفردات

### الوصف
- تظليل النص بألوان مختلفة
- نافذة منبثقة عند الضغط على كلمة (تعريف + TTS + إضافة لمفرداتي)
- صفحة "مفرداتي" تجمع كل الكلمات المحفوظة

### B-1: صفحة مفرداتي

**Flow:**
```
طالب → /my-journal → يرى الكلمات المحفوظة + يسمعها + يحذف
```

**خطوات الاختبار:**
```
http://localhost:4200/my-journal
```

**تحقق من:**
- ✅ تظهر قائمة الكلمات المحفوظة (أو "لا توجد مفردات بعد")
- ✅ زر الصوت يشغّل TTS للكلمة
- ✅ زر الحذف يزيل الكلمة

> **ملاحظة:** `AnnotationService` و `VocabularyService` موجودان لكن **لا يوجد مكان في الـ UI يربطهم بصفحة القراءة بعد.** يحتاج دمجهم في `StoryReaderComponent` أو `LessonReaderComponent`.

### B-2: أدوات التظليل (LessonReaderToolbar)

**الحالة الحالية:** الـ Toolbar موجود كـ component لكن **غير مدمج في أي صفحة قراءة بعد.**

**المكونات الجاهزة:**
- `LessonReaderToolbarComponent` → شريط أدوات التظليل
- `TextHighlighterDirective` → directive للتظليل عند تحديد النص
- `VocabularyPopupComponent` → نافذة الكلمة

**ما يحتاج عمله:**
- إضافة `app-lesson-reader-toolbar` داخل `LessonReaderComponent`
- ربط `TextHighlighterDirective` بفقرة النص
- ربط `VocabularyPopupComponent` عند النقر على كلمة

---

## Module C — ألعاب اللغة

### الوصف
ثلاثة ألعاب تعليمية بـ Drag & Drop:
1. **المطابقة** — اسحب الكلمة إلى الصورة المناسبة
2. **الترتيب** — رتّب كلمات الجملة في القطار
3. **الحرف الناقص** — اسحب الحرف الصحيح لإكمال الكلمة

### الـ Flow الصحيح
```
طالب ينهي رحلة القراءة → ينتقل تلقائياً لـ /mini-games
أو
طالب يدخل مباشرة: /mini-games
```

### خطوات الاختبار

```
http://localhost:4200/mini-games
```

**تحقق من كل لعبة:**

**لعبة المطابقة:**
- ✅ الكلمات على اليمين، الصور على اليسار
- ✅ اسحب الكلمة → أفلتها على الصورة الصحيحة → تتحول للأخضر
- ✅ اسحب على صورة خاطئة → اهتزاز أحمر + صوت خطأ
- ✅ عند اكتمال الكل → "أحسنت!"

**لعبة الترتيب:**
- ✅ كلمات مبعثرة في القطار
- ✅ اسحب لإعادة الترتيب
- ✅ اضغط "تحقق" → صح = أخضر، خطأ = أحمر + اهتزاز

**لعبة الحرف الناقص:**
- ✅ كلمة فيها خانة فارغة (_)
- ✅ حروف دائرية أسفل → اسحب الحرف الصحيح
- ✅ صح = الخانة تمتلئ بالألوان، خطأ = اهتزاز

**عند إنهاء كل الألعاب:**
- ✅ confetti ملوّن يظهر على الشاشة
- ✅ تظهر شاشة "أحسنت! أنهيت كل الألعاب!"

> **ملاحظة:** الألعاب حالياً تستخدم بيانات demo ثابتة (أرنوب، كلب، بيت). يحتاج ربط بيانات ديناميكية من الـ API.

---

## Module D — التواصل والرسائل

### D-1: صندوق الرسائل (للطالب)

**Flow:**
```
معلم يرسل رسالة → طالب يفتح /inbox → يرى الرسالة
```

**خطوات الاختبار:**

**خطوة 1 — المعلم يرسل رسالة (عبر API مباشرة):**
```http
POST http://localhost:5000/api/messages/send
Content-Type: application/json

{
  "senderId": "TEACHER_USER_ID",
  "receiverId": "STUDENT_USER_ID",
  "content": "أحسنت يا بطل! قراءتك اليوم كانت رائعة ⭐",
  "type": "Text"
}
```

**خطوة 2 — الطالب يفتح صندوقه:**
```
http://localhost:4200/inbox
```

**تحقق من:**
- ✅ تظهر الرسالة مع نقطة زرقاء (غير مقروءة)
- ✅ عند الضغط على الرسالة → تختفي النقطة (تم تمييزها كمقروءة)
- ✅ رسائل الصوت تظهر مشغّل audio
- ✅ الملصقات (stickers) تظهر كـ emoji كبيرة

### D-2: إرسال تشجيع (للمعلم)

**Flow:**
```
معلم → /teacher/feedback?studentId=ID&name=اسم الطالب → يختار نوع الرسالة → يرسل
```

**خطوات الاختبار:**
```
http://localhost:4200/teacher/feedback?studentId=STUDENT_ID&name=محمد
```

**تحقق من كل تبويب:**

| التبويب | ما يجب اختباره |
|---|---|
| رسالة نصية | اكتب نص → اضغط إرسال → ✅ "تم الإرسال بنجاح" |
| ملصق | اضغط emoji → اضغط إرسال ملصق → ✅ "تم الإرسال" |
| رسالة صوتية | اضغط ميكروفون → تكلم → اضغط إيقاف → ✅ يُرفع للـ Cloudinary ويُرسل |

> **ملاحظة:** لا يوجد رابط لهذه الصفحة من sidebar المعلم. يحتاج إضافة رابط في `teacher-sidebar.component.ts`.

### D-3: تسجيلات الطفل (لولي الأمر)

**Flow:**
```
ولي أمر → /parent/child/:studentId/recordings → يرى كل تسجيلات الطفل + WCPM + درجات
```

**خطوات الاختبار:**
```
http://localhost:4200/parent/child/STUDENT_ID/recordings
```

**تحقق من:**
- ✅ تظهر بطاقة لكل تسجيل
- ✅ يوجد مشغّل صوت لكل تسجيل
- ✅ يظهر WCPM (كلمة/دقيقة) ودرجة الدقة
- ✅ شرائح حمراء للكلمات الخاطئة
- ✅ إذا لا توجد تسجيلات → رسالة "لا توجد تسجيلات بعد"

> **ملاحظة:** لا يوجد رابط لهذه الصفحة من لوحة ولي الأمر.

---

## الـ IDs التي تحتاجها للاختبار

### كيف تحصل على الـ IDs

**User ID (للطالب/المعلم):**
- افتح `/auth/login` → سجّل دخول
- افتح أدوات المتصفح (F12) → Application → Local Storage
- ابحث عن `user` أو `currentUser` → انسخ الـ `id`

**Book ID:**
- سجّل كأدمن → `/admin/books`
- أو: افتح `/levels` → اضغط مستوى → اضغط كتاب → انسخ الـ id من URL

**Lesson ID:**
- `/lessons-list` → اضغط درس → انسخ الـ id من URL
- أو: `/my-lessons` أو `/assigned-lessons`

**Student ID (للمعلم وولي الأمر):**
- معلم: `/teacher/students` → اضغط على طالب → URL يحتوي الاسم
- ولي أمر: `/parent/children` → اضغط على طفل

---

## مشاكل مؤكدة تحتاج إصلاح

### 1. لا توجد روابط للميزات الجديدة في التنقل

**المطلوب إضافته:**

**Student Dashboard sidebar** (`student-dashboard.component.ts`):
```typescript
{ icon:'🎤', label:'رحلة القراءة',  route:'/books/DYNAMIC/journey' },  // يحتاج منطق
{ icon:'📖', label:'مفرداتي',        route:'/my-journal' },
{ icon:'🎮', label:'ألعاب اللغة',   route:'/mini-games' },
{ icon:'💌', label:'رسائلي',         route:'/inbox' },
```

**Teacher Sidebar** (`teacher-sidebar.component.ts`):
```typescript
{ icon:'💬', label:'إرسال تشجيع', route:'/teacher/feedback' },
```

**Parent Dashboard:**
يحتاج زر "تسجيلات الطفل" في صفحة `/parent/children` يؤدي لـ `/parent/child/:id/recordings`

### 2. لا يوجد زر "ابدأ الرحلة" في صفحة قراءة الكتاب

في `story-reader` يجب إضافة:
```html
<button routerLink="/books/{{ bookId }}/journey">
  🎤 ابدأ رحلة القراءة
</button>
```

### 3. لا يوجد ربط لـ Lesson eTools

`LessonReaderComponent` لا يستخدم:
- `LessonReaderToolbarComponent`
- `TextHighlighterDirective`
- `VocabularyPopupComponent`

هذه المكونات موجودة كـ files لكن غير مدمجة في أي صفحة.

### 4. بيانات المطابقة في Mini-Games ثابتة (Demo)

`MiniGamesHostComponent` يستخدم `DEMO_GAMES` ثابتة بدلاً من جلب بيانات من API.

### 5. لا يوجد API endpoint لجلب أسئلة الألعاب

`/api/mini-games/questions` غير موجود في الـ Backend.

---

## ترتيب الاختبار المقترح

```
1. Module A (يعمل كاملاً تقريباً)
   /books/:id/journey → اختبر الاستماع والتسجيل والنتيجة

2. Module C (يعمل كاملاً بالبيانات الثابتة)
   /mini-games → اختبر كل لعبة

3. Module D-2 (المعلم يرسل)
   /teacher/feedback?studentId=ID&name=اسم

4. Module D-1 (الطالب يقرأ)
   /inbox

5. Module D-3 (ولي الأمر)
   /parent/child/:id/recordings

6. Module B (جزئي فقط)
   /my-journal
```

---

## Backend API Endpoints للاختبار المباشر

افتح Swagger على: `http://localhost:5000/swagger`

| الـ Endpoint | الطريقة | الاستخدام |
|---|---|---|
| `/api/fluency/evaluate` | POST | رفع صوت + تقييم |
| `/api/fluency/student/{id}` | GET | سجلّ طالب |
| `/api/fluency/page/{pageId}/student/{studentId}` | GET | سجلّ صفحة |
| `/api/annotations` | POST | حفظ تظليل |
| `/api/annotations/{studentId}/{pageId}` | GET | تظليلات صفحة |
| `/api/vocabulary/lesson/{lessonId}` | GET | مفردات درس |
| `/api/vocabulary/journal/{studentId}` | GET | مفردات الطالب |
| `/api/vocabulary/journal` | POST | إضافة لمفرداتي |
| `/api/messages/send` | POST | إرسال رسالة نصية/ملصق |
| `/api/messages/send-voice` | POST | إرسال رسالة صوتية |
| `/api/messages/inbox/{userId}` | GET | صندوق الوارد |
| `/api/parent-portal/child/{studentId}/recordings` | GET | تسجيلات الطفل |
