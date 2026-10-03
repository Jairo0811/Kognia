using Kognia.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Kognia.Infrastructure.Persistence;

public static class CatalogSeeder
{
    public static async Task SeedAsync(KogniaDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        db.Categories.AddRange(
            new Category { Name = "Programación", Slug = "programacion", Description = "Desarrollo de software, web y aplicaciones." },
            new Category { Name = "Marketing Digital", Slug = "marketing-digital", Description = "Estrategia, contenido, analítica y crecimiento digital." },
            new Category { Name = "Desarrollo Personal", Slug = "desarrollo-personal", Description = "Habilidades personales y profesionales." }
        );

        await db.SaveChangesAsync();
    }
}
