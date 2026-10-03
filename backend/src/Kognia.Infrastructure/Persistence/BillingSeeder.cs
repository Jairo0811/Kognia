using Kognia.Domain.Billing;
using Microsoft.EntityFrameworkCore;

namespace Kognia.Infrastructure.Persistence;

public static class BillingSeeder
{
    public static async Task SeedAsync(KogniaDbContext db)
    {
        if (await db.SubscriptionPlans.AnyAsync()) return;

        db.SubscriptionPlans.AddRange(
            new SubscriptionPlan
            {
                Code = "FREE",
                Name = "Free",
                Description = "Acceso básico para comenzar a aprender en Kognia.",
                BillingPeriod = BillingPeriod.Monthly,
                PriceAmount = 0,
                Currency = "DOP",
                SortOrder = 0
            },
            new SubscriptionPlan
            {
                Code = "PREMIUM_MONTHLY",
                Name = "Premium Mensual",
                Description = "Acceso Premium con facturación mensual.",
                BillingPeriod = BillingPeriod.Monthly,
                PriceAmount = 699,
                Currency = "DOP",
                SortOrder = 1
            },
            new SubscriptionPlan
            {
                Code = "PREMIUM_ANNUAL",
                Name = "Premium Anual",
                Description = "Acceso Premium anual con precio preferencial.",
                BillingPeriod = BillingPeriod.Annual,
                PriceAmount = 6999,
                Currency = "DOP",
                SortOrder = 2
            });

        await db.SaveChangesAsync();
    }
}
