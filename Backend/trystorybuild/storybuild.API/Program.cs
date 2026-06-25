using storybuild.API.Middleware;
using Application.Agents;
using Infrastructure;
using System.Text.Json;
using System.Text.Json.Serialization;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new()
    {
        Title       = "Arabic Story Platform API",
        Version     = "v1",
        Description = "Agents: Story • Exam • Writing • PDF Import • Judge • Image • Auth"
    }));

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is missing in configuration.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
    });

builder.Services.AddAuthorization();

// ── Infrastructure (DB + AI + Repos) ─────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);

// ── Agents ────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<StoryAgent>();
builder.Services.AddScoped<ExamAgent>();
builder.Services.AddScoped<WritingCorrectionAgent>();
builder.Services.AddScoped<Application.Agents.LessonGenerationAgent>();

// ── File upload limit: 20 MB ──────────────────────────────────────────────────
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 20 * 1024 * 1024;
});

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(o =>
    o.AddPolicy("Angular", p =>
        p.WithOrigins("http://localhost:4200", "https://lughati.runasp.net")
         .AllowAnyHeader()
         .AllowAnyMethod()));

var app = builder.Build();

// ── Auto-migrate + seed admin on startup ─────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Migrations applied.");
        await DbSeeder.SeedAsync(db);
    }
    catch (Exception ex) { logger.LogError(ex, "Migration/seed failed."); }

    // ── Safe column additions (idempotent) ────────────────────────────────────
    try
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Stories') AND name = 'Source')
                ALTER TABLE Stories ADD Source int NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Stories') AND name = 'CoverImagePath')
                ALTER TABLE Stories ADD CoverImagePath nvarchar(max) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Students') AND name = 'WeaknessMapJson')
                ALTER TABLE Students ADD WeaknessMapJson nvarchar(max) NOT NULL DEFAULT '{{}}' ;
            """);
        logger.LogInformation("Story columns ensured.");
    }
    catch (Exception ex) { logger.LogError(ex, "Story column migration failed."); }

    // ── Phase 7: StudentLevelHistories ───────────────────────────────────────────
    try
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'StudentLevelHistories')
                CREATE TABLE StudentLevelHistories (
                    Id uniqueidentifier NOT NULL DEFAULT newsequentialid() PRIMARY KEY,
                    StudentId uniqueidentifier NOT NULL,
                    ChildName nvarchar(200) NOT NULL DEFAULT '',
                    PreviousLevel int NOT NULL DEFAULT 0,
                    NewLevel int NOT NULL DEFAULT 0,
                    ChangedByUserId uniqueidentifier NOT NULL,
                    ChangedByRole nvarchar(50) NOT NULL DEFAULT '',
                    Reason nvarchar(500) NOT NULL DEFAULT '',
                    ChangedAt datetime2 NOT NULL DEFAULT GETUTCDATE()
                );
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Students') AND name = 'AvatarEmoji')
                ALTER TABLE Students ADD AvatarEmoji nvarchar(10) NULL;
            """);
        logger.LogInformation("Phase 7 tables and columns ensured.");
    }
    catch (Exception ex) { logger.LogError(ex, "Phase 7 migration failed."); }

    // ── Phase 8: StudentId on StudentProgress + LessonPageCompletions ────────────
    try
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('StudentProgress') AND name = 'StudentId')
                ALTER TABLE StudentProgress ADD StudentId uniqueidentifier NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('LessonPageCompletions') AND name = 'StudentId')
                ALTER TABLE LessonPageCompletions ADD StudentId uniqueidentifier NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('StudentProgress') AND name = 'IX_StudentProgress_StudentId')
                CREATE INDEX IX_StudentProgress_StudentId ON StudentProgress (StudentId);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('LessonPageCompletions') AND name = 'IX_LessonPageCompletions_StudentId')
                CREATE INDEX IX_LessonPageCompletions_StudentId ON LessonPageCompletions (StudentId);
            """);

        await db.Database.ExecuteSqlRawAsync("""
            UPDATE sp SET sp.StudentId = s.Id
            FROM StudentProgress sp
            INNER JOIN Students s ON s.Name = sp.ChildName
            WHERE sp.StudentId IS NULL;

            UPDATE lpc SET lpc.StudentId = s.Id
            FROM LessonPageCompletions lpc
            INNER JOIN Students s ON s.Name = lpc.ChildName
            WHERE lpc.StudentId IS NULL;

            UPDATE wa SET wa.StudentId = s.Id
            FROM WritingAttempts wa
            INNER JOIN Students s ON s.Name = wa.ChildName
            WHERE wa.StudentId IS NULL;
            """);

        logger.LogInformation("Phase 8: StudentId columns ensured and backfilled.");
    }
    catch (Exception ex) { logger.LogError(ex, "Phase 8 StudentId migration failed."); }

    // ── Phase 6: AssignmentSubmissions + WeakLetterRecords ───────────────────────
    try
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AssignmentSubmissions')
                CREATE TABLE AssignmentSubmissions (
                    Id uniqueidentifier NOT NULL DEFAULT newsequentialid() PRIMARY KEY,
                    AssignmentId uniqueidentifier NOT NULL,
                    StudentId uniqueidentifier NOT NULL,
                    ChildName nvarchar(200) NOT NULL DEFAULT '',
                    PagesCompleted int NOT NULL DEFAULT 0,
                    TotalPages int NOT NULL DEFAULT 0,
                    WritingScore float NOT NULL DEFAULT 0,
                    IsComplete bit NOT NULL DEFAULT 0,
                    SubmittedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    NotesJson nvarchar(max) NOT NULL DEFAULT '{{}}'
                );
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WeakLetterRecords')
                CREATE TABLE WeakLetterRecords (
                    Id uniqueidentifier NOT NULL DEFAULT newsequentialid() PRIMARY KEY,
                    StudentId uniqueidentifier NOT NULL,
                    ChildName nvarchar(200) NOT NULL DEFAULT '',
                    Letter nvarchar(10) NOT NULL DEFAULT '',
                    Attempts int NOT NULL DEFAULT 0,
                    Correct int NOT NULL DEFAULT 0,
                    ActivityType nvarchar(50) NOT NULL DEFAULT 'Writing',
                    LastSeenAt datetime2 NOT NULL DEFAULT GETUTCDATE()
                );
            """);
        logger.LogInformation("Phase 6 tables ensured.");
    }
    catch (Exception ex) { logger.LogError(ex, "Phase 6 table migration failed."); }

    // ── Phase 5b: RAG tracking columns on LessonPages ────────────────────────────
    try
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('LessonPages') AND name = 'IsEmbedded')
                ALTER TABLE LessonPages ADD IsEmbedded bit NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('LessonPages') AND name = 'ChromaId')
                ALTER TABLE LessonPages ADD ChromaId nvarchar(100) NULL;
            """);
        logger.LogInformation("Phase 5b LessonPage RAG columns ensured.");
    }
    catch (Exception ex) { logger.LogError(ex, "Phase 5b column migration failed."); }

    // ── Phase 5: content lifecycle columns on Lessons & Stories ──────────────────
    try
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Lessons') AND name = 'IsPublished')
                ALTER TABLE Lessons ADD IsPublished bit NOT NULL DEFAULT 1;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Lessons') AND name = 'Status')
                ALTER TABLE Lessons ADD Status int NOT NULL DEFAULT 3;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Stories') AND name = 'IsPublished')
                ALTER TABLE Stories ADD IsPublished bit NOT NULL DEFAULT 1;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Stories') AND name = 'Status')
                ALTER TABLE Stories ADD Status int NOT NULL DEFAULT 3;
            """);
        logger.LogInformation("Phase 5 content lifecycle columns ensured.");
    }
    catch (Exception ex) { logger.LogError(ex, "Phase 5 column migration failed."); }

    // ── Phase 3: structured feedback columns on WritingAttempts & FluencyReports ──
    try
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WritingAttempts') AND name = 'LessonId')
                ALTER TABLE WritingAttempts ADD LessonId uniqueidentifier NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WritingAttempts') AND name = 'StudentId')
                ALTER TABLE WritingAttempts ADD StudentId uniqueidentifier NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WritingAttempts') AND name = 'AttemptNumber')
                ALTER TABLE WritingAttempts ADD AttemptNumber int NOT NULL DEFAULT 1;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WritingAttempts') AND name = 'DisplayMessage')
                ALTER TABLE WritingAttempts ADD DisplayMessage nvarchar(500) NOT NULL DEFAULT '';
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WritingAttempts') AND name = 'SpokenFeedback')
                ALTER TABLE WritingAttempts ADD SpokenFeedback nvarchar(500) NOT NULL DEFAULT '';
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WritingAttempts') AND name = 'MistakesJson')
                ALTER TABLE WritingAttempts ADD MistakesJson nvarchar(max) NOT NULL DEFAULT '[]';
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WritingAttempts') AND name = 'TipsJson')
                ALTER TABLE WritingAttempts ADD TipsJson nvarchar(max) NOT NULL DEFAULT '[]';
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FluencyReports') AND name = 'IsAccepted')
                ALTER TABLE FluencyReports ADD IsAccepted bit NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FluencyReports') AND name = 'AttemptNumber')
                ALTER TABLE FluencyReports ADD AttemptNumber int NOT NULL DEFAULT 1;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FluencyReports') AND name = 'DisplayMessage')
                ALTER TABLE FluencyReports ADD DisplayMessage nvarchar(500) NOT NULL DEFAULT '';
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FluencyReports') AND name = 'SpokenFeedback')
                ALTER TABLE FluencyReports ADD SpokenFeedback nvarchar(500) NOT NULL DEFAULT '';
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FluencyReports') AND name = 'TipsJson')
                ALTER TABLE FluencyReports ADD TipsJson nvarchar(max) NOT NULL DEFAULT '[]';
            """);
        logger.LogInformation("Phase 3 feedback columns ensured.");
    }
    catch (Exception ex) { logger.LogError(ex, "Phase 3 column migration failed."); }

    try
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LessonPageCompletions')
                CREATE TABLE LessonPageCompletions (
                    Id uniqueidentifier NOT NULL DEFAULT newsequentialid() PRIMARY KEY,
                    ChildName nvarchar(200) NOT NULL,
                    LessonId uniqueidentifier NOT NULL,
                    LessonPageId uniqueidentifier NOT NULL,
                    WritingSubmitted bit NOT NULL DEFAULT 0,
                    CompletedAt datetime2 NOT NULL DEFAULT GETUTCDATE()
                );
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('LessonPageCompletions') AND name = 'IX_LessonPageCompletions_Child_Lesson')
                CREATE INDEX IX_LessonPageCompletions_Child_Lesson ON LessonPageCompletions (ChildName, LessonId);
            """);
        logger.LogInformation("LessonPageCompletions table ensured.");
    }
    catch (Exception ex) { logger.LogError(ex, "LessonPageCompletions migration failed."); }
}

// ── Pipeline ──────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Arabic Story API v1"));
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("Angular");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
