using System.Reflection;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Data;

// Every stored field must survive entity -> model -> entity, so a field added to one side and
// forgotten in EntityToModel or ModelToEntity fails here instead of silently resetting on save.
[TestFixture]
public class EntityModelRoundTripTests
{
    private const BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    // The entity fields a model does not carry, each written by the store rather than by the app.
    private static readonly string[] _userScoped = ["UserId"];

    private static IEnumerable<TestCaseData> Pairs()
    {
        yield return new TestCaseData(typeof(CategoryEntity), typeof(CategoryModel), Array.Empty<string>()).SetArgDisplayNames("Category");
        yield return new TestCaseData(typeof(HabitEntity), typeof(HabitModel), _userScoped).SetArgDisplayNames("Habit");
        yield return new TestCaseData(typeof(ItemEntity), typeof(ItemModel), _userScoped).SetArgDisplayNames("Item");
        yield return new TestCaseData(typeof(NoteEntity), typeof(NoteModel), _userScoped).SetArgDisplayNames("Note");
        yield return new TestCaseData(typeof(PriorityEntity), typeof(PriorityModel), Array.Empty<string>()).SetArgDisplayNames("Priority");
        yield return new TestCaseData(typeof(UserEntity), typeof(UserModel), Array.Empty<string>()).SetArgDisplayNames("User");
        yield return new TestCaseData(typeof(SettingsEntity), typeof(SettingsModel), Array.Empty<string>()).SetArgDisplayNames("Settings");
        yield return new TestCaseData(typeof(TaskEntity), typeof(TaskModel), _userScoped).SetArgDisplayNames("Task");
        yield return new TestCaseData(typeof(TimeEntity), typeof(TimeModel), _userScoped).SetArgDisplayNames("Time");
    }

    [TestCaseSource(nameof(Pairs))]
    public void ToModelThenToEntity_KeepsEveryField(Type entityType, Type modelType, string[] notOnModel)
    {
        object entity = Activator.CreateInstance(entityType)!;
        List<PropertyInfo> entityProperties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanWrite).ToList();

        int seed = 1;
        foreach (PropertyInfo property in entityProperties)
            property.SetValue(entity, DifferentValue(property.PropertyType, property.GetValue(entity), seed++));

        object model = typeof(EntityToModel).GetMethod(nameof(EntityToModel.ToModel), [entityType])!.Invoke(null, [entity])!;

        List<string> unmapped = [];

        foreach (PropertyInfo property in entityProperties)
        {
            PropertyInfo? modelProperty = modelType.GetProperty(property.Name, AnyInstance);

            if (modelProperty is null)
            {
                unmapped.Add(property.Name);
                continue;
            }

            Assert.That(modelProperty.GetValue(model), Is.EqualTo(property.GetValue(entity)), $"{entityType.Name}.{property.Name} -> model");
        }

        Assert.That(unmapped, Is.EquivalentTo(notOnModel), $"{entityType.Name} fields the model does not carry");

        object roundTrip = typeof(ModelToEntity).GetMethod(nameof(ModelToEntity.ToEntity), [modelType])!.Invoke(null, [model])!;

        foreach (PropertyInfo property in entityProperties.Where(x => !notOnModel.Contains(x.Name)))
            Assert.That(property.GetValue(roundTrip), Is.EqualTo(property.GetValue(entity)), $"{entityType.Name}.{property.Name} -> entity");
    }

    [TestCaseSource(nameof(Pairs))]
    public void CopyToEntity_LeavesTheFieldsTheModelDoesNotCarry(Type entityType, Type modelType, string[] notOnModel)
    {
        object model = Activator.CreateInstance(modelType)!;
        object entity = Activator.CreateInstance(entityType)!;

        int seed = 1;
        foreach (string name in notOnModel)
        {
            PropertyInfo property = entityType.GetProperty(name)!;
            property.SetValue(entity, DifferentValue(property.PropertyType, property.GetValue(entity), seed++));
        }

        Dictionary<string, object?> before = notOnModel.ToDictionary(x => x, x => entityType.GetProperty(x)!.GetValue(entity));

        typeof(ModelToEntity).GetMethod(nameof(ModelToEntity.CopyToEntity), [modelType, entityType])!.Invoke(null, [model, entity]);

        foreach (string name in notOnModel)
            Assert.That(entityType.GetProperty(name)!.GetValue(entity), Is.EqualTo(before[name]), $"{entityType.Name}.{name}");
    }

    private static object? DifferentValue(Type type, object? current, int seed)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(long))
            return 1000L + seed;
        if (underlying == typeof(int))
            return 10 + seed;
        if (underlying == typeof(double))
            return 2.5 + seed;
        if (underlying == typeof(bool))
            return current is bool flag ? !flag : true;
        if (underlying == typeof(string))
            return $"value {seed}";
        if (underlying == typeof(DateTime))
            return new DateTime(2026, 1, 1, 6, 0, 0).AddHours(seed);
        if (underlying == typeof(TimeOnly))
            return new TimeOnly(1, seed % 60);
        if (underlying.IsEnum)
        {
            Array values = Enum.GetValues(underlying);
            int index = current is null ? 0 : Array.IndexOf(values, current);
            return values.GetValue((index + 1) % values.Length);
        }
        if (underlying == typeof(List<long>))
            return new List<long> { seed, seed + 1 };
        if (underlying == typeof(Dictionary<Priority, bool>))
            return Enum.GetValues<Priority>().ToDictionary(x => x, x => x == Priority.High);
        if (underlying == typeof(Dictionary<QuerySection, bool>))
            return Enum.GetValues<QuerySection>().ToDictionary(x => x, x => x == QuerySection.Sort);
        if (underlying == typeof(Dictionary<ContentType, Sort>))
            return Enum.GetValues<ContentType>().ToDictionary(x => x, _ => Sort.Title);

        throw new NotSupportedException($"No test value for {type}");
    }
}
