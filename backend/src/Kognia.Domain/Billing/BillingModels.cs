namespace Kognia.Domain.Billing;

public enum BillingPeriod
{
    Monthly = 0,
    Annual = 1
}

public enum SubscriptionStatus
{
    Pending = 0,
    Active = 1,
    Cancelled = 2,
    Expired = 3
}

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}

public enum InvoiceStatus
{
    Open = 0,
    Paid = 1,
    Void = 2
}

public sealed class SubscriptionPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BillingPeriod BillingPeriod { get; set; }
    public decimal PriceAmount { get; set; }
    public string Currency { get; set; } = "DOP";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}

public sealed class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CurrentPeriodStartUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CurrentPeriodEndUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string Provider { get; set; } = "Internal";
    public string? ProviderSubscriptionId { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "DOP";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string Provider { get; set; } = "Internal";
    public string? ProviderPaymentId { get; set; }
    public string CheckoutToken { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAtUtc { get; set; }
}

public sealed class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "DOP";
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;
    public DateTimeOffset IssuedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAtUtc { get; set; }
}
