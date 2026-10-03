using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kognia.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KogniaDbContext))]
[Migration("20261003234500_Block3BillingDashboards")]
public sealed class Block3BillingDashboards : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE [SubscriptionPlans] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(120) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [BillingPeriod] int NOT NULL,
    [PriceAmount] decimal(12,2) NOT NULL,
    [Currency] nvarchar(3) NOT NULL,
    [IsActive] bit NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_SubscriptionPlans] PRIMARY KEY ([Id])
);

CREATE TABLE [Subscriptions] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [PlanId] uniqueidentifier NOT NULL,
    [Status] int NOT NULL,
    [StartedAtUtc] datetimeoffset NOT NULL,
    [CurrentPeriodStartUtc] datetimeoffset NOT NULL,
    [CurrentPeriodEndUtc] datetimeoffset NOT NULL,
    [CancelAtPeriodEnd] bit NOT NULL,
    [CancelledAtUtc] datetimeoffset NULL,
    [Provider] nvarchar(40) NOT NULL,
    [ProviderSubscriptionId] nvarchar(120) NULL,
    CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Subscriptions_SubscriptionPlans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [SubscriptionPlans] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Payments] (
    [Id] uniqueidentifier NOT NULL,
    [SubscriptionId] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [Amount] decimal(12,2) NOT NULL,
    [Currency] nvarchar(3) NOT NULL,
    [Status] int NOT NULL,
    [Provider] nvarchar(40) NOT NULL,
    [ProviderPaymentId] nvarchar(120) NULL,
    [CheckoutToken] nvarchar(80) NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [PaidAtUtc] datetimeoffset NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Payments_Subscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [Subscriptions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Invoices] (
    [Id] uniqueidentifier NOT NULL,
    [SubscriptionId] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [Number] nvarchar(60) NOT NULL,
    [Amount] decimal(12,2) NOT NULL,
    [Currency] nvarchar(3) NOT NULL,
    [Status] int NOT NULL,
    [IssuedAtUtc] datetimeoffset NOT NULL,
    [PaidAtUtc] datetimeoffset NULL,
    CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Invoices_Subscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [Subscriptions] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_SubscriptionPlans_Code] ON [SubscriptionPlans] ([Code]);
CREATE INDEX [IX_Subscriptions_PlanId] ON [Subscriptions] ([PlanId]);
CREATE INDEX [IX_Subscriptions_UserId_Status] ON [Subscriptions] ([UserId], [Status]);
CREATE UNIQUE INDEX [IX_Payments_CheckoutToken] ON [Payments] ([CheckoutToken]);
CREATE INDEX [IX_Payments_SubscriptionId] ON [Payments] ([SubscriptionId]);
CREATE UNIQUE INDEX [IX_Invoices_Number] ON [Invoices] ([Number]);
CREATE INDEX [IX_Invoices_SubscriptionId] ON [Invoices] ([SubscriptionId]);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TABLE [Invoices];
DROP TABLE [Payments];
DROP TABLE [Subscriptions];
DROP TABLE [SubscriptionPlans];
""");
    }
}
