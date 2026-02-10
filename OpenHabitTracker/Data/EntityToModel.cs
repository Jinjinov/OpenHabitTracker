using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Data;

public static class EntityToModel
{
    public static CategoryModel ToModel(this CategoryEntity entity)
    {
        CategoryModel model = new();
        entity.CopyToModel(model);
        return model;
    }

    public static void CopyToModel(this CategoryEntity entity, CategoryModel model)
    {
        model.Id = entity.Id;
        model.UserId = entity.UserId;
        model.Title = entity.Title;
    }

    public static HabitModel ToModel(this HabitEntity entity)
    {
        HabitModel model = new();
        entity.CopyToModel(model);
        return model;
    }

    public static void CopyToModel(this HabitEntity entity, HabitModel model)
    {
        model.Id = entity.Id;
        model.CategoryId = entity.CategoryId;
        model.Priority = entity.Priority;
        model.IsDeleted = entity.IsDeleted;
        model.Title = entity.Title;
        model.Color = entity.Color;
        model.CreatedAt = entity.CreatedAt;
        model.UpdatedAt = entity.UpdatedAt;

        model.RepeatCount = entity.RepeatCount;
        model.RepeatInterval = entity.RepeatInterval;
        model.RepeatPeriod = entity.RepeatPeriod;
        model.Duration = entity.Duration;
        model.LastTimeDoneAt = entity.LastTimeDoneAt;
    }

    public static ItemModel ToModel(this ItemEntity entity)
    {
        ItemModel model = new();
        entity.CopyToModel(model);
        return model;
    }

    public static void CopyToModel(this ItemEntity entity, ItemModel model)
    {
        model.Id = entity.Id;
        model.ParentId = entity.ParentId;
        model.Title = entity.Title;
        model.DoneAt = entity.DoneAt;
    }

    public static NoteModel ToModel(this NoteEntity entity)
    {
        NoteModel model = new();
        entity.CopyToModel(model);
        return model;
    }

    public static void CopyToModel(this NoteEntity entity, NoteModel model)
    {
        model.Id = entity.Id;
        model.CategoryId = entity.CategoryId;
        model.Priority = entity.Priority;
        model.IsDeleted = entity.IsDeleted;
        model.Title = entity.Title;
        model.Color = entity.Color;
        model.CreatedAt = entity.CreatedAt;
        model.UpdatedAt = entity.UpdatedAt;

        model.Content = entity.Content;
    }

    public static PriorityModel ToModel(this PriorityEntity entity)
    {
        PriorityModel model = new();
        entity.CopyToModel(model);
        return model;
    }

    public static void CopyToModel(this PriorityEntity entity, PriorityModel model)
    {
        model.Id = entity.Id;
        model.Title = entity.Title;
    }

    public static UserModel ToModel(this UserEntity entity)
    {
        UserModel model = new();
        entity.CopyToModel(model);
        return model;
    }

    public static void CopyToModel(this UserEntity entity, UserModel model)
    {
        model.Id = entity.Id;
        model.UserName = entity.UserName;
        model.Email = entity.Email;
        model.PasswordHash = entity.PasswordHash;
        model.LastChangeAt = entity.LastChangeAt;
    }

    public static SettingsModel ToModel(this SettingsEntity entity)
    {
        SettingsModel model = new();
        entity.CopyToModel(model);
        return model;
    }

    public static void CopyToModel(this SettingsEntity entity, SettingsModel model)
    {
        model.Id = entity.Id;
        model.UserId = entity.UserId;
        model.IsDarkMode = entity.IsDarkMode;
        model.Theme = entity.Theme;
        model.StartPage = entity.StartPage;
        model.StartSidebar = entity.StartSidebar;
        model.Culture = entity.Culture;
        model.FirstDayOfWeek = entity.FirstDayOfWeek;
        model.SelectedRatio = entity.SelectedRatio;
        model.BaseUrl = entity.BaseUrl;
        model.RefreshToken = entity.RefreshToken;
        model.RememberMe = entity.RememberMe;
        model.ShowHelp = entity.ShowHelp;
        model.UncheckAllItemsOnHabitDone = entity.UncheckAllItemsOnHabitDone;
        model.ShowItemList = entity.ShowItemList;
        model.ShowSmallCalendar = entity.ShowSmallCalendar;
        model.ShowLargeCalendar = entity.ShowLargeCalendar;
        model.ShowHabitStatistics = entity.ShowHabitStatistics;
        model.ShowColor = entity.ShowColor;
        model.ShowCreatedUpdated = entity.ShowCreatedUpdated;
        model.InsertTabsInNoteContent = entity.InsertTabsInNoteContent;
        model.DisplayNoteContentAsMarkdown = entity.DisplayNoteContentAsMarkdown;
        model.HideCompletedTasks = entity.HideCompletedTasks;
        model.ShowOnlyOverSelectedRatioMin = entity.ShowOnlyOverSelectedRatioMin;
        model.SelectedRatioMin = entity.SelectedRatioMin;
        model.HorizontalMargin = entity.HorizontalMargin;
        model.VerticalMargin = entity.VerticalMargin;
        model.CategoryFilterDisplay = entity.CategoryFilterDisplay;
        model.PriorityFilterDisplay = entity.PriorityFilterDisplay;
        model.SelectedCategoryId = entity.SelectedCategoryId;
        model.SelectedPriority = entity.SelectedPriority;
        model.HiddenCategoryIds = entity.HiddenCategoryIds;
        model.ShowPriority = entity.ShowPriority;
        model.FoldSection = entity.FoldSection;
        model.SortBy = entity.SortBy;
    }

    public static TaskModel ToModel(this TaskEntity entity)
    {
        TaskModel model = new();
        entity.CopyToModel(model);
        return model;
    }

    public static void CopyToModel(this TaskEntity entity, TaskModel model)
    {
        model.Id = entity.Id;
        model.CategoryId = entity.CategoryId;
        model.Priority = entity.Priority;
        model.IsDeleted = entity.IsDeleted;
        model.Title = entity.Title;
        model.Color = entity.Color;
        model.CreatedAt = entity.CreatedAt;
        model.UpdatedAt = entity.UpdatedAt;

        model.StartedAt = entity.StartedAt;
        model.CompletedAt = entity.CompletedAt;
        model.PlannedAt = entity.PlannedAt;
        model.Duration = entity.Duration;
    }

    public static TimeModel ToModel(this TimeEntity entity)
    {
        TimeModel model = new();
        entity.CopyToModel(model);
        return model;
    }

    public static void CopyToModel(this TimeEntity entity, TimeModel model)
    {
        model.Id = entity.Id;
        model.HabitId = entity.HabitId;
        model.StartedAt = entity.StartedAt;
        model.CompletedAt = entity.CompletedAt;
    }
}
