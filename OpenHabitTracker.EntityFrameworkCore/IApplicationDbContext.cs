using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.EntityFrameworkCore;

public interface IApplicationDbContext
{
    DbSet<CategoryEntity> Categories { get; set; }
    DbSet<ContentEntity> Contents { get; set; }
    DbSet<HabitEntity> Habits { get; set; }
    DbSet<ItemEntity> Items { get; set; }
    DbSet<NoteEntity> Notes { get; set; }
    DbSet<PriorityEntity> Priorities { get; set; }
    DbSet<SettingsEntity> Settings { get; set; }
    DbSet<TaskEntity> Tasks { get; set; }
    DbSet<TimeEntity> Times { get; set; }

    DatabaseFacade Database { get; }
    IModel Model { get; }
    ChangeTracker ChangeTracker { get; }

    EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;
    void AddRange(IEnumerable<object> entities);
    EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
