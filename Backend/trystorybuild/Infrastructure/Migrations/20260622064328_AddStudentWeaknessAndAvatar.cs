using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentWeaknessAndAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // All operations are idempotent — safe to run even if Program.cs raw-SQL
            // already applied some of these changes on an existing database.

            // ── Students ──────────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Students') AND name = 'WeaknessMapJson')
                    ALTER TABLE Students ADD WeaknessMapJson nvarchar(max) NOT NULL DEFAULT '{}';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Students') AND name = 'AvatarEmoji')
                    ALTER TABLE Students ADD AvatarEmoji nvarchar(max) NULL;
                """);

            // ── WritingAttempts ───────────────────────────────────────────────────
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WritingAttempts') AND name = 'LessonId')
                    ALTER TABLE WritingAttempts ADD LessonId uniqueidentifier NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WritingAttempts') AND name = 'StudentId')
                    ALTER TABLE WritingAttempts ADD StudentId uniqueidentifier NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WritingAttempts') AND name = 'AttemptNumber')
                    ALTER TABLE WritingAttempts ADD AttemptNumber int NOT NULL DEFAULT 1;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WritingAttempts') AND name = 'DisplayMessage')
                    ALTER TABLE WritingAttempts ADD DisplayMessage nvarchar(max) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WritingAttempts') AND name = 'SpokenFeedback')
                    ALTER TABLE WritingAttempts ADD SpokenFeedback nvarchar(max) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WritingAttempts') AND name = 'MistakesJson')
                    ALTER TABLE WritingAttempts ADD MistakesJson nvarchar(max) NOT NULL DEFAULT '[]';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WritingAttempts') AND name = 'TipsJson')
                    ALTER TABLE WritingAttempts ADD TipsJson nvarchar(max) NOT NULL DEFAULT '[]';
                """);

            // ── FluencyReports ────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FluencyReports') AND name = 'IsAccepted')
                    ALTER TABLE FluencyReports ADD IsAccepted bit NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FluencyReports') AND name = 'AttemptNumber')
                    ALTER TABLE FluencyReports ADD AttemptNumber int NOT NULL DEFAULT 1;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FluencyReports') AND name = 'DisplayMessage')
                    ALTER TABLE FluencyReports ADD DisplayMessage nvarchar(max) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FluencyReports') AND name = 'SpokenFeedback')
                    ALTER TABLE FluencyReports ADD SpokenFeedback nvarchar(max) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FluencyReports') AND name = 'TipsJson')
                    ALTER TABLE FluencyReports ADD TipsJson nvarchar(max) NOT NULL DEFAULT '[]';
                """);

            // ── LessonPages ───────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('LessonPages') AND name = 'IsEmbedded')
                    ALTER TABLE LessonPages ADD IsEmbedded bit NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('LessonPages') AND name = 'ChromaId')
                    ALTER TABLE LessonPages ADD ChromaId nvarchar(max) NULL;
                """);

            // ── Lessons / Stories lifecycle ───────────────────────────────────────
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Lessons') AND name = 'IsPublished')
                    ALTER TABLE Lessons ADD IsPublished bit NOT NULL DEFAULT 1;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Lessons') AND name = 'Status')
                    ALTER TABLE Lessons ADD Status int NOT NULL DEFAULT 3;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Stories') AND name = 'IsPublished')
                    ALTER TABLE Stories ADD IsPublished bit NOT NULL DEFAULT 1;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Stories') AND name = 'Status')
                    ALTER TABLE Stories ADD Status int NOT NULL DEFAULT 3;
                """);

            // ── New tables ────────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AssignmentSubmissions')
                    CREATE TABLE AssignmentSubmissions (
                        Id uniqueidentifier NOT NULL DEFAULT newsequentialid() PRIMARY KEY,
                        AssignmentId uniqueidentifier NOT NULL,
                        StudentId uniqueidentifier NOT NULL,
                        ChildName nvarchar(max) NOT NULL DEFAULT '',
                        PagesCompleted int NOT NULL DEFAULT 0,
                        TotalPages int NOT NULL DEFAULT 0,
                        WritingScore float NOT NULL DEFAULT 0,
                        IsComplete bit NOT NULL DEFAULT 0,
                        SubmittedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        NotesJson nvarchar(max) NOT NULL DEFAULT '{}'
                    );
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LessonPageCompletions')
                    CREATE TABLE LessonPageCompletions (
                        Id uniqueidentifier NOT NULL DEFAULT newsequentialid() PRIMARY KEY,
                        ChildName nvarchar(max) NOT NULL,
                        LessonId uniqueidentifier NOT NULL,
                        LessonPageId uniqueidentifier NOT NULL,
                        WritingSubmitted bit NOT NULL DEFAULT 0,
                        CompletedAt datetime2 NOT NULL DEFAULT GETUTCDATE()
                    );
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'StudentLevelHistories')
                    CREATE TABLE StudentLevelHistories (
                        Id uniqueidentifier NOT NULL DEFAULT newsequentialid() PRIMARY KEY,
                        StudentId uniqueidentifier NOT NULL,
                        ChildName nvarchar(max) NOT NULL DEFAULT '',
                        PreviousLevel int NOT NULL DEFAULT 0,
                        NewLevel int NOT NULL DEFAULT 0,
                        ChangedByUserId uniqueidentifier NOT NULL,
                        ChangedByRole nvarchar(max) NOT NULL DEFAULT '',
                        Reason nvarchar(max) NOT NULL DEFAULT '',
                        ChangedAt datetime2 NOT NULL DEFAULT GETUTCDATE()
                    );
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WeakLetterRecords')
                    CREATE TABLE WeakLetterRecords (
                        Id uniqueidentifier NOT NULL DEFAULT newsequentialid() PRIMARY KEY,
                        StudentId uniqueidentifier NOT NULL,
                        ChildName nvarchar(max) NOT NULL DEFAULT '',
                        Letter nvarchar(max) NOT NULL DEFAULT '',
                        Attempts int NOT NULL DEFAULT 0,
                        Correct int NOT NULL DEFAULT 0,
                        ActivityType nvarchar(max) NOT NULL DEFAULT 'Writing',
                        LastSeenAt datetime2 NOT NULL DEFAULT GETUTCDATE()
                    );
                """);

            // ── Index on AssignmentSubmissions ────────────────────────────────────
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('AssignmentSubmissions') AND name = 'IX_AssignmentSubmissions_AssignmentId')
                    CREATE INDEX IX_AssignmentSubmissions_AssignmentId ON AssignmentSubmissions (AssignmentId);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AssignmentSubmissions') DROP TABLE AssignmentSubmissions;
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LessonPageCompletions')  DROP TABLE LessonPageCompletions;
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'StudentLevelHistories')  DROP TABLE StudentLevelHistories;
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WeakLetterRecords')       DROP TABLE WeakLetterRecords;
                """);
        }
    }
}
