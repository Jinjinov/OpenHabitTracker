using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Data;

public static class DataExtensions
{
    public static CategoryEntity ToEntity(this CategoryModel model)
    {
        CategoryEntity entity = new();
        model.CopyToEntity(entity);
        return entity;
    }

    public static void CopyToEntity(this CategoryModel model, CategoryEntity entity)
    {
        entity.Id = model.Id;
        entity.Title = model.Title;
    }

    public static HabitEntity ToEntity(this HabitModel model)
    {
        HabitEntity entity = new();
        model.CopyToEntity(entity);
        return entity;
    }

    public static void CopyToEntity(this HabitModel model, HabitEntity entity)
    {
        entity.CategoryId = model.CategoryId;
        entity.Priority = model.Priority;
        entity.IsDeleted = model.IsDeleted;
        entity.Title = model.Title;
        entity.Color = model.Color;
        entity.CreatedAt = model.CreatedAt;
        entity.UpdatedAt = model.UpdatedAt;

        entity.RepeatCount = model.RepeatCount;
        entity.RepeatInterval = model.RepeatInterval;
        entity.RepeatPeriod = model.RepeatPeriod;
        entity.Duration = model.Duration;
        entity.LastTimeDoneAt = model.LastTimeDoneAt;
    }

    public static ItemEntity ToEntity(this ItemModel model)
    {
        ItemEntity entity = new();
        model.CopyToEntity(entity);
        return entity;
    }

    public static void CopyToEntity(this ItemModel model, ItemEntity entity)
    {
        entity.Id = model.Id;
        entity.ParentId = model.ParentId;
        entity.Title = model.Title;
        entity.DoneAt = model.DoneAt;
    }

    public static NoteEntity ToEntity(this NoteModel model)
    {
        NoteEntity entity = new();
        model.CopyToEntity(entity);
        return entity;
    }

    public static void CopyToEntity(this NoteModel model, NoteEntity entity)
    {
        entity.CategoryId = model.CategoryId;
        entity.Priority = model.Priority;
        entity.IsDeleted = model.IsDeleted;
        entity.Title = model.Title;
        entity.Color = model.Color;
        entity.CreatedAt = model.CreatedAt;
        entity.UpdatedAt = model.UpdatedAt;

        entity.Content = model.Content;
    }

    public static PriorityEntity ToEntity(this PriorityModel model)
    {
        PriorityEntity entity = new();
        model.CopyToEntity(entity);
        return entity;
    }

    public static void CopyToEntity(this PriorityModel model, PriorityEntity entity)
    {
        entity.Id = model.Id;
        entity.Title = model.Title;
    }

    public static SettingsEntity ToEntity(this SettingsModel model)
    {
        SettingsEntity entity = new();
        model.CopyToEntity(entity);
        return entity;
    }

    public static void CopyToEntity(this SettingsModel model, SettingsEntity entity)
    {
        entity.Id = model.Id;
        entity.IsDarkMode = model.IsDarkMode;
        entity.Theme = model.Theme;
        entity.StartPage = model.StartPage;
        entity.StartSidebar = model.StartSidebar;
        entity.Culture = model.Culture;
        entity.FirstDayOfWeek = model.FirstDayOfWeek;
        entity.SelectedRatio = model.SelectedRatio;
        entity.ShowItemList = model.ShowItemList;
        entity.ShowSmallCalendar = model.ShowSmallCalendar;
        entity.ShowLargeCalendar = model.ShowLargeCalendar;
        entity.ShowColor = model.ShowColor;
        entity.ShowCreatedUpdated = model.ShowCreatedUpdated;
        entity.InsertTabsInNoteContent = model.InsertTabsInNoteContent;
        entity.DisplayNoteContentAsMarkdown = model.DisplayNoteContentAsMarkdown;
        entity.ShowOnlyOverSelectedRatioMin = model.ShowOnlyOverSelectedRatioMin;
        entity.SelectedRatioMin = model.SelectedRatioMin;
        entity.HorizontalMargin = model.HorizontalMargin;
        entity.VerticalMargin = model.VerticalMargin;
        entity.HiddenCategoryIds = model.HiddenCategoryIds;
        entity.ShowPriority = model.ShowPriority;
        entity.SortBy = model.SortBy;
    }

    public static TaskEntity ToEntity(this TaskModel model)
    {
        TaskEntity entity = new();
        model.CopyToEntity(entity);
        return entity;
    }

    public static void CopyToEntity(this TaskModel model, TaskEntity entity)
    {
        entity.CategoryId = model.CategoryId;
        entity.Priority = model.Priority;
        entity.IsDeleted = model.IsDeleted;
        entity.Title = model.Title;
        entity.Color = model.Color;
        entity.CreatedAt = model.CreatedAt;
        entity.UpdatedAt = model.UpdatedAt;

        entity.StartedAt = model.StartedAt;
        entity.CompletedAt = model.CompletedAt;
        entity.PlannedAt = model.PlannedAt;
        entity.Duration = model.Duration;
    }

    public static TimeEntity ToEntity(this TimeModel model)
    {
        TimeEntity entity = new();
        model.CopyToEntity(entity);
        return entity;
    }

    public static void CopyToEntity(this TimeModel model, TimeEntity entity)
    {
        entity.Id = model.Id;
        entity.HabitId = model.HabitId;
        entity.StartedAt = model.StartedAt;
        entity.CompletedAt = model.CompletedAt;
    }
}
