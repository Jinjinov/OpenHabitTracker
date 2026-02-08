using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class PriorityService(ClientState appData)
{
    private readonly ClientState _appData = appData;

    public IReadOnlyCollection<PriorityModel>? Priorities => _appData.Priorities?.Values;

    public PriorityModel? SelectedPriority { get; set; }

    public PriorityModel? NewPriority { get; set; }

    public string GetPriorityTitle(Priority priority)
    {
        return priority switch
        {
            Priority.None => "⊘",
            Priority.VeryLow => "︾",
            Priority.Low => "﹀",
            Priority.Medium => "—",
            Priority.High => "︿",
            Priority.VeryHigh => "︽",
            _ => throw new ArgumentOutOfRangeException(nameof(priority)),
        };

        // after we delete and re-load all data, "(long)priority" does not work anymore, because database Id are not 0,1,2,3,4,5 anymore

        /*
        if (priority == Priority.None)
            return "⊘";

        return _appData.Priorities?.GetValueOrDefault((long)priority)?.Title ?? priority.ToString();
        */
    }

    public async Task Initialize()
    {
        await _appData.LoadPriorities();

        NewPriority ??= new();
    }

    public void SetSelectedPriority(long? id)
    {
        if (_appData.Priorities is null)
            return;

        SelectedPriority = id.HasValue && _appData.Priorities.TryGetValue(id.Value, out PriorityModel? priority) ? priority : null;
    }
    /*
    public async Task AddPriority()
    {
        if (_appData.Priorities is null || NewPriority is null)
            return;

        PriorityEntity priority = NewPriority.ToEntity();

        await _appData.DataAccess.AddPriority(priority);

        NewPriority.Id = priority.Id;

        _appData.Priorities.Add(NewPriority.Id, NewPriority);

        NewPriority = new();
    }
    */
    public async Task UpdatePriority(string title)
    {
        if (Priorities is null || SelectedPriority is null)
            return;

        SelectedPriority.Title = title;

        if (await _appData.DataAccess.GetPriority(SelectedPriority.Id) is PriorityEntity priority)
        {
            priority.Title = SelectedPriority.Title;

            await _appData.DataAccess.UpdatePriority(priority);
        }

        SelectedPriority = null;
    }
    /*
    public async Task DeletePriority(PriorityModel priority)
    {
        if (_appData.Priorities is null)
            return;

        _appData.Priorities.Remove(priority.Id);

        await _appData.DataAccess.RemovePriority(priority.Id);
    }
    */
}
