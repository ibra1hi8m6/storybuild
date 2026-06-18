# Team 4 — Frontend: Teacher, Parent & Admin UI

**Owner:** Frontend developer  
**Stack:** Angular, TypeScript, HTML/CSS

---

## Your Responsibility

You own the teacher dashboard, parent portal, school admin panel, and the system admin panel.

---

## Current Bugs to Fix

### BUG-1: Parent recordings page — child name not shown
**File:** `Frontend/story-app/src/app/features/messaging/parent-recordings/parent-recordings.component.ts`  
**Problem:** The page title says "تسجيلات طفلك" with no child name. The `studentId` is in the URL but the child's name is never fetched.  
**Fix needed:** Call `GET /api/auth/student/{studentId}` (or a profile endpoint) on init and display the child's name in the header.

### BUG-2: Teacher dashboard — student progress shows 0 for all
**File:** `Frontend/story-app/src/app/features/dashboards/teacher-dashboard/teacher-dashboard.component.ts`  
**Problem:** The teacher dashboard fetches progress stats but the data always shows 0 completed lessons.  
**Fix needed:** Check the API call — verify the endpoint URL includes `/api/` prefix and the response shape matches the component's interface. Log the raw response in the browser console.

### BUG-3: Admin PDF library — upload success but list not refreshed
**File:** `Frontend/story-app/src/app/features/admin-pdf-library/admin-pdf-library.component.ts`  
**Problem:** After a successful PDF upload, the uploaded file does not appear in the list until the page is manually refreshed.  
**Fix needed:** After the upload `subscribe` success callback, call `loadPdfs()` again to reload the list.

### BUG-4: School dashboard — classroom list empty on first load
**File:** `Frontend/story-app/src/app/features/dashboards/school-dashboard/`  
**Problem:** Classrooms are not shown on first load; a manual refresh is required.  
**Fix needed:** Verify `ngOnInit` is calling the correct API endpoint and the JWT is being sent (check with Network tab in DevTools).

### BUG-5: Admin RAG chunks page — pagination missing
**File:** `Frontend/story-app/src/app/features/admin-rag-chunks/admin-rag-chunks.ts`  
**Problem:** All RAG chunks load at once, which can be hundreds of items and freezes the browser.  
**Fix needed:** Add simple client-side pagination: show 20 items per page with Previous/Next buttons.

---

## Tasks to Build

### TASK-1: Teacher — student fluency history view
Teachers should be able to click on a student and see their fluency recording history (accuracy scores, mispronounced words, audio playback).

- Route: `/teacher/students/:studentId/fluency`
- API: `GET /api/fluency/student/{studentId}` (already exists)
- Show a table: date | page | accuracy % | audio player | mispronounced words

### TASK-2: Parent — child progress summary card
**File:** `Frontend/story-app/src/app/features/parent/child-progress/`  
Add a summary card that shows:
- Total lessons completed
- Average fluency accuracy (from recordings)
- Last active date

### TASK-3: Admin — story page sentence editor
After an AI story or uploaded story is created, the admin should be able to edit the sentence for each page directly from the UI (in case OCR extracted wrong text).

- List pages in a table with an inline edit field
- Call `PUT /api/admin/story-pages/{pageId}/sentence` on save (this endpoint is in Team 2's task list to build)

### TASK-4: Teacher — lesson assignment to group
**File:** `Frontend/story-app/src/app/features/teacher/groups/teacher-groups.ts`  
Allow the teacher to select a group and assign a lesson to all students in that group at once. The backend endpoint already exists at `POST /api/groups/{groupId}/assign-lesson`.

### TASK-5: Admin — uploaded stories management table
**File:** `Frontend/story-app/src/app/features/admin/` (needs new component)  
Show all uploaded story PDFs in a table:
- Title, page count, upload date, cover image
- Delete button
- Status badge showing if all pages have sentences (OCR status)

---

## Files You Own

```
Frontend/story-app/src/app/features/
├── dashboards/
│   ├── teacher-dashboard/
│   ├── parent-dashboard/
│   └── school-dashboard/
├── teacher/
│   ├── ai-generator/
│   ├── lesson-create/
│   ├── reports/
│   ├── student-detail/
│   └── groups/
├── parent/
│   ├── child-progress/
│   ├── children/
│   └── notifications/
├── messaging/
│   └── parent-recordings/     ← Parent recordings view
├── admin/
│   ├── content/
│   ├── ai-settings/
│   ├── subscriptions/
│   └── users/
├── admin-pdf-library/          ← PDF workbook management
├── admin-rag/                  ← RAG document management
├── admin-rag-chunks/           ← RAG chunks viewer
├── admin-import/               ← Lesson import
└── admin/shared/
    └── admin-sidebar.component.ts
```

---

## How to Test

1. Login as teacher → verify dashboard shows students and progress
2. Login as parent → go to `/parent/child/{id}/recordings` → recordings should appear
3. Login as school admin → verify classrooms show correctly
4. Login as system admin → test PDF upload and RAG chunk viewer
