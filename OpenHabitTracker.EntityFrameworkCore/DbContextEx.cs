using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace OpenHabitTracker.EntityFrameworkCore;

public static class DbContextEx
{
    public static void ClearAllTables(this IApplicationDbContext dbContext)
    {
        foreach (IEntityType entityType in dbContext.Model.GetEntityTypes())
        {
            string? tableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                dbContext.Database.ExecuteSqlRaw($"DELETE FROM {tableName}");
                dbContext.Database.ExecuteSqlRaw($"DELETE FROM sqlite_sequence WHERE name = '{tableName}'");
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.
            }
        }

        foreach (EntityEntry entry in dbContext.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }
}
