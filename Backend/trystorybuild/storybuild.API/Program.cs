using storybuild.API.Middleware;
using Application.Agent;
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
builder.Services.AddScoped<Application.Agent.LessonGenerationAgent>();

// ── File upload limit: 20 MB ──────────────────────────────────────────────────
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 20 * 1024 * 1024;
});

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(o =>
    o.AddPolicy("Angular", p =>
        p.WithOrigins("http://localhost:4200")
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
            """);
        logger.LogInformation("Story columns ensured.");
    }
    catch (Exception ex) { logger.LogError(ex, "Story column migration failed."); }
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

app.Run();
