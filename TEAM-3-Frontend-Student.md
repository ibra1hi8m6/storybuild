# Team 3 — Frontend: Student-Facing Features

**Owner:** Frontend developer  
**Stack:** Angular, TypeScript, HTML/CSS, Web Speech API, Canvas API

---

## Your Responsibility

You own the student experience: lesson reader, story journey, writing practice, exams, placement test, and the student dashboard.

---

## Current Bugs to Fix

### BUG-1: Uploaded story journey — listen mode silent when sentence is empty
**File:** `Frontend/story-app/src/app/features/uploaded-stories/uploaded-story-journey.component.ts`  
**Problem:** If OCR failed and `sentence` is an empty string, clicking "استمع" does nothing — no error, no feedback to the student.  
**Fix needed:** In the template, disable the listen/record tabs and show a message when `currentPage().sentence` is empty:
```html
@if (!currentPage().sentence) {
  <div class="empty-sentence-notice">
    <p>النص غير متاح لهذه الصفحة</p>
  </div>
}
```

### BUG-2: Record mode error message is generic
**File:** `Frontend/story-app/src/app/features/fluency/reading-journey/record-mode.component.ts`  
**Problem:** When evaluation fails (network error, 400, 429), the error shown is always "حدث خطأ أثناء التقييم. حاول مرة أخرى." regardless of the real cause.  
**Fix needed:** Parse the HTTP error response and show specific messages:
- 400 → "بيانات غير صحيحة — تأكد من اختيار الصفحة الصحيحة"
- 429 → "الخادم مشغول، انتظر قليلاً وحاول مرة أخرى"
- 500 → "خطأ في الخادم، يرجى المحاولة لاحقاً"

### BUG-3: Writing practice canvas — iOS touch events broken
**File:** `Frontend/story-app/src/app/features/writing-practice/writing-practice.ts`  
**Problem:** On some iOS devices, `pointermove` events don't fire continuously during a drag. The canvas stroke is choppy.  
**Fix needed:** Add `touch-action: none` CSS to the canvas element and ensure `pointerdown` calls `setPointerCapture()`.

### BUG-4: Exam — next button enabled before answer is selected
**File:** `Frontend/story-app/src/app/features/exam/`  
**Problem:** The "التالي" button is sometimes clickable before the student picks an answer, submitting an empty answer.  
**Fix needed:** Disable the button until `selectedAnswer !== null`.

### BUG-5: Lesson reader — page unlock not reflecting immediately
**File:** `Frontend/story-app/src/app/features/` (lesson reader component)  
**Problem:** After completing writing practice, the student must refresh the page to see the next page unlocked.  
**Fix needed:** After a successful writing evaluation response, call `loadLesson()` again to refresh the page list without a full reload.

---

## Tasks to Build

### TASK-1: Story journey progress persistence
When a student closes the browser mid-journey and returns, they should resume from where they left off — not restart from page 1.  
- Store `{ storyId, pageIdx }` in `localStorage`
- On component init, check localStorage before defaulting to `pageIdx = 0`

### TASK-2: Fluency result summary screen
After completing all pages in a journey, show a summary:
- Average accuracy score across all pages
- List of all mispronounced words (deduplicated)
- "أحسنت! أكملت القصة" celebration message

### TASK-3: Student dashboard — show assigned lessons count
**File:** `Frontend/story-app/src/app/features/dashboards/student-dashboard/student-dashboard.component.ts`  
Currently the dashboard shows generic stats. Add a card: "الدروس المعينة لك: X" that links to `/student/assigned-lessons`.

### TASK-4: Placement test — show progress bar
**File:** `Frontend/story-app/src/app/features/placement/question/`  
Add a progress bar showing question N of total (e.g. "السؤال 3 من 10").

---

## Files You Own

```
Frontend/story-app/src/app/features/
├── uploaded-stories/
│   ├── uploaded-story-journey.component.ts   ← Journey (listen/read/record)
│   └── uploaded-story-journey.component.html
├── fluency/
│   ├── reading-journey/
│   │   ├── listen-mode.component.ts          ← Arabic TTS
│   │   ├── record-mode.component.ts          ← Record + evaluate
│   │   └── reading-journey-host.component.*  ← Lesson journey host
│   └── services/
│       ├── fluency-api.service.ts
│       └── web-audio.service.ts
├── writing-practice/
│   ├── writing-practice.ts                   ← Canvas + Pointer API
│   └── writing-practice.html
├── exam/                                     ← MCQ exam
├── placement/                                ← Placement test
├── dashboards/student-dashboard/             ← Student dashboard
├── books/                                    ← Book list
├── levels/                                   ← Level selection
└── student/
    ├── my-lessons/
    ├── assigned-lessons/
    ├── progress/
    └── achievements/
```

---

## How to Test

1. Run frontend: `ng serve`
2. Login as a student account
3. Go through: Placement test → Dashboard → Lessons → Writing → Exam
4. Go to `/uploaded-stories` → open a story → test Listen / Read / Record tabs
5. Check browser console for any errors
