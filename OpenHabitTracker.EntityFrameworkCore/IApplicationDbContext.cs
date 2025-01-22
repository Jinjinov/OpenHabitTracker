using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.EntityFrameworkCore;

public interface IApplicationDbContext
{
    IQueryable<IUserEntity> Users { get; }
    DbSet<CategoryEntity> Categories { get; }
    DbSet<ContentEntity> Contents { get; }
    DbSet<HabitEntity> Habits { get; }
    DbSet<ItemEntity> Items { get; }
    DbSet<NoteEntity> Notes { get; }
    DbSet<PriorityEntity> Priorities { get; }
    DbSet<SettingsEntity> Settings { get; }
    DbSet<TaskEntity> Tasks { get; }
    DbSet<TimeEntity> Times { get; }

    DatabaseFacade Database { get; }
    IModel Model { get; }
    ChangeTracker ChangeTracker { get; }

    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;
    void AddRange(IEnumerable<object> entities);
    EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
