import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  { path: 'lessons/:id',                  renderMode: RenderMode.Server },
  { path: 'lessons/:id/complete',         renderMode: RenderMode.Client },
  { path: 'lessons/:id/journey',          renderMode: RenderMode.Client },
  { path: 'levels/:id/books',             renderMode: RenderMode.Server },
  { path: 'books/:id/read',               renderMode: RenderMode.Server },
  { path: 'books/:id/journey',            renderMode: RenderMode.Client },
  { path: 'books/:id/quiz-result',        renderMode: RenderMode.Client },
  { path: 'teacher/students/:name',       renderMode: RenderMode.Server },
  { path: 'parent/child/:name/progress',  renderMode: RenderMode.Server },
  { path: 'parent/child/:id/recordings',  renderMode: RenderMode.Client },
  { path: 'uploaded-stories/:id/journey', renderMode: RenderMode.Client },
  { path: 'learning/letters/:id',         renderMode: RenderMode.Client },
  { path: 'learning/words/:id',           renderMode: RenderMode.Client },
  { path: 'learning/sentences/:id',       renderMode: RenderMode.Client },
  { path: 'writing-practice',             renderMode: RenderMode.Client },
  { path: 'test/question',                renderMode: RenderMode.Client },
  { path: 'test/result',                  renderMode: RenderMode.Client },
  { path: 'auth/create-student',          renderMode: RenderMode.Client },
  { path: '**',                           renderMode: RenderMode.Prerender }
];
