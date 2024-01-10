using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Data;

public static class DataExtensions
{
    public static Model ToModel(this Entity entity, ModelType modelType)
    {
        return new Model
        {
            Id = entity.Id,
            IsDeleted = entity.IsDeleted,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Priority = entity.Priority,
            Importance = entity.Importance,
            ModelType = modelType
        };
    }
}
