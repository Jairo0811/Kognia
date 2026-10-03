using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kognia.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KogniaDbContext))]
[Migration("20261003231500_Block2LearningAssessmentCertificates")]
public sealed class Block2LearningAssessmentCertificates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE [Enrollments] (
    [Id] uniqueidentifier NOT NULL,
    [StudentUserId] nvarchar(450) NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [Status] int NOT NULL,
    [EnrolledAtUtc] datetimeoffset NOT NULL,
    [CompletedAtUtc] datetimeoffset NULL,
    CONSTRAINT [PK_Enrollments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Enrollments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Quizzes] (
    [Id] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [Title] nvarchar(180) NOT NULL,
    [PassingScorePercent] int NOT NULL,
    [IsPublished] bit NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Quizzes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Quizzes_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Certificates] (
    [Id] uniqueidentifier NOT NULL,
    [StudentUserId] nvarchar(450) NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [VerificationCode] nvarchar(64) NOT NULL,
    [IssuedAtUtc] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Certificates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Certificates_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LessonProgress] (
    [Id] uniqueidentifier NOT NULL,
    [EnrollmentId] uniqueidentifier NOT NULL,
    [LessonId] uniqueidentifier NOT NULL,
    [IsCompleted] bit NOT NULL,
    [LastPositionSeconds] int NOT NULL,
    [LastAccessedAtUtc] datetimeoffset NOT NULL,
    [CompletedAtUtc] datetimeoffset NULL,
    CONSTRAINT [PK_LessonProgress] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LessonProgress_Enrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [Enrollments] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LessonProgress_Lessons_LessonId] FOREIGN KEY ([LessonId]) REFERENCES [Lessons] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Questions] (
    [Id] uniqueidentifier NOT NULL,
    [QuizId] uniqueidentifier NOT NULL,
    [Text] nvarchar(1000) NOT NULL,
    [Position] int NOT NULL,
    CONSTRAINT [PK_Questions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Questions_Quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [Quizzes] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [QuizAttempts] (
    [Id] uniqueidentifier NOT NULL,
    [QuizId] uniqueidentifier NOT NULL,
    [StudentUserId] nvarchar(450) NOT NULL,
    [ScorePercent] decimal(5,2) NOT NULL,
    [Passed] bit NOT NULL,
    [SubmittedAtUtc] datetimeoffset NOT NULL,
    CONSTRAINT [PK_QuizAttempts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuizAttempts_Quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [Quizzes] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [QuestionOptions] (
    [Id] uniqueidentifier NOT NULL,
    [QuestionId] uniqueidentifier NOT NULL,
    [Text] nvarchar(500) NOT NULL,
    [IsCorrect] bit NOT NULL,
    [Position] int NOT NULL,
    CONSTRAINT [PK_QuestionOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuestionOptions_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [QuizAnswers] (
    [Id] uniqueidentifier NOT NULL,
    [QuizAttemptId] uniqueidentifier NOT NULL,
    [QuestionId] uniqueidentifier NOT NULL,
    [SelectedOptionId] uniqueidentifier NULL,
    [IsCorrect] bit NOT NULL,
    CONSTRAINT [PK_QuizAnswers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuizAnswers_QuizAttempts_QuizAttemptId] FOREIGN KEY ([QuizAttemptId]) REFERENCES [QuizAttempts] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Enrollments_CourseId] ON [Enrollments] ([CourseId]);
CREATE UNIQUE INDEX [IX_Enrollments_StudentUserId_CourseId] ON [Enrollments] ([StudentUserId], [CourseId]);
CREATE INDEX [IX_LessonProgress_LessonId] ON [LessonProgress] ([LessonId]);
CREATE UNIQUE INDEX [IX_LessonProgress_EnrollmentId_LessonId] ON [LessonProgress] ([EnrollmentId], [LessonId]);
CREATE INDEX [IX_Quizzes_CourseId] ON [Quizzes] ([CourseId]);
CREATE INDEX [IX_Questions_QuizId] ON [Questions] ([QuizId]);
CREATE INDEX [IX_QuestionOptions_QuestionId] ON [QuestionOptions] ([QuestionId]);
CREATE INDEX [IX_QuizAttempts_QuizId] ON [QuizAttempts] ([QuizId]);
CREATE INDEX [IX_QuizAttempts_StudentUserId] ON [QuizAttempts] ([StudentUserId]);
CREATE INDEX [IX_QuizAnswers_QuizAttemptId] ON [QuizAnswers] ([QuizAttemptId]);
CREATE INDEX [IX_Certificates_CourseId] ON [Certificates] ([CourseId]);
CREATE UNIQUE INDEX [IX_Certificates_VerificationCode] ON [Certificates] ([VerificationCode]);
CREATE UNIQUE INDEX [IX_Certificates_StudentUserId_CourseId] ON [Certificates] ([StudentUserId], [CourseId]);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TABLE [QuizAnswers];
DROP TABLE [QuestionOptions];
DROP TABLE [LessonProgress];
DROP TABLE [Certificates];
DROP TABLE [QuizAttempts];
DROP TABLE [Questions];
DROP TABLE [Enrollments];
DROP TABLE [Quizzes];
""");
    }
}
