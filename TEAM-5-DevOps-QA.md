# Team 5 — DevOps, QA & Cross-Cutting Concerns

**Owner:** Developer with testing + deployment experience  
**Stack:** Angular, ASP.NET Core, SQL Server, Docker (optional), GitHub Actions (optional)

---

## Your Responsibility

You own code quality, environment configuration, shared services, security, and making sure the app runs cleanly end-to-end. You also write tests and fix bugs that affect multiple parts of the system.

---

## Current Bugs to Fix

### BUG-1: appsettings.Development.json is partially committed
**File:** `Backend/trystorybuild/storybuild.API/appsettings.Development.json`  
**Problem:** This file contains real API keys (Gemini, Cloudinary, Cloudflare, DB password) and should NEVER be in git.  
**Fix needed:**
- Verify `.gitignore` lists `appsettings.Development.json` ← check this first
- If it was ever committed, remove it from git history: `git rm --cached appsettings.Development.json`
- Create `appsettings.Development.example.json` with placeholder values as a template for new team members

### BUG-2: CORS is too permissive
**File:** `Backend/trystorybuild/storybuild.API/Program.cs`  
**Problem:** CORS policy likely allows `*` (all origins). In production this is a security risk.  
**Fix needed:** Restrict to known frontend URLs:
```csharp
options.WithOrigins("http://localhost:4200", "https://yourdomain.com")
       .AllowAnyHeader()
       .AllowAnyMethod();
```

### BUG-3: No request size limit on file uploads
**File:** `Program.cs` / `appsettings.json`  
**Problem:** No `MaxRequestBodySize` is configured. Large PDF/audio uploads could exhaust server memory.  
**Fix needed:** Add to `Program.cs`:
```csharp
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 25_000_000); // 25MB
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 25_000_000);
```

### BUG-4: Frontend has no global HTTP error handler
**File:** `Frontend/story-app/src/app/core/error-interceptor.ts`  
**Problem:** 401 errors (token expired) send the user nowhere — they see a blank screen or cryptic error.  
**Fix needed:** In the error interceptor, redirect to `/auth/login` on 401:
```typescript
if (error.status === 401) {
  this.router.navigate(['/auth/login']);
}
```

### BUG-5: Angular build uses development configuration in `ng serve` but SSR prerendering fails for dynamic routes
**File:** `Frontend/story-app/src/app/app.routes.server.ts`  
**Problem:** Some dynamic routes are missing from `serverRoutes`, causing build failures or blank pages on SSR.  
**Fix needed:** Audit all routes in `app.routes.ts` and ensure every route with a URL parameter has a matching entry in `app.routes.server.ts` with `renderMode: RenderMode.Client` or `RenderMode.Server`.

---

## Tasks to Build

### TASK-1: Environment configuration documentation
Create a `docs/SETUP.md` file that explains every required environment variable:

```markdown
## Backend — appsettings.Development.json
- ConnectionStrings:DefaultConnection — SQL Server connection string
- Gemini:ApiKey — from Google AI Studio
- Gemini:Model — gemini-2.5-flash
- Cloudinary:CloudName / ApiKey / ApiSecret — from cloudinary.com
- Rag:ChromaEndpoint / ChromaApiKey — from trychroma.com
```

### TASK-2: Write integration tests for critical API endpoints
Use `WebApplicationFactory` to write tests for:
- `POST /api/auth/login` — valid and invalid credentials
- `POST /api/fluency/evaluate` — missing audio returns 400
- `GET /api/parent-portal/child/{id}/recordings` — unauthorized user returns 403

### TASK-3: Add health check endpoint
```csharp
app.MapHealthChecks("/health");
```
This lets load balancers and monitoring tools check if the API is alive.

### TASK-4: Angular lazy loading audit
Check `app.routes.ts` — verify all feature modules use `loadComponent: () => import(...)` for lazy loading. Any eagerly loaded feature component slows down the initial page load.

### TASK-5: Add loading skeleton screens
Currently most pages show a spinner during loading. Replace spinners with skeleton screens (grey animated boxes) for a better UX. Focus on:
- Student dashboard
- Story list
- Lesson list

### TASK-6: Mobile responsiveness audit
Test every page at 375px width (iPhone SE). Known problem areas:
- Teacher dashboard tables
- Admin RAG chunks page
- Story journey mode tabs

---

## Shared Things to Review

### Security checklist
- [ ] JWT secret is strong (min 32 chars) and not hardcoded in `appsettings.json`
- [ ] All `[Authorize]` controllers are actually protected
- [ ] Parent can only see their own children's data
- [ ] File uploads validate extension and MIME type
- [ ] SQL injections are impossible (EF Core parameterizes queries — verify no raw SQL)
- [ ] No sensitive data in Angular `localStorage` (only JWT token)

### Performance checklist
- [ ] EF Core queries use `.AsNoTracking()` on read-only queries
- [ ] Story/lesson list endpoints are paginated
- [ ] Images served from Cloudinary (CDN), not from the API server directly
- [ ] Angular production build runs: `ng build --configuration production`

---

## How to Test the Full Flow

```
1. Start backend:   cd Backend/trystorybuild && dotnet run --project storybuild.API
2. Start frontend:  cd Frontend/story-app && ng serve
3. Open:            http://localhost:4200

Full happy path:
  Register student → Placement test → Student dashboard
  → Open lesson → Writing practice → Pass → Unlock next page
  → Take exam → See score
  → Go to uploaded stories → Open story journey
  → Listen mode → Read mode → Record mode → Submit → See fluency score
  → Login as parent → View child recordings
```
