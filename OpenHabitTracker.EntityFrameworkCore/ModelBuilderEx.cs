using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using System.Text.Json;

namespace OpenHabitTracker.EntityFrameworkCore;

public static class ModelBuilderEx
{
    public static void OnModelCreating(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContentEntity>().HasIndex(x => x.CategoryId);

        modelBuilder.Entity<TimeEntity>().HasIndex(x => x.HabitId);

        modelBuilder.Entity<ItemEntity>().HasIndex(x => x.ParentId);

        var contentTypeSortComparer = new ValueComparer<Dictionary<ContentType, Sort>>(
            (c1, c2) => ReferenceEquals(c1, c2) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)
        );

        modelBuilder.Entity<SettingsEntity>()
            .Property(e => e.SortBy)
            .HasColumnName("SortBy")
            .HasConversion(
                dictionary => JsonSerializer.Serialize(dictionary, (JsonSerializerOptions?)null),
                json => string.IsNullOrWhiteSpace(json)
                    ? new Dictionary<ContentType, Sort>()
                    {
                        { ContentType.Note, Sort.Priority },
                        { ContentType.Task, Sort.PlannedAt },
                        { ContentType.Habit, Sort.SelectedRatio }
                    }
                    : JsonSerializer.Deserialize<Dictionary<ContentType, Sort>>(json, (JsonSerializerOptions?)null)!,
                contentTypeSortComparer);

        var priorityBoolComparer = new ValueComparer<Dictionary<Priority, bool>>(
            (c1, c2) => ReferenceEquals(c1, c2) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)
        );

        modelBuilder.Entity<SettingsEntity>()
            .Property(e => e.ShowPriority)
            .HasColumnName("ShowPriority")
            .HasConversion(
                dictionary => JsonSerializer.Serialize(dictionary, (JsonSerializerOptions?)null),
                json => string.IsNullOrWhiteSpace(json)
                    ? new Dictionary<Priority, bool>()
                    {
                        { Priority.None, true },
                        { Priority.VeryLow, true },
                        { Priority.Low, true },
                        { Priority.Medium, true },
                        { Priority.High, true },
                        { Priority.VeryHigh, true }
                    }
                    : JsonSerializer.Deserialize<Dictionary<Priority, bool>>(json, (JsonSerializerOptions?)null)!,
                priorityBoolComparer);

        var querySectionBoolComparer = new ValueComparer<Dictionary<QuerySection, bool>>(
            (c1, c2) => ReferenceEquals(c1, c2) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)
        );

        modelBuilder.Entity<SettingsEntity>()
            .Property(e => e.FoldSection)
            .HasColumnName("FoldSection")
            .HasConversion(
                dictionary => JsonSerializer.Serialize(dictionary, (JsonSerializerOptions?)null),
                json => string.IsNullOrWhiteSpace(json)
                    ? new Dictionary<QuerySection, bool>()
                    {
                        { QuerySection.Search, false },
                        { QuerySection.FilterByDate, false },
                        { QuerySection.FilterByCategory, false },
                        { QuerySection.FilterByPriority, false },
                        { QuerySection.FilterByStatus, false },
                        { QuerySection.Sort, false }
                    }
                    : JsonSerializer.Deserialize<Dictionary<QuerySection, bool>>(json, (JsonSerializerOptions?)null)!,
                querySectionBoolComparer);
    }
}
