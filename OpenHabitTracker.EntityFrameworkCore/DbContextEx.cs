using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.ChangeTracking;
//using Microsoft.EntityFrameworkCore.Metadata;

namespace OpenHabitTracker.EntityFrameworkCore;

public static class DbContextEx
{
    public static void DeleteAllUserData(this IApplicationDbContext dbContext)
    {
        //dbContext.Users.ExecuteDelete();
        dbContext.Contents.ExecuteDelete();
        dbContext.Habits.ExecuteDelete();
        dbContext.Notes.ExecuteDelete();
        dbContext.Tasks.ExecuteDelete();
        dbContext.Times.ExecuteDelete();
        dbContext.Items.ExecuteDelete();
        dbContext.Categories.ExecuteDelete();
        dbContext.Priorities.ExecuteDelete();
        dbContext.Settings.ExecuteDelete();

        dbContext.ChangeTracker.Clear();

        /*
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
            string? tableName = entry.Metadata.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                entry.State = EntityState.Detached;
            }
        }
        */
    }
}
