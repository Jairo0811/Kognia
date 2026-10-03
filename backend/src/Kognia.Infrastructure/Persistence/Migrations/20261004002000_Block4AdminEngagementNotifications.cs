using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kognia.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KogniaDbContext))]
[Migration("20261004002000_Block4AdminEngagementNotifications")]
public sealed class Block4AdminEngagementNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE [Reviews] (
    [Id] uniqueidentifier NOT NULL,
    [StudentUserId] nvarchar(450) NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [Rating] int NOT NULL,
    [Comment] nvarchar(2000) NOT NULL,
    [IsVisible] bit NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [UpdatedAtUtc] datetimeoffset NULL,
    CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reviews_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
);
CREATE UNIQUE INDEX [IX_Reviews_StudentUserId_CourseId] ON [Reviews] ([StudentUserId], [CourseId]);
CREATE INDEX [IX_Reviews_CourseId_IsVisible] ON [Reviews] ([CourseId], [IsVisible]);

CREATE TABLE [Favorites] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [CourseId] uniqueidentifier NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Favorites] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Favorites_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
);
CREATE UNIQUE INDEX [IX_Favorites_UserId_CourseId] ON [Favorites] ([UserId], [CourseId]);
CREATE INDEX [IX_Favorites_CourseId] ON [Favorites] ([CourseId]);

CREATE TABLE [Notifications] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [Type] int NOT NULL,
    [Title] nvarchar(180) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [ActionUrl] nvarchar(500) NULL,
    [IsRead] bit NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [ReadAtUtc] datetimeoffset NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
);
CREATE INDEX [IX_Notifications_UserId_IsRead_CreatedAtUtc] ON [Notifications] ([UserId], [IsRead], [CreatedAtUtc]);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Notifications");
        migrationBuilder.DropTable(name: "Favorites");
        migrationBuilder.DropTable(name: "Reviews");
    }
}
