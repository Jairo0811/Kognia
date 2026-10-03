using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kognia.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KogniaDbContext))]
[Migration("20261003223000_Block1InitialSchema")]
public sealed class Block1InitialSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [Categories] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(120) NOT NULL,
    [Slug] nvarchar(140) NOT NULL,
    [Description] nvarchar(max) NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RefreshTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [TokenHash] nvarchar(128) NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [ExpiresAtUtc] datetimeoffset NOT NULL,
    [RevokedAtUtc] datetimeoffset NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Courses] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(180) NOT NULL,
    [Slug] nvarchar(200) NOT NULL,
    [Summary] nvarchar(400) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Level] nvarchar(50) NOT NULL,
    [InstructorUserId] nvarchar(450) NOT NULL,
    [CategoryId] uniqueidentifier NOT NULL,
    [Status] int NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [PublishedAtUtc] datetimeoffset NULL,
    CONSTRAINT [PK_Courses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Courses_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [CourseSections] (
    [Id] uniqueidentifier NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [Title] nvarchar(180) NOT NULL,
    [Position] int NOT NULL,
    CONSTRAINT [PK_CourseSections] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseSections_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Lessons] (
    [Id] uniqueidentifier NOT NULL,
    [CourseSectionId] uniqueidentifier NOT NULL,
    [Title] nvarchar(180) NOT NULL,
    [LessonType] nvarchar(30) NOT NULL,
    [Content] nvarchar(max) NULL,
    [VideoUrl] nvarchar(max) NULL,
    [Position] int NOT NULL,
    [IsPreview] bit NOT NULL,
    CONSTRAINT [PK_Lessons] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Lessons_CourseSections_CourseSectionId] FOREIGN KEY ([CourseSectionId]) REFERENCES [CourseSections] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
CREATE UNIQUE INDEX [IX_Categories_Slug] ON [Categories] ([Slug]);
CREATE INDEX [IX_Courses_CategoryId] ON [Courses] ([CategoryId]);
CREATE UNIQUE INDEX [IX_Courses_Slug] ON [Courses] ([Slug]);
CREATE INDEX [IX_CourseSections_CourseId] ON [CourseSections] ([CourseId]);
CREATE INDEX [IX_Lessons_CourseSectionId] ON [Lessons] ([CourseSectionId]);
CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TABLE [AspNetRoleClaims];
DROP TABLE [AspNetUserLogins];
DROP TABLE [AspNetUserRoles];
DROP TABLE [AspNetUserTokens];
DROP TABLE [AspNetUserClaims];
DROP TABLE [RefreshTokens];
DROP TABLE [Lessons];
DROP TABLE [AspNetRoles];
DROP TABLE [AspNetUsers];
DROP TABLE [CourseSections];
DROP TABLE [Courses];
DROP TABLE [Categories];
""");
    }
}
