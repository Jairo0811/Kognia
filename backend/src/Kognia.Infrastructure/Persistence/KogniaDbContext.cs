using Microsoft.EntityFrameworkCore;

namespace Kognia.Infrastructure.Persistence;

public sealed class KogniaDbContext(DbContextOptions<KogniaDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KogniaDbContext).Assembly);
    }
}
